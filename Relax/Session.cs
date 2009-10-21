using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Relax.Design;

namespace Relax
{
    public class Document
    {
        public Session Session { get; set; }
        public string Id { get; set; }
        public string Revision { get; set; }

        public string Location
        {
            get
            {
                return Session.Connection.GetDatabaseLocation(Session.Database) +
                    (Id.StartsWith("_design/")
                        ? "_design/" + Id.Substring(8).Replace("/", "%2F")
                        : Id.Replace("/", "%2F"));
            }
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode() ^ (Revision ?? "-").GetHashCode();
        }

        public override bool Equals(object obj)
        {
            var d = (Document)obj;
            return null == d
                       ? base.Equals(obj)
                       : d.Id == Id &&
                         d.Revision == Revision;
        }
    }

    public class Session
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

        private Dictionary<object, Document> _entities = new Dictionary<object, Document>(100);

        public Session(Connection connection, string database)
        {
            InvalidDatabaseNameException.Validate(database);
            Connection = connection;
            Database = database;
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
        
        public Document Save<TDocument>(TDocument document, string id) where TDocument : class
        {
            if (_entities.ContainsKey(document))
            {
                throw new Exception("This overload cannot be used to save existing documents.");
            }

            return Save(document, new Document {Session = this, Id = id});
        }
            
        public Document Save<TDocument>(TDocument document) where TDocument : class
        {
            var d = _entities.ContainsKey(document)
                ? _entities[document]
                : new Document
                {
                    Session = this,
                    Id = typeof(TDocument).Name.ToLowerInvariant() + "-" + Guid.NewGuid(),
                    Revision = null,
                };
            
            return Save(document, d);
        }

        private Document Save<TDocument>(TDocument document, Document d) where TDocument : class
        {
            var serializer = new JsonSerializer(); 
            var request = (HttpWebRequest) WebRequest.Create(d.Location);
            request.Method = "PUT";
            using (var writer = new JsonTextWriter(new StreamWriter(request.GetRequestStream())))
            {
                if (!String.IsNullOrEmpty(d.Revision))
                {
                    var jwriter = new JTokenWriter();
                    serializer.Serialize(jwriter, document);
                    var o = jwriter.Token;
                    o.First.AddBeforeSelf(new JProperty("_rev", d.Revision));
                    o.First.AddBeforeSelf(new JProperty("_id", d.Id));
                    o.WriteTo(writer);
                }
                else
                {
                    serializer.Serialize(writer, document);
                }
            }
            using (var reader = request.GetCouchResponse())
            {
                var response = (__DocumentResponse)serializer.Deserialize(reader, typeof(__DocumentResponse));
                d = response.ToDocument(this);
            }
    
            if (_entities.ContainsKey(document))
            {
                _entities.Remove(document);
            }
            _entities.Add(document, d);

            return d;
        }

        public TDocument Load<TDocument>(string id) where TDocument : class
        {
            foreach (var p in _entities)
            {
                if (p.Value.Id == id)
                {
                    var doc = p.Key as TDocument;
                    if (null == doc)
                    {
                        throw new InvalidCastException();
                    }
                    return doc;
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
                d.Id = (string)o["_id"];
                d.Revision = (string) o["_rev"];
                var serializer = new JsonSerializer();
                var response = (TDocument)serializer.Deserialize(new JTokenReader(o), typeof(TDocument));
                _entities[response] = d;
                return response;
            }
        }

        public void Delete<TDocument>(TDocument document) where TDocument : class
        {
            if (!_entities.ContainsKey(document))
            {
                throw new IndexOutOfRangeException("The document is not currently enrolled in this session.");
            }
            var d = _entities[document];
            var request = (HttpWebRequest)WebRequest.Create(d.Location);
            request.Method = "DELETE";
            request.Headers[HttpRequestHeader.IfMatch] = d.Revision;
            using (var reader = request.GetCouchResponse())
            {
                var serializer = new JsonSerializer();
                var response = (__DocumentResponse)serializer.Deserialize(reader, typeof(__DocumentResponse));
                if (response.ok)
                {
                    _entities.Remove(document);
                }
                else
                {
                    throw new Exception();
                }
            }
        }

        public bool Contains(string id)
        {
            foreach (var p in _entities)
            {
                if (p.Value.Id == id)
                {
                    return true;
                }
            }
            return false;
        }

        public void Enroll<TDocument>(Document d, TDocument document) where TDocument : class
        {
            if (Contains(d.Id))
            {
                throw new Exception("A document with this key is already enrolled.");
            }
            _entities.Add(document, d);
        }

        public void Reset()
        {
            throw new NotImplementedException();
        }

        public IDisposable Lock()
        {
            throw new NotImplementedException();
        }
    }
}
