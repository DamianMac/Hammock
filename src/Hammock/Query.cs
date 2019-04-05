// 
//  Query.cs
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
    public partial class Query<TEntity> where TEntity : class
    {
        public Session Session { get; private set; }    
        public string Design { get; private set; }
        public string View { get; private set; }
        public bool Group { get; private set; }

        public virtual string Location
        {
            get
            {
                var location = new StringBuilder(
                    Session.Connection.GetDatabaseLocation(
                        Session.Database));

                location.Append("_design/");
                location.Append(Design);
                location.Append("/_view/");
                location.Append(View);

                return location.ToString();
            }
        }

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

    public class AllDocumentsQuery : Query<JToken>
    {
        public AllDocumentsQuery(Session sx) : base(sx, null, null, false)
        {
        }

        public override string Location
        {
            get
            {
                var location = new StringBuilder(
                    Session.Connection.GetDatabaseLocation(
                        Session.Database));

                location.Append("_all_docs");

                return location.ToString();
            }
        }
    }
}
