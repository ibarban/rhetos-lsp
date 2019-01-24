using RhetosLSP.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RhetosLSP.Utilities
{
    public class WordOnHover
    {
        public string Word { get; set; }

        public Position Start { get; set; }

        public Position End { get; set; }
    }
}
