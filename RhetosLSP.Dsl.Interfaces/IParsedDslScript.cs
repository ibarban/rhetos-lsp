using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Rhetos.Dsl;
using RhetosLSP.Contracts;

namespace RhetosLSP.Dsl
{
    public interface IParsedDslScript
    {
        void UpdateDocument(ICollection<TextDocumentContentChangeEvent> contentChanges);

        Task<bool> IsKeywordAtPositionAsync(int line, int column);

        Task<string> GetWordOnPositionAsync(int line, int column);

        Task<IConceptInfo> GetContextAtPositionAsync(int line, int column);
    }
}
