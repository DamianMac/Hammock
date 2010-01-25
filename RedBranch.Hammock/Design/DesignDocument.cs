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
