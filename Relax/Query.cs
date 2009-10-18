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

                internal Row(Query<TEntity> q, JToken o)
                {
                    Query = q;
                    Id = (string)o["id"];
                    Key = o["key"];
                    Value = o["value"];
                }

                public string Id { get; set; }
                public JToken Key { get; set; }
                public JToken Value { get; set; }
            }
            
            private Query<TEntity> Query { get; set; }

            internal Result(Query<TEntity> q, JToken o)
            {
                Query = q;
                Total = (long) o["total_rows"];
                Offset = (long) o["offset"];
                Rows = o["rows"].Select(x => new Row(Query, x)).ToArray();
            }

            public long Total { get; set; }
            public long Offset { get; set; }

            public Row[] Rows { get; set; }
        }



        public Session Session { get; private set; }    
        public string Design { get; private set; }
        public string View { get; private set; }

        public Query(Session sx, string design, string view)
        {
            Session = sx;
            Design = design;
            View = view;
        }

        public Result Execute()
        {
            return Execute(new QueryPagination());
        }

        public Result Execute(int limit)
        {
            return Execute(new QueryPagination {limit = limit});
        }

        public Result Execute(
            int? limit,
            JToken start_key,
            JToken end_key)
        {
            return Execute(new QueryPagination {start_key = start_key, end_key = end_key, limit = limit});
        }

        public Result Execute(QueryPagination pagination)
        {
            var location = String.Format(
                "{0}_design/{1}/_view/{2}{3}",
                Session.Connection.GetDatabaseLocation(Session.Database),
                Design,
                View,
                pagination.ToString()
            );

            var request = (HttpWebRequest)WebRequest.Create(location);
            using (var reader = request.GetCouchResponse())
            {
                var serializer = new JsonSerializer();
                
                return (Result)serializer.Deserialize(reader, typeof(Result));
            }
        }
    }

    public class QueryPagination
    {
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
    }
}
