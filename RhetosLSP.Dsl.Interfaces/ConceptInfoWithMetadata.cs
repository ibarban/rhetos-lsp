using Rhetos.Dsl;

namespace RhetosLSP.Dsl
{
    public class ConceptInfoWithMetadata
    {
        public IConceptInfo Concept { get; set; }
        public LocationInScript Location { get; set; }
    }
}