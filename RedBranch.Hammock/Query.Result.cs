// 
//  Query.Result.cs
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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RedBranch.Hammock
{
    public partial class Query<TEntity>
    {
        public class Result
        {
            public class Row
            {
                private Query<TEntity> Query { get; set; }

                private JToken _data;
                private TEntity _entity;

                internal Row(Query<TEntity> q, JToken o)
                {
                    Query = q;
                    Id = (string)o["id"];
                    Key = o["key"];
                    Value = o["value"];
                    _data = o["doc"];
                }

                public string Id { get; set; }
                public JToken Key { get; set; }
                public JToken Value { get; set; }
                public JToken Data { get; set; }

                public TEntity Entity
                {
                    get
                    {
                        // no id means no way to load a doc (reduce)
                        if (String.IsNullOrEmpty(Id)) return null;
                        if (null == _data)
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
                                    Id = (string)_data["_id"],
                                    Revision = (string)_data["_rev"]
                                };
                                if (Query.Session.IsEnrolled(d.Id))
                                {
                                    _entity = Query.Session.Load<TEntity>(d.Id);
                                }
                                else
                                {
                                    _entity = Query.Session.Serializer.Read<TEntity>(_data, ref d);
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
                Offset = ((long?)o["offset"]) ?? 0;
                Rows = null == o["rows"]
                    ? new Row[0]
                    : o["rows"].Select(x => new Row(Query, x)).ToArray();
                Total = ((long?)o["total_rows"]) ?? Rows.Length;
            }

            public long Total { get; set; }
            public long Offset { get; set; }

            public Row[] Rows { get; set; }

            public Result Next()
            {
                if (Offset + Rows.Length >= Total) return null;
                return Spec.Next().Execute();
            }

            private class __UniqueDocumentRowEqualityComparer : IEqualityComparer<Row>
            {
                public bool Equals(Row x, Row y)
                {
                    return String.Equals(x.Id, y.Id);
                }

                public int GetHashCode(Row obj)
                {
                    return obj.Id.GetHashCode();
                }
            }

            /// <summary>
            /// Returns only one Row for each unique document id in the result set.
            /// </summary>
            /// <returns></returns>
            public IEnumerable<Row> GetUniqueDocumentRows()
            {
                return Rows.Distinct(new __UniqueDocumentRowEqualityComparer());
            }
        }
    }
}
