using System.Globalization;
using Rhetos.Utilities;

namespace RhetosLSP.Dsl
{
    public class ConceptInfoLocation
    {
        public string Script { get; set; }
        public string Path { get; set; }
        public int Line { get; set; }
        public int Column { get; set; }
        public int Position { get; set; }

        public ConceptInfoLocation() { }
        public ConceptInfoLocation(string script, int line, int column, int position, string path = null)
        {
            Script = script;
            Path = path;
            Line = line;
            Column = column;
            Position = position;
        }

        public string ReportLocation()
        {
            return string.Format(CultureInfo.InvariantCulture,
                "At line {0}, column {1},{2}.",
                    Line, Column,
                    (Path != null) ? " file '" + Path + "'," : "");
        }

        public string ReportLocationWithSurroundings()
        {
            return string.Format(CultureInfo.InvariantCulture,
                "At line {0}, column {1},{2}\r\nafter: \"{3}\",\r\nbefore: \"{4}\".",
                    Line, Column,
                    (Path != null) ? " file '" + Path + "'," : "",
                    ScriptPositionReporting.PreviousText(Script, Line, Column, 70),
                    ScriptPositionReporting.FollowingText(Script, Line, Column, 70));
        }
    }
}