using System;
using RhetosLSP.Contracts;
using Rhetos.Logging;

namespace RhetosLanguageServer
{
    public class VSCodeClientLogger : ILogger
    {
        string _eventName;

        LanguageServerSession _session;

        public VSCodeClientLogger(LanguageServerSession session, string eventName)
        {
            _eventName = eventName;

            _session = session;
        }

        public void Write(EventType eventType, Func<string> logMessage)
        {
            _session.Client.Window.ShowMessage(TranslateEventType(eventType), _eventName + ": " + logMessage());
        }

        private static MessageType TranslateEventType(EventType eventType)
        {
            switch (eventType)
            {
                case EventType.Error:
                    return MessageType.Error;
                case EventType.Info:
                    return MessageType.Info;
                case EventType.Trace:
                    return MessageType.Log;
                default:
                    return MessageType.Log;
            }
        }
    }
}
