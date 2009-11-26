using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace RedBranch.Hammock.Design
{
    public class DesignDocument
    {
        [JsonProperty("language")] public string Language { get; set; }
        [JsonProperty("views")] public IDictionary<string, View> Views { get; set; }
    }
}
