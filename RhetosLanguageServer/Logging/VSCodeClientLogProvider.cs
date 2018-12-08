using Rhetos.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RhetosLanguageServer.Services;

namespace RhetosLanguageServer
{
    public class VSCodeClientLogProvider : ILogProvider
    {
        LanguageServerSession _session;

        public VSCodeClientLogProvider(LanguageServerSession session)
        {
            _session = session;
        }

        public ILogger GetLogger(string eventName)
        {
            return new VSCodeClientLogger(_session, eventName);
        }
    }
}
