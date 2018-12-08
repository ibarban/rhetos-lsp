﻿using System;
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

namespace RhetosLanguageServer.Services
{
    [JsonRpcScope(MethodPrefix = "textDocument/")]
    public class TextDocumentService : RhetosLanguageServiceBase
    {
        private static List<string> Keywords = new List<string>();

        private readonly DslModel _dslModel;

        public TextDocumentService(DslModel dslModel)
        {
            _dslModel = dslModel;
        }

        [JsonRpcMethod]
        public async Task<Hover> Hover(TextDocumentIdentifier textDocument, Position position, CancellationToken ct)
        {
            // Note that Hover is cancellable.
            await Task.Delay(1000, ct);
            return new Hover {Contents = "Test _hover_ @" + position + "\n\n" + textDocument};
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
                var doc1 = ((SessionDocument) sender).Document;
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

           /* if (IsCurrentPositionAKeyword(tokens, GetPositionInString(content, position)))
            {*/
                return new CompletionList(_dslModel.ConceptKeywords.Select(x => new CompletionItem { Label = x, Kind = CompletionItemKind.Keyword, Detail = "No details" }));
            /*}
            else
            {
                return new CompletionList();
            }*/
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

        public bool IsCurrentPositionAKeyword(List<Token> tokens, int position)
        {
            if (tokens.Count <= 2)
                return true;

            var curentTokenIndex = -1;
            for (int i = tokens.Count - 1; i >= 0; i--)
            {
                var token = tokens[i];
                if (token.PositionInDslScript <= position)
                {
                    curentTokenIndex = i;
                    break;
                }
            }

            if (curentTokenIndex < 1)
                return false;

            var tokenBeforeIndex = curentTokenIndex - 1;

            if (tokens[tokenBeforeIndex].Type == TokenType.Special &&
                position <= tokens[curentTokenIndex].PositionInDslScript + tokens[curentTokenIndex].Value.Length)
                return true;
            else
                return false;

        }
    }
}
