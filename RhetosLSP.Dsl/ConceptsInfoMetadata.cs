using Rhetos.Dsl;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RhetosLSP.Dsl
{
    public class ConceptsInfoMetadata : IConceptsInfoMetadata
    {
        private readonly IEnumerable<Type> _conceptTypes;

        public List<string> ConceptKeywords { get; set; }

        public List<ConceptInfoMetadata> Metadata { get; private set;}

        public ConceptsInfoMetadata(
            IEnumerable<IConceptInfo> conceptPrototypes, ConceptDescriptionProvider conceptDescriptionProvider)
        {
            _conceptTypes = conceptPrototypes.Select(conceptInfo => conceptInfo.GetType());
            ConceptKeywords = _conceptTypes.Select(x => ConceptInfoHelper.GetKeyword(x)).Distinct().ToList();
            Metadata = new List<ConceptInfoMetadata>();

            foreach (var conceptType in _conceptTypes)
            {

                var members = ConceptMembers.Get(conceptType);
                ConceptInfoDocumentation documentation = null;
                conceptDescriptionProvider.ConceptInfoDescriptions.TryGetValue(conceptType, out documentation);
                Metadata.Add(new ConceptInfoMetadata( conceptType, documentation));
            }
        }
    }
}
