using System;
using JsonRpc.Standard.Server;
using RhetosLSP.Contracts;
using RhetosLSP.Utilities;

namespace RhetosLanguageServer
{
    public class RhetosLanguageServiceBase : JsonRpcService
    {

        protected LanguageServerSession Session => RequestContext.Features.Get<LanguageServerSession>();

        protected ClientProxy Client => Session.Client;

        protected TextDocument GetDocument(Uri uri)
        {
            if (Session.Documents.TryGetValue(uri, out var sd))
                return sd.Document;
            return null;
        }

        protected TextDocument GetDocument(TextDocumentIdentifier id) => GetDocument(id.Uri);

    }
}
