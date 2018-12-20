using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Rhetos.Dsl;

namespace RhetosLSP.Dsl
{
    public interface IParsedDslScript
    {
        Task<bool> IsKeywordAtPositionAsync(int line, int column);

        Task<string> GetWordOnPositionAsync(int line, int column);

        Task<IConceptInfo> GetContextAtPositionAsync(int line, int column);
    }
}
