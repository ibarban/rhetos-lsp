using System;
using System.Collections.Generic;
using Rhetos.Dsl;

namespace RhetosLSP.Dsl
{
    public interface IParsedDslScript
    {
        IEnumerable<ConceptInfoLSP> ParsedConcepts { get; }

        bool IsKeywordAtPosition(int line, int column);

        string GetWordOnPosition(int line, int column);
    }
}
