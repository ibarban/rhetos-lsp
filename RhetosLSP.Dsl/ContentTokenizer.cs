using Rhetos.Dsl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RhetosLSP.Dsl
{
    public static class ContentTokenizer
    {
        public static List<Token> TokenizeContent(string content, Uri uri)
        {
            var tokenizer = new RhetosLSPUtilities.Tokenizer(new DslScriptProviderFromContent(content, uri));
            return tokenizer.GetTokens();
        }

        public class DslScriptProviderFromContent : IDslScriptsProvider
        {
            public IEnumerable<DslScript> DslScripts { get; private set; }

            public DslScriptProviderFromContent(string content, Uri uri)
            {
                DslScripts = new List<DslScript> {
                    new DslScript {
                        Name  = uri.Segments.Last(),
                        Script = content,
                        Path = uri.LocalPath
                    }
                };
            }
        }
    }

}
