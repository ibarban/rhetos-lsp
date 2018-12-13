using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RhetosLSP.Dsl
{
    public interface IConceptsInfoMetadata
    {
        List<ConceptInfoMetadata> Metadata { get; }
    }
}
