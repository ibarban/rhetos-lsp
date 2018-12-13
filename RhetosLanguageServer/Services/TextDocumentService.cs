using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JsonRpc.Standard.Contracts;
using LanguageServer.VsCode;
using LanguageServer.VsCode.Contracts;
using Rhetos.Logging;
using RhetosLSP.Dsl;

namespace RhetosLanguageServer.Services
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
            string wordOverHover = _parsedDslScriptProvider.GetScriptOnPath(textDocument.Uri).GetWordOnPosition(position.Line, position.Character);

            await Task.Delay(500, ct);

            var foundConcept = _conceptsInfoMetadata.Metadata.FirstOrDefault(cim => cim.Keyword == wordOverHover);
            if (foundConcept != null && foundConcept.Documentation != null)
            {
                return new Hover { Contents = foundConcept.Documentation.ConceptSummary};
            }

            return new Hover { Contents = wordOverHover};
        }
        
        [JsonRpcMethod]
        public SignatureHelp SignatureHelp(TextDocumentIdentifier textDocument, Position position)
        {
            return new SignatureHelp(new List<SignatureInformation>
            {
                new SignatureInformation("**Function1**", "Documentation1"),
                new SignatureInformation("**Function2** <strong>test</strong>", "Documentation2"),
            });
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
        public void WillSave(TextDocumentIdentifier textDocument, TextDocumentSaveReason reason)
        {
            //Client.Window.LogMessage(MessageType.Log, "-----------");
            //Client.Window.LogMessage(MessageType.Log, Documents[textDocument].Content);
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
            if (parsedScript.IsKeywordAtPosition(position.Line, position.Character))
                return new CompletionList(_conceptsInfoMetadata.Metadata.Where(x => !string.IsNullOrEmpty(x.Keyword)).Select(x => new CompletionItem { Label = x.Keyword, Kind = CompletionItemKind.Keyword, Detail = x.GetUserDescription(false) }));
            else
                return new CompletionList();
        }
    }
}
