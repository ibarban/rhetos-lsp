using Rhetos.Dsl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RhetosLanguageServer
{
    public static class ContentTokenizer
    {
        public static List<Token> TokenizeContent(string content)
        {
            var tokenizer = new RhetosLSPUtilities.Tokenizer(new DslScriptProviderFromContent(content));
            return tokenizer.GetTokens();
        }

        public class DslScriptProviderFromContent : IDslScriptsProvider
        {
            public IEnumerable<DslScript> DslScripts { get; private set; }

            public DslScriptProviderFromContent(string content)
            {
                DslScripts = new List<DslScript> {
                    new DslScript {
                        Name  = "",
                        Script = content,
                        Path = ""
                    }
                };
            }
        }
    }

}
