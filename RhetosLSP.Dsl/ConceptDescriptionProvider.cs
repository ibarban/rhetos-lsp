using Rhetos.Dsl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Xml;

namespace RhetosLSP.Dsl
{
    public class ConceptDescriptionProvider
    {
        private readonly IEnumerable<Type> _conceptTypes;

        private readonly IPluginFolderProvider _rhetosProjectConfiguration;

        public Dictionary<Type, ConceptInfoDocumentation> ConceptInfoDescriptions { get; private set; }

        public ConceptDescriptionProvider(IPluginFolderProvider rhetosProjectConfiguration, IEnumerable<IConceptInfo> conceptPrototypes)
        {
            _conceptTypes = conceptPrototypes.Select(conceptInfo => conceptInfo.GetType());
            _rhetosProjectConfiguration = rhetosProjectConfiguration;

            ConceptInfoDescriptions = new Dictionary<Type, ConceptInfoDocumentation> ();

            var assemblyDocumentations = _conceptTypes.Select(x => x.Assembly.Location.Replace(".dll", ".xml")).Distinct();
            var conceptTypesInSummary = _conceptTypes.Select(x => x.FullName).ToList();

            foreach (var assemblyDocumentation in assemblyDocumentations)
            {
                if (File.Exists(assemblyDocumentation))
                {
                    XmlDocument doc = new XmlDocument();
                    doc.Load(assemblyDocumentation);
                    foreach (XmlNode member in doc.DocumentElement.SelectNodes("/doc/members/member"))
                    {
                        var summrayName = member.Attributes.GetNamedItem("name").Value;
                        var splits = summrayName.Split(':');
                        var memberType = splits[0];
                        if (memberType == "T" && conceptTypesInSummary.Contains(splits[1]))
                        {
                            var type = _conceptTypes.FirstOrDefault(x => x.FullName == splits[1]);
                            if (type != null)
                            {
                                var summeryContent = member.SelectSingleNode("summary").InnerText;
                                ConceptInfoDescriptions.Add(type, new ConceptInfoDocumentation
                                {
                                    ConceptSummary = summeryContent.Trim()
                                });
                            }
                        }
                    }
                }
            }
        }
    }
}
