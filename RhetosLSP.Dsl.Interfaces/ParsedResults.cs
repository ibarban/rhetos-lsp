using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RhetosLSP.Dsl
{
    public class ParsedResults
    {
        public List<ConceptInfoLSP> Concepts;
        public List<ParserError> Errors;
    }

    public class ParserError
    {
        public ConceptInfoLocation Location;
        public string Error;
    }
}
