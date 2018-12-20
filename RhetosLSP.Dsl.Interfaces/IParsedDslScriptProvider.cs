using RhetosLSP.Contracts;
using System;

namespace RhetosLSP.Dsl
{
    public interface IParsedDslScriptProvider
    {
        IParsedDslScript GetScriptOnPath(Uri path);

        void AddScript(TextDocumentItem textDocumentItem);

        void TryRemoveScript(Uri path);
    }
}
