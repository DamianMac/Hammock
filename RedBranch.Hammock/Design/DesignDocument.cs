//
//  DesignDocument.cs
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
    public class DesignDocument
    {
        private IDictionary<string, View> _views;

        [JsonProperty("language")] public string Language { get; set; }
        [JsonProperty("views")]
        public IDictionary<string, View> Views
        {
            get { return _views ?? (_views = new Dictionary<string, View>()); }
            set { _views = value; }
        }
    }
}
