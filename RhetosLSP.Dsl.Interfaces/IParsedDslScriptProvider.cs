using System;

namespace RhetosLSP.Dsl
{
    public interface IParsedDslScriptProvider
    {
        IParsedDslScript GetScriptOnPath(Uri path);

        void UpdateScript(Uri path, string script);
    }
}
