using Rhetos.Logging;

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
