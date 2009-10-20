using System;
using System.Linq;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Relax
{
    public class Query<TEntity> where TEntity : class
    {
        public class Result
        {
            public class Row
            {
                private Query<TEntity> Query { get; set; }

                private JToken _doc;
                private TEntity _entity;

                internal Row(Query<TEntity> q, JToken o)
                {
                    Query = q;
                    Id = (string)o["id"];
                    Key = o["key"];
                    Value = o["value"];
                    _doc = o["doc"];
                }

                public string Id { get; set; }
                public JToken Key { get; set; }
                public JToken Value { get; set; }
                
                public TEntity Entity
                {
                    get
                    {
                        // no id means no way to load a doc (reduce)
                        if (String.IsNullOrEmpty(Id)) return null;
                        if (null == _doc)
                        {
                            // no inline doc means we just do a simple pull from the session
                            _entity = Query.Session.Load<TEntity>(Id);
                        }
                        else
                        {
                            if (null == _entity)
                            {
                                // we have a full document inline, but we might need to return one
                                // from the session if this docid is already enrolled, otherwise
                                // we enroll it now using the data from this row
                                var d = new Document
                                  {
                                      Session = Query.Session,
                                      Id = (string) _doc["_id"],
                                      Revision = (string) _doc["_rev"]
                                  };
                                if (Query.Session.Contains(d.Id))
                                {
                                    _entity = Query.Session.Load<TEntity>(d.Id);
                                }
                                else
                                {
                                    var serializer = new JsonSerializer();
                                    _entity = (TEntity) serializer.Deserialize(new JTokenReader(_doc), typeof (TEntity));
                                    Query.Session.Enroll(d, _entity);
                                }
                            }
                        }
                        return _entity;   
                    }
                }

            }

            private Spec Spec { get; set; }
            private Query<TEntity> Query { get; set; }

            internal Result(Query<TEntity> q, Spec spec, JToken o)
            {
                Query = q;
                Spec = spec;
                Offset = ((long?) o["offset"]) ?? 0;
                Rows = null == o["rows"] 
                    ? new Row[0] 
                    : o["rows"].Select(x => new Row(Query, x)).ToArray();
                Total = ((long?) o["total_rows"]) ?? Rows.Length;
            }

            public long Total { get; set; }
            public long Offset { get; set; }

            public Row[] Rows { get; set; }

            public Result Next()
            {
                if (Offset + Rows.Length >= Total) return null;
                return Spec.Next().Execute();
            }
        }

        public class Spec
        {
            public Query<TEntity> Query { get; private set; }

            bool _include_docs;
            bool _group;
            long? _skip;
            long? _limit;
            JToken _start_key;
            JToken _end_key;
            string _startkey_docid;
            string _endkey_docid;

            public Spec(Query<TEntity> query)
            {
                Query = query;
            }

            public Result Execute()
            {
                var location = ToString();
                var request = (HttpWebRequest)WebRequest.Create(location);
                using (var reader = request.GetCouchResponse())
                {
                    return new Result(Query, this, JToken.ReadFrom(reader));
                }
            }

            public Spec From(JToken key)
            {
                _start_key = key;
                return this;
            }
            public Spec From(object key)
            {
                _start_key = JToken.FromObject(key);
                return this;
            }
            public Spec To(JToken key)
            {
                _end_key = key;
                return this;
            }
            public Spec To(object key)
            {
                _end_key = JToken.FromObject(key);
                return this;
            }
            public Spec FromDocId(string docid)
            {
                _startkey_docid = docid;
                return this;
            }
            public Spec ToDocId(string docid)
            {
                _endkey_docid = docid;
                return this;
            }
            public Spec Skip(long rows)
            {
                _skip = rows;
                return this;
            }
            public Spec Limit(long rows)
            {
                _limit = rows;
                return this;
            }
            public Spec WithDocuments()
            {
                _include_docs = true;
                return this;
            }
            public Spec Group()
            {
                _group = true;
                return this;
            }

            public override string ToString()
            {
                var location = new StringBuilder(
                    Query.Session.Connection.GetDatabaseLocation(
                        Query.Session.Database));

                location.Append("_design/");
                location.Append(Query.Design);
                location.Append("/_view/");
                location.Append(Query.View);

                var sep = '?';
                if (_group)
                {
                    location.AppendFormat("{0}group=true", sep);
                    sep = '&';
                }
                if (_include_docs)
                {
                    location.AppendFormat("{0}include_docs=true", sep);
                    sep = '&';
                }
                if (null != _skip && _skip > 0)
                {
                    location.AppendFormat("{0}skip={1}", sep, _skip);
                    sep = '&';
                }
                if (null != _limit && _limit >= 0)
                {
                    location.AppendFormat("{0}limit={1}", sep, _limit);
                    sep = '&';
                }
                if (null != _start_key)
                {
                    location.AppendFormat("{0}startkey={1}", sep, _start_key.ToString(Formatting.None));
                    sep = '&';
                }
                if (null != _end_key)
                {
                    location.AppendFormat("{0}endkey={1}", sep, _end_key.ToString(Formatting.None));
                    sep = '&';
                }
                if (null != _startkey_docid)
                {
                    location.AppendFormat("{0}startkey_docid={1}", sep, _startkey_docid);
                    sep = '&';
                }
                if (null != _endkey_docid)
                {
                    location.AppendFormat("{0}endkey_docid={1}", sep, _endkey_docid);
                    sep = '&';
                }
                return location.ToString();
            }

            public Spec Next()
            {
                // TODO: implement key+docid paging: http://wiki.apache.org/couchdb/How_to_page_through_results
                // Since emit() will write multiple rows with the same key+docid, I don't see
                // how you can make any claim that this pagination method works. This holds
                // especially true in this case, where we are a framework library.

                var page = (Spec)MemberwiseClone();
                page._skip = (page._skip ?? 0) + (page._limit ?? 10);
                return page;
            }
        }

        public Session Session { get; private set; }    
        public string Design { get; private set; }
        public string View { get; private set; }
        public bool Group { get; private set; }

        public Query(Session sx, string design, string view)
            : this(sx, design, view, false)
        {
        }

        public Query(Session sx, string design, string view, bool group)
        {
            Session = sx;
            Design = design;
            View = view;
            Group = group;
        }

        public Spec All()
        {
            var s = new Spec(this);
            if (Group) s.Group();
            return s;
        }
        public Spec From(JToken key)
        {
            return All().From(key);
        }
        public Spec From(object key)
        {
            return All().From(key);
        }
        public Spec Skip(long rows)
        {
            return All().Skip(rows);
        }
        public Spec Limit(long rows)
        {
            return All().Limit(rows);
        }
    }
}
