using Rhetos.Dsl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RhetosLSP.Dsl
{
    public interface IParsedDslScriptProvider
    {
        IParsedDslScript GetScriptOnPath(string path);

        void UpdateScript(string path, string script);
    }
}
