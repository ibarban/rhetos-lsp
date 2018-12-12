using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RhetosLSP.Extensibility
{
    internal class PluginInfo
    {
        public Type Type;
        public Dictionary<string, object> Metadata;
    }
}
