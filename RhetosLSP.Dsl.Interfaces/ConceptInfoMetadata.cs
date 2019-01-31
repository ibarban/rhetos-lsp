using Rhetos.Dsl;
using System;
using System.Collections.Generic;
using System.Linq;
using RhetosLSP.Contracts;
using System.Text;

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

        public string GetUserDescription(bool includeParentConcept, int numberOverload = 1)
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

            var propertiesSummary = propertyDescriptions.Any() ? "Properties: " + string.Join(", ", propertyDescriptions.ToArray()) : "";
            var summary = string.Format(@"{0} (+ {1} overload(s))", propertiesSummary, numberOverload);
            return summary;
        }

        public SignatureInformation GetSignatureInformation(bool includeParentConcept)
        {
            var members = !includeParentConcept ? Members.Skip(1) : Members;
            var memberTexts = members.Select(x => {
                StringBuilder result = new StringBuilder();
                string format = "<{0}:{1}>";
                if (x.IsConceptInfo)
                {
                    var keywordOrType = ConceptInfoHelper.GetKeywordOrTypeName(x.ValueType);
                    result.AppendFormat(format, x.Name, keywordOrType);
                }
                else
                {
                    result.AppendFormat(format, x.Name, x.ValueType.Name);
                }
                return result.ToString();
            });
            string usage = memberTexts.Count() > 0
                ? string.Format("{0} {1}", Keyword, string.Join(" ", memberTexts))
                : Keyword;
            ParameterInformation parameter = new ParameterInformation
            {
                Label = "",
                Documentation = usage
            };

            return new SignatureInformation
            {
                Label = Keyword,
                Documentation = Documentation != null ? Documentation.ConceptSummary : "",
                Parameters = new List<ParameterInformation> { parameter }
            };
        }
    }
}
