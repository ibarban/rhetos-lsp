using Rhetos.Dsl;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RhetosLanguageServer
{
    public class DslModel
    {
        private readonly IEnumerable<Type> _conceptTypes;

        private List<ConceptInfoMetadata> _conceptsInfoMetadata;

        public List<string> ConceptKeywords { get; set; }

        public List<ConceptInfoMetadata> ConceptsInfoMetadata { get; private set;}

        public DslModel(
            IEnumerable<IConceptInfo> conceptPrototypes, ConceptDescriptionProvider conceptDescriptionProvider)
        {
            _conceptTypes = conceptPrototypes.Select(conceptInfo => conceptInfo.GetType());
            ConceptKeywords = _conceptTypes.Select(x => ConceptInfoHelper.GetKeyword(x)).Distinct().ToList();
            ConceptsInfoMetadata = new List<ConceptInfoMetadata>();

            foreach (var conceptType in _conceptTypes)
            {
                ConceptInfoDocumentation documentation = null;
                conceptDescriptionProvider.ConceptInfoDescriptions.TryGetValue(conceptType, out documentation);
                ConceptsInfoMetadata.Add(new ConceptInfoMetadata
                {
                    Type = conceptType,
                    Keyword = ConceptInfoHelper.GetKeyword(conceptType),
                    Documentation = documentation
                });
            }
        }
    }

    public class ConceptInfoMetadata
    {
        public Type Type { get; set; }

        public string Keyword { get; set; }

        public ConceptInfoDocumentation Documentation { get; set; }
    }
}
