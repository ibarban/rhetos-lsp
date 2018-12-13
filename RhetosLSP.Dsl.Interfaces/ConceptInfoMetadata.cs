using Rhetos.Dsl;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RhetosLSP.Dsl
{
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
            var propertyDescriptions = new List<string>();
            bool isFirst = true;
            foreach (var memeber in Members)
            {
                if (isFirst && !includeParentConcept)
                {
                    isFirst = false;
                    continue;
                }

                if (memeber.IsConceptInfo)
                {
                    var keywordOrType = ConceptInfoHelper.GetKeywordOrTypeName(Type);
                    if (memeber.IsKey)
                        propertyDescriptions.Add("Key " + " " + keywordOrType + " " + memeber.Name);
                    else
                        propertyDescriptions.Add(keywordOrType + " " + memeber.Name);
                }
                else
                {
                    if (memeber.IsKey)
                        propertyDescriptions.Add("Key " + " " + memeber.ValueType.Name + " " + memeber.Name);
                    else
                        propertyDescriptions.Add(memeber.ValueType.Name + " " + memeber.Name);
                }
            }

            var propertiesSummary = propertyDescriptions.Any() ? "Properties: " + string.Join(", ", propertyDescriptions.ToArray()) + "\n" : "";
            var conceptSummary = Documentation == null ? "" : "Summary: " + Documentation.ConceptSummary;
            return propertiesSummary + conceptSummary;
        }
    }
}
