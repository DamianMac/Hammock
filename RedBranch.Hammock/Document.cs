//
//  Document.cs
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

using Newtonsoft.Json;

namespace RedBranch.Hammock
{
    public class Document
    {
        [JsonIgnore] public Session Session { get; set; }
        [JsonIgnore] public string Id { get; set; }
        [JsonIgnore] public string Revision { get; set; }

        [JsonIgnore] public string Location
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
            return (Id ?? "/").GetHashCode() ^ (Revision ?? "-").GetHashCode();
        }

        public override bool Equals(object obj)
        {
            var d = obj as Document;
            return null == d
                       ? base.Equals(obj)
                       : d.Id == Id &&
                         d.Revision == Revision;
        }

        public static string Prefix<TEntity>()
        {
            return typeof (TEntity).Name.ToLowerInvariant();
        }

        public static string For<TEntity>(string withId)
        {
            return string.Format("{0}-{1}", Prefix<TEntity>(), withId);
        }
    }

    public interface IHasDocument
    {
        [JsonIgnore]
        Document Document { get; set; }
    }
}