﻿using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JsonRpc.Standard.Contracts;
using RhetosLSP.Contracts;
using Rhetos.Logging;
using RhetosLSP.Dsl;
using System;
using RhetosLSP.Utilities;

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
            WordOnHover wordOverHover = await _parsedDslScriptProvider.GetScriptOnPath(textDocument.Uri).GetWordOnPositionAsync(position.Line, position.Character);

            var foundConcept = _conceptsInfoMetadata.Metadata.FirstOrDefault(cim => cim.Keyword == wordOverHover.Word);
            if (foundConcept != null && foundConcept.Documentation != null)
            {
                return new Hover {
                    Contents = foundConcept.Documentation.ConceptSummary,
                    Range = new Range(wordOverHover.Start, wordOverHover.End)
                };
            }

            return new Hover
            {
                Contents = wordOverHover.Word,
                Range = new Range(wordOverHover.Start, wordOverHover.End)
            };
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
            _parsedDslScriptProvider.AddScript(textDocument);
            var diag = Session.DiagnosticProvider.LintDocument(doc.Document, Session.Settings.MaxNumberOfProblems);
            await Client.Document.PublishDiagnostics(textDocument.Uri, diag);
        }

        [JsonRpcMethod(IsNotification = true)]
        public void DidChange(TextDocumentIdentifier textDocument,
            ICollection<TextDocumentContentChangeEvent> contentChanges)
        {
            Session.Documents[textDocument.Uri].NotifyChanges(contentChanges);
            _parsedDslScriptProvider.GetScriptOnPath(textDocument.Uri).UpdateDocument(contentChanges);
        }

        [JsonRpcMethod(IsNotification = true)]
        public async Task DidClose(TextDocumentIdentifier textDocument)
        {
            if (textDocument.Uri.IsUntitled())
            {
                await Client.Document.PublishDiagnostics(textDocument.Uri, new Diagnostic[0]);
            }
            Session.Documents.TryRemove(textDocument.Uri, out _);
            _parsedDslScriptProvider.TryRemoveScript(textDocument.Uri);
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

                List<CompletionItem> completionsList = new List<CompletionItem>();
                foreach(var conceptInfo in conceptsInfoMetadataToApply)
                {
                    if(!completionsList.Exists(x => x.Label == conceptInfo.Keyword))
                    {
                        int numberOverload = conceptsInfoMetadataToApply.Where(concept => concept.Keyword == conceptInfo.Keyword).Count();
                        completionsList.Add(new CompletionItem
                        {
                            Label = conceptInfo.Keyword,
                            Kind = CompletionItemKind.Keyword,
                            Detail = conceptInfo.GetUserDescription(false, numberOverload),
                            CommitCharacters = Constants.CommitCharacters,
                            Documentation = conceptInfo.Documentation != null ? conceptInfo.Documentation.ConceptSummary : ""
                        });
                    }
                }

                return new CompletionList(completionsList, false);
            }
            else
            {
                return null;
            }
        }

        [JsonRpcMethod]
        public async Task<SignatureHelp> SignatureHelp(TextDocumentIdentifier textDocument, Position position, CancellationToken ct)
        {
            var parsedScript = _parsedDslScriptProvider.GetScriptOnPath(textDocument.Uri);
            var foundWord = await parsedScript.GetWordSignatureHelpOnPositionAsync(position.Line, position.Character);
            List<SignatureInformation> signatures = new List<SignatureInformation>();
            if (foundWord != null)
            {
                IEnumerable<ConceptInfoMetadata> conceptsInfoMetadata = _conceptsInfoMetadata
                    .Metadata
                    .Where(x => !string.IsNullOrEmpty(x.Keyword) && x.Keyword.Equals(foundWord.Word));
                foreach(var conceptInfo in conceptsInfoMetadata)
                {
                    signatures.Add(conceptInfo.GetSignatureInformation(false));
                }
            }
            return new SignatureHelp
            {
                Signatures = signatures,
                ActiveParameter = foundWord == null ? 0 : foundWord.ActiveParameter
            };
        }
    }
}
