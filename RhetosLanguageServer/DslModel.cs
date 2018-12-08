using Rhetos.Dsl;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RhetosLanguageServer
{
    public class DslModel
    {
        private readonly IEnumerable<Type> _conceptTypes;

        private readonly List<ConceptInfoDocumentation> _conceptDescriptionProvider;

        private List<ConceptInfoMetadata> _conceptsInfoMetadata;

        public List<string> ConceptKeywords { get; set; }

        public List<ConceptInfoMetadata> ConceptsInfoMetadata { get; private set;}

        public DslModel(
            IEnumerable<IConceptInfo> conceptPrototypes, ConceptDescriptionProvider conceptDescriptionProvider)
        {
            _conceptTypes = conceptPrototypes.Select(conceptInfo => conceptInfo.GetType());
            _conceptDescriptionProvider = conceptDescriptionProvider.ConceptInfoDescriptions;
            ConceptKeywords = _conceptTypes.Select(x => ConceptInfoHelper.GetKeyword(x)).Distinct().ToList();

            ConceptsInfoMetadata = _conceptTypes.Select(x => new ConceptInfoMetadata
            {
                Type = x,
                Keyword = ConceptInfoHelper.GetKeyword(x),
                Documentation = conceptDescriptionProvider.ConceptInfoDescriptions.FirstOrDefault(y => y.ConceptType == x)
            }).ToList();
        }
    }

    public class ConceptInfoMetadata
    {
        public Type Type { get; set; }

        public string Keyword { get; set; }

        public ConceptInfoDocumentation Documentation { get; set; }
    }
}
