using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhetos.Dsl;

namespace RhetosLSP.Dsl
{
    public class ParsedDslScript : IParsedDslScript
    {
        public IEnumerable<IConceptInfo> ParsedConcepts => throw new NotImplementedException();

        public bool IsKeywordAtPosition(int positionInScript)
        {
            throw new NotImplementedException();
        }
    }
}
