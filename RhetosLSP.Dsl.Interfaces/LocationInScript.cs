using Rhetos.Dsl;

namespace RhetosLSP.Dsl
{
    public class LocationInScript
    {
        public DslScript DslScript { get; set; }
        public int Position { get; set; }

        public LocationInScript() {
            Position = -1;
        }
        public LocationInScript(DslScript dslScript, int position)
        {
            DslScript = dslScript;
            Position = position;
        }
    }
}