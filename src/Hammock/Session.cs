// 
//  Session.cs
//  
//  Author:
//       Nick Nystrom <nnystrom@gmail.com>
//  
//  Copyright (c) 2009-2011 Nicholas J. Nystrom
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
// 
//  You should have received a copy of the GNU Lesser General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RedBranch.Hammock.Design;

namespace RedBranch.Hammock
{
    public partial class Session : IDisposable
    {
        private class __DocumentResponse
        {
            public bool ok { get; set; }
            public string id { get; set; }
            public string rev { get; set; }

            public Document ToDocument(Session session)
            {
                return new Document
                           {
                               Session = session,
                               Id = id,
                               Revision = rev
                           };
            }
        }

        public Connection Connection { get; private set; }
        public string Database { get; private set; }
        public EntitySerializer Serializer { get; private set; }

        private int _locks;
        private Dictionary<object, Document> _entities = new Dictionary<object, Document>(100);
        private List<IObserver> _observers;

        public Session(Connection connection, string database)
        {
            InvalidDatabaseNameException.Validate(database);
            Connection = connection;
            Database = database;
            Serializer = new EntitySerializer(this);
        }

        public ICollection<IObserver> Observers
        {
            get { return _observers ?? (_observers = new List<IObserver>()); }
        }

        /// <summary>
        /// Lists all documents in this session's database using the _all_docs view.
        /// </summary>
        /// <returns></returns>
        public IList<Document> ListDocuments()
        {
            var q = new AllDocumentsQuery(this);
            var r = q.All().Execute();
            return r.Rows.Select(x => new Document {
                Id = x.Id,
                Revision = x.Value.Value<string>("rev")
            }).ToList();
        }

        /// <summary>
        /// Lists all entity data in this session's database, returning the full document
        /// for each entity. This is an expensive operation and should be used carefully.
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// Unlike all other methods of retrieving an entity, the data returned by this
        /// method bypasses the enrollment system.
        /// </remarks>
        public IList<JToken> ListEntities()
        {
            return new AllDocumentsQuery(this).All().WithDocuments().ToList();
        }

        /// <summary>
        /// Loads the raw json for an entity from the database.
        /// </summary>
        /// <param name="id">The id of the entity to load.</param>
        /// <returns></returns>
        /// <remarks>
        /// This Load override completely bypasses the standard enrollment system that
        /// typed versions of Load use, and can easily cause version conflicts if mis-used.
        /// </remarks>
        public JToken LoadRaw(string id)
        {
            var d = new Document
            {
                Session = this,
                Id = id,
            };
            var request = (HttpWebRequest)WebRequest.Create(d.Location);
            using (var reader = request.GetCouchResponse())
            {
                return JToken.ReadFrom(reader);
            }
        }

        /// <summary>
        /// Saves the raw json entity to the database. Entity must have an _id property, and
        /// must have a _rev property if it will update an existing entity.
        /// </summary>
        /// <param name="entity">The entity to save.</param>
        /// <remarks>
        /// This Save override completely bypasses the standard enrollment system that typed
        /// versions of Save use, and can easily cause version conflicts if mis-used.
        /// </remarks>
        public Document SaveRaw(JToken entity)
        {
            // read the id from the entity
            var d = new Document {
                Session = this,
                Id = entity.Value<string>("_id"),
            };

            // send put            
            var request = (HttpWebRequest)WebRequest.Create(d.Location);
            request.Method = "PUT";
            using (var writer = new JsonTextWriter(new StreamWriter(request.GetRequestStream())))
            {
                entity.WriteTo(writer);
            }

            // get couch reply
            using (var reader = request.GetCouchResponse())
            {
                var serializer = new JsonSerializer();
                var response = (__DocumentResponse)serializer.Deserialize(reader, typeof(__DocumentResponse));
                d = response.ToDocument(this);
            }
            var o = entity as JObject;
            if (null != o)
            {
                o["_rev"] = new JValue(d.Revision);
            }

            return d;
        }

        public Document GetDocument(object entity)
        {
            lock (_entities)
            {
                return _entities.ContainsKey(entity) ? _entities[entity] : null;
            }
        }

        public Document Save<TEntity>(TEntity entity, string id) where TEntity : class
        {
            var d = GetDocument(entity);
            if (null != d)
            {
                if (d.Id != id)
                {
                    throw new Exception("This entity is already saved under the id '" + d.Id + "' and cannot be reassigned the new id '" + id + "' in this session.");
                }
                return Save(entity, d);
            }
            return Save(entity, new Document {Session = this, Id = id});
        }
            
        public Document Save<TEntity>(TEntity entity) where TEntity : class
        {
            var d = GetDocument(entity) ??
                      (entity as Document) ?? 
                      (entity is IHasDocument ? ((IHasDocument)entity).Document : null) ??
                      new Document();
            if (String.IsNullOrEmpty(d.Id))
			{
                d.Id = Document.For<TEntity>(Guid.NewGuid().ToString());
                d.Revision = null;
            }
            return Save(entity, d);
        }

        private Document Save<TEntity>(TEntity entity, Document d) where TEntity : class
        {
            if (null == d.Session)
            {
                d.Session = this;
            }

            // allow observers to veto the save
            if (null != _observers)
            {
                if (_observers.Any(x => Disposition.Decline == x.BeforeSave(entity, d)))
                {
                    throw new Exception("An observer declined the save operation.");
                }
            }

            // send put            
            var request = (HttpWebRequest) WebRequest.Create(d.Location);
            request.Method = "PUT";
            var o = Serializer.Write(entity, d);
            using (var writer = new JsonTextWriter(new StreamWriter(request.GetRequestStream())))
            {
                o.WriteTo(writer);
            }

            // get couch reply
            using (var reader = request.GetCouchResponse())
            {
                var serializer = new JsonSerializer();
                var response = (__DocumentResponse)serializer.Deserialize(reader, typeof(__DocumentResponse));
                d = response.ToDocument(this);
            }
            d = UpdateEntityDocument(entity, d);

            // inform observers
            if (null != _observers)
            {
                _observers.Each(x => x.AfterSave(entity, d));
            }

            return d;
        }

        private Document UpdateEntityDocument<TEntity>(TEntity entity, Document d) where TEntity : class
        {
            // use the entity itself if it subclasses document
            var docsubclass = entity as Document;
            if (null != docsubclass)
            {
                docsubclass.Id = d.Id;
                docsubclass.Revision = d.Revision;
                docsubclass.Session = d.Session;
                d = docsubclass;
            }

            lock (_entities)
            {
                if (_entities.ContainsKey(entity))
                {
                    _entities.Remove(entity);
                }
                _entities.Add(entity, d);
            }

            // fill the document property if the entity implements IHasDocument
            var icanhasdoc = entity as IHasDocument;
            if (null != icanhasdoc)
            {
                icanhasdoc.Document = d;
            }

            return d;
        }
        
        public TEntity Load<TEntity>(string id) where TEntity : class
        {
            lock (_entities)
            {
                foreach (var e in _entities.Where(p => p.Value.Id == id))
                {
                    return (TEntity)e.Key;
                }
            }

            var d = new Document
                        {
                            Session = this,
                            Id = id,
                        };
            var request = (HttpWebRequest)WebRequest.Create(d.Location);
            using (var reader = request.GetCouchResponse())
            {
                var o = JToken.ReadFrom(reader);

                var entity = Serializer.Read<TEntity>(o, ref d);
                lock (_entities)
                {
                    _entities[entity] = d;
                }

                // inform observers
                if (null != _observers)
                {
                    _observers.Each(x => x.AfterLoad(entity, d));
                }

                return entity;
            }
        }

        public void Delete<TEntity>(TEntity entity) where TEntity : class
        {
            // locate the document
            var d = GetDocument(entity);
            if (null == d)
            {
                throw new IndexOutOfRangeException("The entity is not currently enrolled in this session.");
            }
            
            // allow observers to veto the delete
            if (null != _observers)
            {
                if (_observers.Any(x => Disposition.Decline == x.BeforeDelete(entity, d)))
                {
                    throw new Exception("An observer declined the delete operation.");
                }
            }

            // delete
            var request = (HttpWebRequest)WebRequest.Create(d.Location);
            request.Method = "DELETE";
            request.Headers[HttpRequestHeader.IfMatch] = d.Revision;
            using (var reader = request.GetCouchResponse())
            {
                var serializer = new JsonSerializer();
                var response = (__DocumentResponse)serializer.Deserialize(reader, typeof(__DocumentResponse));
                if (response.ok)
                {
                    lock (_entities)
                    {
                        _entities.Remove(entity);
                    }
                }
                else
                {
                    throw new Exception();
                }
            }

            // inform observers
            if (null != _observers)
            {
                _observers.Each(x => x.AfterDelete(entity, d));
            }
        }

        /// <summary>
        /// Determines whether an entity with the given id is enrolled into this Session.
        /// </summary>
        /// <param name="id">The document id to search for.</param>
        /// <returns>True, if an entity with the given id is enrolled, or false if it is not.</returns>
        public bool IsEnrolled(string id)
        {
            lock (_entities)
            {
                return _entities.Where(x => x.Value.Id == id).Count() == 1;
            }
        }

        /// <summary>
        /// Determines whether the given entity is enrolled into this Session.
        /// </summary>
        /// <typeparam name="TEntity">The type of entity to search for.</typeparam>
        /// <param name="entity">The entity to search for.</param>
        /// <returns>True, if the given entity is enrolled, or false if it is not.</returns>
        public bool IsEnrolled<TEntity>(TEntity entity) where TEntity : class
        {
            lock (_entities)
            {
                return _entities.ContainsKey(entity);
            }
        }

        /// <summary>
        /// Enrolls a given entity in this Session, using the suppied document.
        /// </summary>
        /// <typeparam name="TEntity">The type of entity to enroll.</typeparam>
        /// <param name="d">The document that will be associated with the entity when it is enrolled.</param>
        /// <param name="entity">The entity to enroll.</param>
        /// <exception cref="System.Exception">Thrown if the any entity is already enrolled using the given document.</exception>
        public void Enroll<TEntity>(Document d, TEntity entity) where TEntity : class
        {
            lock (_entities)
            {
                if (IsEnrolled(d.Id))
                {
                    throw new Exception("A entity with this key is already enrolled.");
                }
                _entities.Add(entity, d);
            }
        }

        /// <summary>
        /// Flushes all non-design documents from this Session. Sessions retrieved through the 
        /// Connection object are automatically Reset() for you.
        /// </summary>
        /// <remarks>
        /// When calling Reset() manually, take care that you do not hold any references to entities retrieved
        /// through the Session, as any attempt to Update() or Delete() them will result in exceptions.
        /// </remarks>
        public void Reset()
        {
            lock (_entities)
            {
                _entities = _entities.Where(x => x.Value.Id.StartsWith("_design/")).ToDictionary(x => x.Key, x => x.Value);
            }
        }

        /// <summary>
        /// Increments the reference count for this Session, ensuring that the Session will not be recycled
        /// back to the owning Connection's available Session pool until the lock is disposed.
        /// </summary>
        /// <remarks>
        /// See http://code.google.com/p/relax-net/wiki/SessionPooling for details.
        /// </remarks>
        /// <returns></returns>
        public IDisposable Lock()
        {
            Interlocked.Increment(ref _locks);
            return this;
        }

        private void Release()
        {
            ((IDisposable)this).Dispose();
        }

        void IDisposable.Dispose()
        {
            var l = Interlocked.Decrement(ref _locks);
            if (l < 1)
            {
                Connection.ReturnSession(this);
            }        
        }
    }
}
