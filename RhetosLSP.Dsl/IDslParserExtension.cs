using System.Collections.Generic;
using Rhetos.Dsl;

namespace RhetosLSP.Dsl.Interfaces
{
    public interface IDslParserExtension : IDslParser
    {
        IEnumerable<ParserError> Errors { get; }

        IEnumerable<ConceptInfoMetadata> GetConceptsInScript(DslScript dslScript);
    }
}
