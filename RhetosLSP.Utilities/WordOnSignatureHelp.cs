using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RhetosLSP.Utilities
{
    public class WordOnSignatureHelp
    {
        public string Word { get; set; }
        
        public int ActiveParameter { get; set; }

        public WordOnSignatureHelp(string word, int activeParameter)
        {
            Word = word;
            ActiveParameter = activeParameter;
        }

        public WordOnSignatureHelp(string word) : this(word, 0)
        {
        }
    }
}
