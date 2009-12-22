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
