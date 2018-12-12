using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RhetosLSP.Dsl
{
    public class ParsedDslScriptProvider : IParsedDslScriptProvider
    {
        public IParsedDslScript GetScriptOnPath(string path)
        {
            throw new NotImplementedException();
        }

        public void UpdateScript(string path, string script)
        {
            throw new NotImplementedException();
        }
    }
}
