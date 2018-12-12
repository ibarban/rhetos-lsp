using System;
using System.Collections.Generic;
using Rhetos.Dsl;

namespace RhetosLSP.Dsl
{
    public interface IParsedDslScript
    {
        IEnumerable<IConceptInfo> ParsedConcepts { get; }

        bool IsKeywordAtPosition(int positionInScript);
    }
}
