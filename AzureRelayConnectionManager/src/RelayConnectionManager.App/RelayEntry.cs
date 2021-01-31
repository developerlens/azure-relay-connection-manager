using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RelayConnectionManager.App
{
    public class RelayItem
    {
        [JsonProperty("relayNamespace")]
        public string RelayNamespace { get; set; }

        [JsonProperty("keyName")]
        public string KeyName { get; set; }

        [JsonProperty("key")]
        public string Key { get; set; }

        [JsonProperty("entityPath")]
        public string EntityPath { get; set; }

        [JsonProperty("forwardTo")]
        public string ForwardTo { get; set; }
    }

    public class RelayEntry
    {
        /// <summary>
        /// 
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public List<RelayItem> RelayItems { get; set; }
    }
}
