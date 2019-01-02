using Rhetos.Dsl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RhetosLSP.Dsl.Test
{
    public class GenericParserHelper<TConceptInfo> : GenericParser where TConceptInfo : IConceptInfo, new()
    {
        public TokenReader tokenReader;

        public GenericParserHelper(string keyword)
            : base(typeof(TConceptInfo), keyword)
        {
        }
    }
}
