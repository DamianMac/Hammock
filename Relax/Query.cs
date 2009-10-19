using System;
using System.Collections.Generic;
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
                        if (String.IsNullOrEmpty(Id)) return null;
                        if (null == _doc)
                        {
                            _entity = Query.Session.Load<TEntity>(Id);
                        }
                        else
                        {
                            if (null == _entity)
                            {
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

            private QueryPage Page { get; set; }
            private Query<TEntity> Query { get; set; }

            internal Result(Query<TEntity> q, QueryPage page, JToken o)
            {
                Query = q;
                Page = page;
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
                var p = Page.Next();
                return p.offset >= Total ? null : Query.Execute(p);
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

        public Result Execute()
        {
            return Execute(new QueryPage {group = Group});
        }

        public Result Execute(int limit)
        {
            return Execute(new QueryPage {limit = limit, group = Group});
        }

        public Result Execute(
            int? limit,
            JToken start_key,
            JToken end_key)
        {
            return Execute(new QueryPage
               {
                   group = Group,
                   start_key = start_key,
                   end_key = end_key,
                   limit = limit
               });
        }

        public Result Execute(QueryPage page)
        {
            var location = String.Format(
                "{0}_design/{1}/_view/{2}{3}",
                Session.Connection.GetDatabaseLocation(Session.Database),
                Design,
                View,
                page.ToString()
            );

            var request = (HttpWebRequest)WebRequest.Create(location);
            using (var reader = request.GetCouchResponse())
            {
                return new Result(this, page, JToken.ReadFrom(reader));
            }
        }
    }

    public class QueryPage
    {
        public bool include_docs { get; set; }
        public bool group { get; set; }
        public int? offset { get; set; }
        public int? limit { get; set; }
        public JToken start_key { get; set; }
        public JToken end_key { get; set; }
        public string start_docid { get; set; }
        public string end_docid { get; set; }

        public override string ToString()
        {
            var location = new StringBuilder();
            var sep = '?';
            if (group)
            {
                location.AppendFormat("{0}group=true", sep);
                sep = '&';
            }
            if (include_docs)
            {
                location.AppendFormat("{0}include_docs=true", sep);
                sep = '&';
            }
            if (null != offset && offset > 0)
            {
                location.AppendFormat("{0}skip={1}", sep, offset);
                sep = '&';
            }
            if (null != limit && limit >= 0)
            {
                location.AppendFormat("{0}limit={1}", sep, limit);
                sep = '&';
            }
            if (null != start_key)
            {
                location.AppendFormat("{0}startkey={1}", sep, start_key.ToString(Formatting.None));
                sep = '&';
            }
            if (null != end_key)
            {
                location.AppendFormat("{0}endkey={1}", sep, end_key.ToString(Formatting.None));
                sep = '&';
            }
            if (null != start_docid)
            {
                location.AppendFormat("{0}startkey_docid={1}", sep, start_docid);
                sep = '&';
            }
            if (null != end_docid)
            {
                location.AppendFormat("{0}endkey_docid={1}", sep, end_docid);
                sep = '&';
            }
            return location.ToString();
        }

        public QueryPage Next()
        {
            // TODO: implement key+docid paging: http://wiki.apache.org/couchdb/How_to_page_through_results
            // Since emit() will write multiple rows with the same key+docid, I don't see
            // how you can make any claim that this pagination method works. This holds
            // especially true in this case, where we are a framework library.

            var page = (QueryPage)MemberwiseClone();
            page.offset = (page.offset ?? 0) + (page.limit ?? 10);
            return page;
        }
    }
}
