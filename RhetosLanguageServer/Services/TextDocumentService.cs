using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JsonRpc.Standard.Contracts;
using RhetosLSP.Contracts;
using Rhetos.Logging;
using RhetosLSP.Dsl;
using System;

namespace RhetosLanguageServer
{
    [JsonRpcScope(MethodPrefix = "textDocument/")]
    public class TextDocumentService : RhetosLanguageServiceBase
    {
        private readonly IConceptsInfoMetadata _conceptsInfoMetadata;

        private readonly IParsedDslScriptProvider _parsedDslScriptProvider;

        private readonly ILogger _parsingLogger;

        private readonly DslParser _dslParser;

        public TextDocumentService(ILogProvider logProvider, IConceptsInfoMetadata conceptsInfoMetadata, DslParser dslParser, IParsedDslScriptProvider parsedDslScriptProvider)
        {
            _conceptsInfoMetadata = conceptsInfoMetadata;
            _parsingLogger = logProvider.GetLogger("TextDocumentService");
            _dslParser = dslParser;
            _parsedDslScriptProvider = parsedDslScriptProvider;
        }

        [JsonRpcMethod]
        public async Task<Hover> Hover(TextDocumentIdentifier textDocument, Position position, CancellationToken ct)
        {
            string wordOverHover = _parsedDslScriptProvider.GetScriptOnPath(textDocument.Uri).GetWordOnPositionAsync(position.Line, position.Character).Result;

            await Task.Delay(500, ct);

            var foundConcept = _conceptsInfoMetadata.Metadata.FirstOrDefault(cim => cim.Keyword == wordOverHover);
            if (foundConcept != null && foundConcept.Documentation != null)
            {
                return new Hover { Contents = foundConcept.Documentation.ConceptSummary};
            }

            return new Hover { Contents = wordOverHover};
        }

        [JsonRpcMethod(IsNotification = true)]
        public async Task DidOpen(TextDocumentItem textDocument)
        {
            var doc = new SessionDocument(textDocument);
            var session = Session;
            doc.DocumentChanged += async (sender, args) =>
            {
                // Lint the document when it's changed.
                var doc1 = ((SessionDocument)sender).Document;
                var diag1 = session.DiagnosticProvider.LintDocument(doc1, session.Settings.MaxNumberOfProblems);
                if (session.Documents.ContainsKey(doc1.Uri))
                {
                    // In case the document has been closed when we were linting…
                    await session.Client.Document.PublishDiagnostics(doc1.Uri, diag1);
                }
            };
            Session.Documents.TryAdd(textDocument.Uri, doc);
            _parsedDslScriptProvider.UpdateScript(textDocument.Uri, Session.Documents[textDocument.Uri].Document.Content);
            var diag = Session.DiagnosticProvider.LintDocument(doc.Document, Session.Settings.MaxNumberOfProblems);
            await Client.Document.PublishDiagnostics(textDocument.Uri, diag);
        }

        [JsonRpcMethod(IsNotification = true)]
        public void DidChange(TextDocumentIdentifier textDocument,
            ICollection<TextDocumentContentChangeEvent> contentChanges)
        {
            Session.Documents[textDocument.Uri].NotifyChanges(contentChanges);
            _parsedDslScriptProvider.UpdateScript(textDocument.Uri, Session.Documents[textDocument.Uri].Document.Content);
        }

        [JsonRpcMethod(IsNotification = true)]
        public async Task DidClose(TextDocumentIdentifier textDocument)
        {
            if (textDocument.Uri.IsUntitled())
            {
                await Client.Document.PublishDiagnostics(textDocument.Uri, new Diagnostic[0]);
            }
            Session.Documents.TryRemove(textDocument.Uri, out _);
        }

        [JsonRpcMethod]
        public CompletionList Completion(TextDocumentIdentifier textDocument, Position position)
        {
            var parsedScript = _parsedDslScriptProvider.GetScriptOnPath(textDocument.Uri);

            if (parsedScript.IsKeywordAtPositionAsync(position.Line, position.Character).Result)
            {
                var context = parsedScript.GetContextAtPositionAsync(position.Line, position.Character).Result;
                IEnumerable<ConceptInfoMetadata> conceptsInfoMetadataToApply = _conceptsInfoMetadata.Metadata.Where(x => !string.IsNullOrEmpty(x.Keyword));
                if (context != null)
                    conceptsInfoMetadataToApply = conceptsInfoMetadataToApply.Where(x => x.Members.Count > 0 && x.Members[0].IsConceptInfo && x.Members[0].ValueType.IsAssignableFrom(context.GetType()));
                else
                    conceptsInfoMetadataToApply = conceptsInfoMetadataToApply.Where(x => x.Members.Count > 0 && !x.Members[0].IsConceptInfo);
                return new CompletionList(conceptsInfoMetadataToApply.Select(x => new CompletionItem { Label = x.Keyword, Kind = CompletionItemKind.Keyword, Detail = x.GetUserDescription(false) }));
            }
            else
            {
                return new CompletionList();
            }
        }
    }
}
