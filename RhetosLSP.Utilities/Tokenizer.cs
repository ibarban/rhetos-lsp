﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Rhetos.Utilities;
using Rhetos.Dsl;
using Rhetos;

namespace RhetosLSPUtilities
{
    public class Tokenizer
    {
        IDslScriptsProvider _dslScriptsProvider;
        List<Token> _tokens = null;
        object _tokensLock = new object();

        public Tokenizer(IDslScriptsProvider dslScriptsProvider)
        {
            _dslScriptsProvider = dslScriptsProvider;
        }
        public List<Token> GetTokens()
        {
            if (_tokens == null)
                lock (_tokensLock)
                    if (_tokens == null)
                        ParseTokens();
            return _tokens;
        }


        private void ParseTokens()
        {
            _tokens = new List<Token>();

            foreach (var dslScript in _dslScriptsProvider.DslScripts)
            {
                int scriptPosition = 0;

                while (true)
                {
                    TokenizerInternals.SkipWhitespaces(dslScript.Script, ref scriptPosition);
                    if (scriptPosition >= dslScript.Script.Length)
                        break;
                    int startPosition = scriptPosition;

                    try
                    {
                        Token token = new Token();
                        token = TokenizerInternals.GetNextToken_ValueType(dslScript, ref scriptPosition);
                        if (scriptPosition < 0)
                        {
                            // Current token is invalid
                            break;
                        }
                        token.DslScript = dslScript;
                        token.PositionInDslScript = startPosition;

                        if (token.Type != TokenType.Comment)
                            _tokens.Add(token);
                    }
                    catch (DslSyntaxException ex)
                    {
                        //TODO
                    }
                    
                }

                _tokens.Add(new Token { DslScript = dslScript, PositionInDslScript = dslScript.Script.Length, Type = TokenType.EndOfFile, Value = "" });

                if (scriptPosition < 0)
                {
                    // The rest of script is invalid
                    break;
                }
            }
        }
    }

    public static class TokenizerInternals
    {
        readonly static char[] Whitespaces = { ' ', '\t', '\n', '\r' };

        public static void SkipWhitespaces(string script, ref int position)
        {
            while (position < script.Length && Whitespaces.Contains(script[position]))
                position++;
        }

        public static Token GetNextToken_ValueType(DslScript dslScript, ref int position)
        {
            string script = dslScript.Script;
            if (position < script.Length && Whitespaces.Contains(script[position]))
                throw new FrameworkException("Unexpected call of GetNextToken_ValueType without skipping whitespaces.");

            if (IsSimpleStringElement(script[position]))
                return new Token
                {
                    Value = ReadSimpleStringToken(script, ref position),
                    Type = TokenType.Text
                };
            else if (IsQuotedStringStart(script[position]))
                return new Token
                {
                    Value = ReadQuotedString(dslScript, ref position),
                    Type = TokenType.Text
                };
            else if (IsExternalTextStart(script[position]))
                return new Token
                {
                    Value = ReadExternalText(dslScript, ref position),
                    Type = TokenType.Text
                };
            else if (IsSingleLineCommentStart(script, position))
                return new Token
                {
                    Value = ReadSingleLineComment(script, ref position),
                    Type = TokenType.Comment
                };
            else
                return new Token
                {
                    Value = ReadSpecialCharacter(script, ref position),
                    Type = TokenType.Special
                };
        }

        private static bool IsSingleLineCommentStart(string dsl, int position)
        {
            return position < dsl.Length && dsl[position] == '/'
                && position + 1 < dsl.Length && dsl[position + 1] == '/';
        }

        private static string ReadSingleLineComment(string dsl, ref int end)
        {
            end += 2;
            int begin = end;
            while (end < dsl.Length && dsl[end] != '\r' && dsl[end] != '\n')
                end++;
            return dsl.Substring(begin, end - begin);
        }

        private static string ReadSpecialCharacter(string dsl, ref int end)
        {
            end++;
            return dsl.Substring(end - 1, 1);
        }

        private static bool IsSimpleStringElement(char c)
        {
            return Char.IsLetterOrDigit(c) || c == '_';
        }

        private static string ReadSimpleStringToken(string dsl, ref int end)
        {
            int begin = end;
            while (end < dsl.Length && IsSimpleStringElement(dsl[end]))
                end++;
            return dsl.Substring(begin, end - begin);
        }

        private static bool IsQuotedStringStart(char c)
        {
            return c == '"' || c == '\'';
        }

        private static string ReadQuotedString(DslScript dslScript, ref int end)
        {
            string script = dslScript.Script;
            char quote = script[end];
            int begin = end;
            end++;

            while (true)
            {
                while (end < script.Length && script[end] != quote)
                    end++;
                if (end >= script.Length)
                {
                    //throw new DslSyntaxException("Unexpected end of script within quoted string. Missing closing character: " + quote + ". " + dslScript.ReportPosition(begin));
                    // If snppet isn't closed discard this token, and stop further tokenizing
                    end = -1;
                    break;
                }
                if (end + 1 < script.Length && script[end + 1] == quote)
                {
                    // Two quote characters make escape sequence for a quote within the string:
                    end += 2;
                    continue;
                }
                else
                {
                    // Single quote ends string:
                    end++;
                    break;
                }
            }

            return end >= 0 ? script.Substring(begin + 1, end - begin - 2).Replace(new string(quote, 2), new string(quote, 1)) : "INVALID";
        }

        private static bool IsExternalTextStart(char c)
        {
            return c == '<';
        }

        private static HashSet<char> invalidPathChars = new HashSet<char>(Path.GetInvalidPathChars());

        private static string ReadExternalText(DslScript dslScript, ref int end)
        {
            string script = dslScript.Script;
            int begin = end;
            end++;

            while (end < script.Length && script[end] != '>' && !invalidPathChars.Contains(script[end]))
                end++;

            if (end >= script.Length)
                throw new DslSyntaxException("Unexpected end of script within external text reference. Missing closing character: '>'." + dslScript.ReportPosition(end));

            if (script[end] != '>')
                throw new DslSyntaxException("Invalid filename character within external text reference. " + dslScript.ReportPosition(end));

            end++; // Skip closing character.

            string basicFilePath = script.Substring(begin + 1, end - begin - 2);
            string dslScriptFolder = Path.GetDirectoryName(dslScript.Path);
            return LoadFile(Path.Combine(dslScriptFolder, basicFilePath), dslScript, begin);
        }

        private static string LoadFile(string basicFilePath, DslScript dslScript, int begin)
        {
            var filePaths = new List<string> { basicFilePath };

            string basicFileExtension = Path.GetExtension(basicFilePath);
            if (basicFileExtension.Equals(".sql", StringComparison.OrdinalIgnoreCase))
            {
                var directory = Path.GetDirectoryName(basicFilePath);
                var fileName = Path.GetFileNameWithoutExtension(basicFilePath);
                if (string.IsNullOrWhiteSpace(fileName))
                    throw new DslSyntaxException("Referenced empty file name (" + basicFilePath + ") in DSL script. " + dslScript.ReportPosition(begin));

                // Look for SQL dialect-specific files before the generic SQL file:
                filePaths.Insert(0, Path.Combine(directory, fileName + "." + SqlUtility.DatabaseLanguage + basicFileExtension));
                filePaths.Insert(1, Path.Combine(directory, fileName + " (" + SqlUtility.DatabaseLanguage + ")" + basicFileExtension));
            }

            foreach (var filePath in filePaths)
                if (File.Exists(filePath))
                    return File.ReadAllText(filePath, Encoding.Default);

            throw new DslSyntaxException("Cannot find the extension file referenced in DSL script. " + dslScript.ReportPosition(begin) + "\r\nLooking for files:\r\n" + string.Join("\r\n", filePaths));
        }
    }
}
