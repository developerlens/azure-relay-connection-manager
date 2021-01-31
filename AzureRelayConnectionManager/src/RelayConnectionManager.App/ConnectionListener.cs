using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RelayConnectionManager.App
{
    public class ConnectionListener
    {
        public string KeyName { get; set; }
        public string Key { get; set; }
        public string Namespace { get; set; }
        public string Path { get; set; }
        public string TargetUrl { get; set; }
    }
}
