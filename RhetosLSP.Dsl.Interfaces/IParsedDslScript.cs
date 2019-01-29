using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Rhetos.Dsl;
using RhetosLSP.Contracts;
using RhetosLSP.Utilities;

namespace RhetosLSP.Dsl
{
    public interface IParsedDslScript
    {
        void UpdateDocument(ICollection<TextDocumentContentChangeEvent> contentChanges);

        Task<bool> IsKeywordAtPositionAsync(int line, int column);

        Task<WordOnHover> GetWordOnPositionAsync(int line, int column);

        Task<IConceptInfo> GetContextAtPositionAsync(int line, int column);

        Task<WordOnHover> GetWordSignatureHelpOnPositionAsync(int line, int column);
    }
}
