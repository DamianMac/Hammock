using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace Relax.Design
{
    public class View
    {
        [JsonProperty("map")] public string Map { get; set; }
        [JsonProperty("reduce")] public string Reduce { get; set; }
    }
}
