using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JsonRpc.Standard.Contracts;
using JsonRpc.Standard.Server;
using LanguageServer.VsCode;
using LanguageServer.VsCode.Contracts;
using LanguageServer.VsCode.Server;
using Rhetos.Dsl;
using Rhetos.Logging;

using Token = Rhetos.Dsl.Token;
using TokenType = Rhetos.Dsl.TokenType;
using DslParser = RhetosLSP.Utilities.DslParser;

namespace RhetosLanguageServer.Services
{
    [JsonRpcScope(MethodPrefix = "textDocument/")]
    public class TextDocumentService : RhetosLanguageServiceBase
    {
        private static List<string> Keywords = new List<string>();

        private readonly DslModel _dslModel;

        ILogger _tokenLogger;

        private readonly DslParser _dslParser;

        public TextDocumentService(ILogProvider logProvider, DslModel dslModel, DslParser dslParser)
        {
            _dslModel = dslModel;
            _tokenLogger = logProvider.GetLogger("Token parser");
            _dslParser = dslParser;
        }

        [JsonRpcMethod]
        public async Task<Hover> Hover(TextDocumentIdentifier textDocument, Position position, CancellationToken ct)
        {
            // Note that Hover is cancellable.
            await Task.Delay(1000, ct);
            return new Hover { Contents = "Test _hover_ @" + position + "\n\n" + textDocument };
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
            var diag = Session.DiagnosticProvider.LintDocument(doc.Document, Session.Settings.MaxNumberOfProblems);
            await Client.Document.PublishDiagnostics(textDocument.Uri, diag);
        }

        [JsonRpcMethod(IsNotification = true)]
        public void DidChange(TextDocumentIdentifier textDocument,
            ICollection<TextDocumentContentChangeEvent> contentChanges)
        {
            Session.Documents[textDocument.Uri].NotifyChanges(contentChanges);
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

        private static readonly CompletionItem[] PredefinedCompletionItems =
        {
            new CompletionItem(".NET", CompletionItemKind.Keyword,
                "Keyword1",
                "Short for **.NET Framework**, a software framework by Microsoft (possibly its subsets) or later open source .NET Core.",
                null),
            new CompletionItem(".NET Standard", CompletionItemKind.Keyword,
                "Keyword2",
                "The .NET Standard is a formal specification of .NET APIs that are intended to be available on all .NET runtimes.",
                null),
            new CompletionItem(".NET Framework", CompletionItemKind.Keyword,
                "Keyword3",
                ".NET Framework (pronounced dot net) is a software framework developed by Microsoft that runs primarily on Microsoft Windows.", null),
        };

        [JsonRpcMethod]
        public CompletionList Completion(TextDocumentIdentifier textDocument, Position position)
        {
            var doc = Session.Documents[textDocument.Uri];
            var content = doc.Document.Content;
            var tokens = ContentTokenizer.TokenizeContent(content);
            var concepts = _dslParser.Parse(tokens);
            Client.Window.ShowMessage(MessageType.Info, string.Join(", ", concepts.Select(x => x.GetShortDescription())));
            if (IsCurrentPositionAKeyword(content, GetPositionInString(content, position)))
            {
                var a = _dslModel.ConceptsInfoMetadata.Where(x => !string.IsNullOrEmpty(x.Keyword)).Select(x => new CompletionItem { Label = x.Keyword, Kind = CompletionItemKind.Keyword, Detail = x.Documentation?.ConceptSummary });
                return new CompletionList(_dslModel.ConceptsInfoMetadata.Where(x => !string.IsNullOrEmpty(x.Keyword)).Select(x => new CompletionItem { Label = x.Keyword, Kind = CompletionItemKind.Keyword, Detail = x.GetUserDescription(false) }));
            }
            else
            {
                return new CompletionList();
            }
        }

        public int GetPositionInString(string content, Position position)
        {
            var lineIndex = 0;
            var line = 0;
            while (line < position.Line && lineIndex != -1)
            {
                line = line + 1;
                lineIndex = content.IndexOf("\n", lineIndex + 1);
            }

            if (line != position.Line)
                return -1;
            else
                return lineIndex + position.Character;
        }

        public bool IsCurrentPositionAKeyword(string content, int position)
        {
            char[] specialChars = { ';', '}', '{' };
            char[] charactersToIgnore = { '\n', ' ', '\r' };
            var contentAsCharArray = content.ToCharArray();
            
            for (int i = position; i > 0; i--)
            {
                var currentChar = contentAsCharArray[i];

                if (charactersToIgnore.Contains(currentChar))
                    continue;
                if (!charactersToIgnore.Contains(currentChar) && !specialChars.Contains(currentChar)) //Regular word found
                    return false;
                if (specialChars.Contains(currentChar))
                    return true;
            }

            return false;
        }
    }
}
