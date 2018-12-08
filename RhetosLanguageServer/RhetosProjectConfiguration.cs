namespace RhetosLanguageServer
{
    public class RhetosProjectConfiguration
    {
        public string RhetosServerPath { get; private set; }

        public string PluginsFolderPath { get { return RhetosServerPath + "\\bin\\Plugins"; } }

        public RhetosProjectConfiguration(string rhetosServerPath)
        {
            RhetosServerPath = rhetosServerPath;
        }
    }
}
