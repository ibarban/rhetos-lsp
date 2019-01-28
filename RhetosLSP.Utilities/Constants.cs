using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RhetosLSP.Utilities
{
    public static class Constants
    {
        public static readonly char[] CommitCharacters = new char[] { ' ', '{' };
        public static readonly char[] StopCharacters = new char[] { '\n', ' ', '\r', ';', '{', '}' };
    }
}
