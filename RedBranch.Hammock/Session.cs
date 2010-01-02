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
    public enum Disposition
    {
        Continue,
        Decline,
    }

    public interface IObserver
    {
        Disposition BeforeSave(object entity, Document document);
        Disposition BeforeDelete(object entity, Document document);

        void AfterSave(object entity, Document document);
        void AfterDelete(object entity, Document document);
        void AfterLoad(object entity, Document document);
    }

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

        private int _locks;
        private Dictionary<object, Document> _entities = new Dictionary<object, Document>(100);
        private List<IObserver> _observers;

        public Session(Connection connection, string database)
        {
            InvalidDatabaseNameException.Validate(database);
            Connection = connection;
            Database = database;
        }

        public ICollection<IObserver> Observers
        {
            get { return _observers ?? (_observers = new List<IObserver>()); }
        }

        public IList<Document> List()
        {
            var request = (HttpWebRequest)WebRequest.Create(Connection.GetDatabaseLocation(Database) + "_all_docs");
            using (var reader = request.GetCouchResponse())
            {
                var a = new List<Document>();
                var o = JObject.Load(reader);
                var rows = o["rows"];
                foreach (JObject r in rows)
                {
                    a.Add(new Document()
                              {
                                  Id = r.Value<string>("id"),
                                  Revision = r["value"].Value<string>("rev")
                              });
                }
                return a;
            }
        }
        
        public Document Save<TEntity>(TEntity entity, string id) where TEntity : class
        {
            if (_entities.ContainsKey(entity))
            {
                var d = _entities[entity];
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
            var d =
                _entities.ContainsKey(entity)
                    ? _entities[entity]
                    : (entity as Document) ?? 
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
            var o = EntitySerializer.Write(entity, d);
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
    
            if (_entities.ContainsKey(entity))
            {
                _entities.Remove(entity);
            }
            _entities.Add(entity, d);

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
            foreach (var p in _entities)
            {
                if (p.Value.Id == id)
                {
                    var e = p.Key as TEntity;
                    if (null == e)
                    {
                        throw new InvalidCastException();
                    }
                    return e;
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

                var entity = EntitySerializer.Read<TEntity>(o, ref d);
                _entities[entity] = d;

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
            if (!_entities.ContainsKey(entity))
            {
                throw new IndexOutOfRangeException("The entity is not currently enrolled in this session.");
            }
            var d = _entities[entity];

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
                    _entities.Remove(entity);
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

        public bool IsEnrolled(string id)
        {
            return _entities.Where(x => x.Value.Id == id).Count() == 1;
        }

        public bool IsEnrolled<TEntity>(TEntity entity) where TEntity : class
        {
            return _entities.ContainsKey(entity);
        }

        public void Enroll<TEntity>(Document d, TEntity entity) where TEntity : class
        {
            if (IsEnrolled(d.Id))
            {
                throw new Exception("A entity with this key is already enrolled.");
            }
            _entities.Add(entity, d);
        }

        public void Reset()
        {
            _entities = _entities.Where(x => x.Value.Id.StartsWith("_design/")).ToDictionary(x => x.Key, x => x.Value);
        }

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
