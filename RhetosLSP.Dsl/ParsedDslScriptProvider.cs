using RhetosLSP.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RhetosLSP.Dsl
{
    public class ParsedDslScriptProvider : IParsedDslScriptProvider
    {
        private readonly DslParser _dslParser;

        Dictionary<Uri, ParsedDslScript> _parsedScripts;

        public ParsedDslScriptProvider(DslParser dslParser)
        {
            _dslParser = dslParser;
            _parsedScripts = new Dictionary<Uri, ParsedDslScript>();
        }

        public IParsedDslScript GetScriptOnPath(Uri path)
        {
            ParsedDslScript parsedScript = null;
            _parsedScripts.TryGetValue(path, out parsedScript);
            return parsedScript;
        }

        public void AddScript(TextDocumentItem textDocumentItem)
        {
            _parsedScripts.Add(textDocumentItem.Uri, new ParsedDslScript(textDocumentItem, _dslParser));
        }

        public void TryRemoveScript(Uri path)
        {
            if(_parsedScripts.ContainsKey(path))
                _parsedScripts.Remove(path);
        }
    }
}
