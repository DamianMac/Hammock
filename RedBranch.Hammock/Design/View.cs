// 
//  View.cs
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
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace RedBranch.Hammock.Design
{
    public class View
    {
        [JsonProperty("map")] public string Map { get; set; }
        [JsonProperty("reduce")] public string Reduce { get; set; }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof (View)) return false;
            return Equals((View) obj);
        }

        public bool Equals(View other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(other.Map, Map) && Equals(other.Reduce, Reduce);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Map != null ? Map.GetHashCode() : 0)*397) ^
                       (Reduce != null ? Reduce.GetHashCode() : 0);
            }
        }

        public static string BasicMap<TEntity>(string inner)
        {
            return String.Format(
@"function(doc) {{
  if (doc._id.indexOf('{0}-') === 0) {{
    {1}
  }}
}}",
  Document.Prefix<TEntity>(),
  inner);
        }
    }
}
