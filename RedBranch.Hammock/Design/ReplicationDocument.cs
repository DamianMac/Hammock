// 
//  ReplicationDocument.cs
//  
//  Author:
//       Eddie Dillon <eddie.d.2000@gmail.com>
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
using Newtonsoft.Json.Linq;

namespace RedBranch.Hammock.Design
{
    public class ReplicationDocument
    {
        [JsonProperty("source")] public string Source { get; set; }
        [JsonProperty("target")] public string Target { get; set; }
        [JsonProperty("continuous")] public bool Continuous { get; set; }
        [JsonProperty("create_target")] public bool CreateTarget { get; set; }
        [JsonProperty("filter")] public string Filter { get; set; }
        [JsonProperty("query_params")] public JObject QueryParams { get; set; }
    }
}
