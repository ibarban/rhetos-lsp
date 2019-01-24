﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhetos.Dsl;
using RhetosLSP.Contracts;
using RhetosLSP.Utilities;

namespace RhetosLSP.Dsl
{
    public class ParsedDslScript : IParsedDslScript
    {
        private IEnumerable<ConceptInfoLSP> ParsedConcepts { get; set; }

        private ParsedResults _parsedResults;

        private List<Token> _tokens;

        private TextDocument _document;

        DslParser _dslParser;

        private Task _parsingScriptTask;

        public ParsedDslScript(TextDocumentItem doc, DslParser dslParser)
        {
            _dslParser = dslParser;
            _parsingScriptTask = Task.Run(() =>
            {
                _document = TextDocument.Load<FullTextDocument>(doc);
                _tokens = ContentTokenizer.TokenizeContent(_document.Content, _document.Uri);
                _parsedResults = _dslParser.Parse(_tokens);
                ParsedConcepts = _parsedResults.Concepts;
            });
        }

        public void UpdateDocument(ICollection<TextDocumentContentChangeEvent> contentChanges)
        {
            _parsingScriptTask.ContinueWith((results) => {
                _document = _document.ApplyChanges(new List<TextDocumentContentChangeEvent>(contentChanges));
                _tokens = ContentTokenizer.TokenizeContent(_document.Content, _document.Uri);
                _parsedResults = _dslParser.Parse(_tokens);
                ParsedConcepts = _parsedResults.Concepts;
            });
        }

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

        public Task<WordOnHover> GetWordOnPositionAsync(int line, int column)
        {
            return _parsingScriptTask.ContinueWith((result) =>
            {
                return ReadWordOverHover(_document.Content, line, column);
            });
        }

        public Task<IConceptInfo> GetContextAtPositionAsync(int line, int column)
        {
            return _parsingScriptTask.ContinueWith((result) =>
            {
                var tokenContextEnd = TokenHelper.FindContextStart(_tokens, GetPosition(line, column));
                var tokenContextStart = TokenHelper.FindConceptStart(_tokens, tokenContextEnd);
                var tokenContextStartPosition = _tokens[tokenContextStart].PositionInDslScript;
                foreach (var parsedConcept in ParsedConcepts)
                {
                    if (parsedConcept.Location.Position == tokenContextStartPosition)
                        return parsedConcept.Concept;
                }
                return null;
            });
        }

        private int GetPosition(int line, int column)
        {
            var lineIndex = 0;
            var currentLine = 0;
            while (currentLine < line && lineIndex != -1)
            {
                currentLine = currentLine + 1;
                lineIndex = _document.Content.IndexOf("\n", lineIndex + 1);
            }

            if (currentLine != line)
                return -1;
            else
                return lineIndex + column;
        }

        private WordOnHover ReadWordOverHover(string content, int line, int column)
        {
            int position = GetPosition(line, column);
            int lineIndex = position != -1 ? position - column : 0;

            var contentAsCharArray = content.ToCharArray();
            int startPosition = 0;
            int endPosition = 0;

            for (int i = position; i >= 0; i--)
            {
                var currentChar = contentAsCharArray[i];
                if (Constants.StopCharacters.Contains(currentChar))
                {
                    startPosition = i;
                    break;
                }
            }

            for (int i = startPosition + 1; i < content.Length; i++)
            {
                var currentChar = contentAsCharArray[i];
                if (Constants.StopCharacters.Contains(currentChar))
                {
                    endPosition = i;
                    break;
                }
            }
            startPosition = startPosition == 0 ? 0 : startPosition + 1;

            string foundWord = new string(contentAsCharArray, startPosition, (endPosition - startPosition));
            int startColumn = startPosition - lineIndex <= 0 ? 0 : startPosition - lineIndex - 1;
            int endColumn = endPosition - lineIndex <= 0 ? 0 : endPosition - lineIndex - 1;

            return new WordOnHover
            {
                Word = foundWord,
                Start = new Position(line, startColumn),
                End = new Position(line, endColumn)
            };
        }
    }
}
