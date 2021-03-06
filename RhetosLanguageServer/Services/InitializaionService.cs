﻿using JsonRpc.Standard;
using JsonRpc.Standard.Contracts;
using JsonRpc.Standard.Server;
using Newtonsoft.Json.Linq;
using RhetosLSP.Contracts;
using RhetosLSP.Utilities;
using System;
using System.Threading.Tasks;

namespace RhetosLanguageServer
{
    public class InitializaionService : RhetosLanguageServiceBase
    {
        [JsonRpcMethod(AllowExtensionData = true)]
        public InitializeResult Initialize(int processId, Uri rootUri, ClientCapabilities capabilities,
            JToken initializationOptions = null, string trace = null)
        {
            return new InitializeResult(new ServerCapabilities
            {
                HoverProvider = true,
                SignatureHelpProvider = new SignatureHelpOptions(Constants.SignatureHelpTriggerCharacters),
                CompletionProvider = new CompletionOptions(true, "."),
                TextDocumentSync = new TextDocumentSyncOptions
                {
                    OpenClose = true,
                    WillSave = true,
                    Change = TextDocumentSyncKind.Incremental
                },
            });
        }

        [JsonRpcMethod(IsNotification = true)]
        public async Task Initialized()
        {
            await Client.Window.ShowMessage(MessageType.Info, "Rhetos language server initialized.");
        }

        [JsonRpcMethod]
        public void Shutdown()
        {
        }

        [JsonRpcMethod(IsNotification = true)]
        public void Exit()
        {
            Session.StopServer();
        }

        [JsonRpcMethod("$/cancelRequest", IsNotification = true)]
        public void CancelRequest(MessageId id)
        {
            RequestContext.Features.Get<IRequestCancellationFeature>().TryCancel(id);
        }
    }
}