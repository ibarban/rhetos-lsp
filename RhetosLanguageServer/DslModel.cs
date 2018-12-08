using Rhetos.Dsl;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RhetosLanguageServer
{
    public class DslModel
    {
        private readonly IEnumerable<Type> _conceptTypes;

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

                var members = ConceptMembers.Get(conceptType);
                ConceptInfoDocumentation documentation = null;
                conceptDescriptionProvider.ConceptInfoDescriptions.TryGetValue(conceptType, out documentation);
                ConceptsInfoMetadata.Add(new ConceptInfoMetadata( conceptType, documentation));
            }
        }
    }

    public class ConceptInfoMetadata
    {
        public Type Type { get; private set; }

        public string Keyword { get; private set; }

        public List<ConceptMember> Members { get; private set; }

        public ConceptInfoDocumentation Documentation { get; private set; }

        public ConceptInfoMetadata(Type conceptType, ConceptInfoDocumentation documentation)
        {
            Type = conceptType;
            Keyword = ConceptInfoHelper.GetKeyword(conceptType);
            Members = ConceptMembers.Get(conceptType).ToList();
            Documentation = documentation;
        }

        public string GetUserDescription(bool includeParentConcept)
        {
            var propertyNames = Members.Select(x => x.Name);
            if (!includeParentConcept && Members.Any() && Members.First().IsConceptInfo)
                propertyNames = propertyNames.Skip(1);
            var propertiesSummary = propertyNames.Any() ? "Properties: " + string.Join(" ", propertyNames.ToArray()) + "\n" : "";
            var conceptSummary = Documentation == null ? "" : "Summary: " + Documentation.ConceptSummary;
            return propertiesSummary + conceptSummary;
        }
    }
}
