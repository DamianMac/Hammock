//
//  Query.Spec.cs
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
        public class Spec : IEnumerable<TEntity>
        {
            private Result _cachedResult;

            public Query<TEntity> Query { get; private set; }

            bool _include_docs;
            bool _group;
            long? _skip;
            long? _limit;
            bool _descending;
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
                    return (_cachedResult = new Result(Query, this, JToken.ReadFrom(reader)));
                }
            }

            public Spec From(JToken key)
            {
                _cachedResult = null;
                _start_key = key;
                return this;
            }
            public Spec From(object key)
            {
                _cachedResult = null;
                _start_key = JToken.FromObject(key);
                return this;
            }
            public Spec To(JToken key)
            {
                _cachedResult = null;
                _end_key = key;
                return this;
            }
            public Spec To(object key)
            {
                _cachedResult = null;
                _end_key = JToken.FromObject(key);
                return this;
            }
            public Spec Exactly(JToken key)
            {
                return From(key).To(key);
            }
            public Spec Exactly(object key)
            {
                return From(key).To(key);
            }
            public Spec FromDocId(string docid)
            {
                _cachedResult = null;
                _startkey_docid = docid;
                return this;
            }
            public Spec ToDocId(string docid)
            {
                _cachedResult = null;
                _endkey_docid = docid;
                return this;
            }
            public Spec Skip(long rows)
            {
                _cachedResult = null;
                _skip = rows;
                return this;
            }
            public Spec Limit(long rows)
            {
                _cachedResult = null;
                _limit = rows;
                return this;
            }
            public Spec Descending()
            {
                _descending = true;
                return this;
            }
            public Spec Descending(bool descending)
            {
                _descending = descending;
                return this;
            }
            public Spec WithDocuments()
            {
                _cachedResult = null;
                _include_docs = true;
                return this;
            }
            public Spec Group()
            {
                _cachedResult = null;
                _group = true;
                return this;
            }

            public override string ToString()
            {
                var location = new StringBuilder(Query.Location);

                var sep = '?';
                if (_group)
                {
                    location.Append(sep);
                    location.Append("group=true");
                    sep = '&';
                }
                if (_include_docs)
                {
                    location.Append(sep);
                    location.Append("include_docs=true");
                    sep = '&';
                }
                if (null != _skip && _skip > 0)
                {
                    location.Append(sep);
                    location.Append("skip=");
                    location.Append(_skip);
                    sep = '&';
                }
                if (null != _limit && _limit >= 0)
                {
                    location.Append(sep);
                    location.Append("limit=");
                    location.Append(_limit);
                    sep = '&';
                }
                if (_descending)
                {
                    location.Append(sep);
                    location.Append("descending=true");
                    sep = '&';
                }
                if (null != _start_key)
                {
                    location.Append(sep);
                    location.Append("startkey=");
                    location.Append(HttpUtility.UrlEncode(_start_key.ToString(Formatting.None)));
                    sep = '&';
                }
                if (null != _end_key)
                {
                    location.Append(sep);
                    location.Append("endkey=");
                    location.Append(HttpUtility.UrlEncode(_end_key.ToString(Formatting.None)));
                    sep = '&';
                }
                if (null != _startkey_docid)
                {
                    location.Append(sep);
                    location.Append("startkey_docid=");
                    location.Append(_startkey_docid);
                    sep = '&';
                }
                if (null != _endkey_docid)
                {
                    location.Append(sep);
                    location.Append("endkey_docid=");
                    location.Append(_endkey_docid);
                    sep = '&';
                }
                return location.ToString();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public IEnumerator<TEntity> GetEnumerator()
            {
                return (_cachedResult ?? Execute()).Rows.Select(x => x.Entity).GetEnumerator();
            }

            public Spec Next()
            {
                // TODO: implement key+docid paging: http://wiki.apache.org/couchdb/How_to_page_through_results

                // Since emit() will write multiple rows with the same key+docid, I don't see
                // how you can make any claim that this pagination method works. This holds
                // especially true in this case, where we are a framework library.

                // Perhaps a map() method that outputs rows like that is just such bad practice that we can
                // allow pagination to fail if someone tries it? Holding off on a decision until I have
                // some better evidence and more real experience with views.

                var page = (Spec)MemberwiseClone();
                page._skip = (page._skip ?? 0) + (page._limit ?? 10);
                return page;
            }
        }
    }
}
