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
        private IEnumerable<ConceptInfoLSP> ParsedConcepts { get; set; }

        private ParsedResults _parsedResults;

        private readonly string _script;

        private List<Token> _tokens;

        private Task _parsingScriptTask;

        public ParsedDslScript(string script, Uri scriptUri, DslParser dslParser)
        {
            _script = script;

            _parsingScriptTask = Task.Run(() =>
            {
                _tokens = ContentTokenizer.TokenizeContent(script, scriptUri);
                _parsedResults = dslParser.Parse(_tokens);
                ParsedConcepts = _parsedResults.Concepts;
            });
        }

        private readonly char[] specialChars = { ';', '}', '{' };

        private readonly char[] whitespaces = { '\n', ' ', '\r' };

        public Task<bool> IsKeywordAtPositionAsync(int line, int column)
        {
            return _parsingScriptTask.ContinueWith((result) =>
            {
                var position = GetPosition(line, column);
                var tokenIndex = -1;
                for (var i = 0; i < _tokens.Count; i++)
                {
                    if (_tokens[i].PositionInDslScript <= position && position <= (_tokens[i].PositionInDslScript + _tokens[i].Value.Length + 1))
                        tokenIndex = i;
                }

                if (tokenIndex == 0)
                    return true;
                if (tokenIndex > 0 && _tokens[tokenIndex - 1].Type == TokenType.Special)
                    return true;

                return false;
            });
        }

        public Task<string> GetWordOnPositionAsync(int line, int column)
        {
            return _parsingScriptTask.ContinueWith((result) =>
            {
                return ReadWordOverHover(_script, GetPosition(line, column));
            });
        }

        public Task<IConceptInfo> GetContextAtPositionAsync(int line, int column)
        {
            return _parsingScriptTask.ContinueWith((result) =>
            {
                var tokenContextEnd = TokenHelper.GetTokenIndexOfStartContext(_tokens, GetPosition(line, column));
                var tokenContextStart = TokenHelper.FindFirstStartContextBefore(_tokens, tokenContextEnd);
                var tokenContextStartPosition = _tokens[tokenContextStart].PositionInDslScript;
                foreach (var parsedConcept in ParsedConcepts)
                {
                    if (parsedConcept.Location.Position == tokenContextStartPosition)
                        return parsedConcept.Concept;
                }
                return null;
            });
        }

        private int GetPreviousWhitespace(char[] charArray, int position)
        {
            for (int i = position; i > 0; i--)
            {
                if (whitespaces.Contains(charArray[i]))
                    return i;
            }
            return -1;
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
