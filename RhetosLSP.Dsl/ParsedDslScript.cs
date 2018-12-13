using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhetos.Dsl;

namespace RhetosLSP.Dsl
{
    public class ParsedDslScript : IParsedDslScript
    {
        public IEnumerable<ConceptInfoLSP> ParsedConcepts { get; private set; }

        private readonly string _script;

        private List<Token> _tokens;

        public ParsedDslScript(string script, Uri scriptUri, DslParser dslParser)
        {
            _script = script;
            _tokens = ContentTokenizer.TokenizeContent(script, scriptUri);
            ParsedConcepts = dslParser.Parse(_tokens);
        }

        public bool IsKeywordAtPosition(int line, int column)
        {
            var position = GetPosition(line, column);
            char[] specialChars = { ';', '}', '{' };
            char[] charactersToIgnore = { '\n', ' ', '\r' };
            var contentAsCharArray = _script.ToCharArray();

            for (int i = position; i > 0; i--)
            {
                var currentChar = contentAsCharArray[i];

                if (charactersToIgnore.Contains(currentChar))
                    continue;
                if (!charactersToIgnore.Contains(currentChar) && !specialChars.Contains(currentChar)) //Regular word found
                    return false;
                if (specialChars.Contains(currentChar))
                    return true;
            }

            return false;
        }

        public string GetWordOnPosition(int line, int column)
        {
            return ReadWordOverHover(_script, GetPosition(line, column));
        }

        private int GetPosition(int line, int column)
        {
            var lineIndex = 0;
            var currentLine = 0;
            while (currentLine < line && lineIndex != -1)
            {
                currentLine = currentLine + 1;
                lineIndex = _script.IndexOf("\n", lineIndex + 1);
            }

            if (currentLine != line)
                return -1;
            else
                return lineIndex + column;
        }

        private string ReadWordOverHover(string content, int position)
        {
            char[] stopCharacters = { '\n', ' ', '\r', ';' };
            var contentAsCharArray = content.ToCharArray();
            int startPosition = 0;
            int endPosition = 0;

            for (int i = position; i >= 0; i--)
            {
                var currentChar = contentAsCharArray[i];
                if (stopCharacters.Contains(currentChar))
                {
                    startPosition = i;
                    break;
                }
            }

            for (int i = startPosition + 1; i < content.Length; i++)
            {
                var currentChar = contentAsCharArray[i];
                if (stopCharacters.Contains(currentChar))
                {
                    endPosition = i;
                    break;
                }
            }
            startPosition = startPosition == 0 ? 0 : startPosition + 1;

            string foundWord = new string(contentAsCharArray, startPosition, (endPosition - startPosition));

            return foundWord;
        }
    }
}
