using Rhetos.Dsl;
using System.Collections.Generic;

namespace RhetosLSP.Utilities
{
    public static class TokenHelper
    {
        public static int FindFirstStartContextBefore(List<Token> tokens, int tokenIndex)
        {
            for (var i = tokenIndex - 1; i >= 0; tokenIndex--)
            {
                if (tokens[tokenIndex].Type == TokenType.Special && tokens[tokenIndex].Value == "{")
                    return i;
            }
            return -1;
        }

        public static int GetFirstTokenAfter(List<Token> tokens, int positionInScript)
        {
            var tokenIndexStart = GetTokenIndexAfterPosition(tokens, positionInScript);
            while (tokenIndexStart > 0)
            {
                if (tokens[tokenIndexStart].Type == TokenType.Special && tokens[tokenIndexStart].Value == "{")
                    return tokenIndexStart;

                GetContextEndForTokenIndex(tokens, ref tokenIndexStart);
            }

            return -1;
        }

        public static int GetTokenIndexOfStartContext(List<Token> tokens, int positionInScript)
        {
            var tokenIndexStart = GetTokenIndexAfterPosition(tokens, positionInScript);
            while (tokenIndexStart > 0)
            {
                if (tokens[tokenIndexStart].Type == TokenType.Special && tokens[tokenIndexStart].Value == "{")
                    return tokenIndexStart;

                GetContextEndForTokenIndex(tokens, ref tokenIndexStart);
            }

            return -1;
        }

        private static void GetContextEndForTokenIndex(List<Token> tokens, ref int tokenIndexStart)
        {
            while (tokenIndexStart >= 0)
            {
                tokenIndexStart = tokenIndexStart - 1;
                if (tokens[tokenIndexStart].Type == TokenType.Special && tokens[tokenIndexStart].Value == "{")
                    return;

                if (tokens[tokenIndexStart].Type == TokenType.Special && tokens[tokenIndexStart].Value == "}")
                    GetContextEndForTokenIndex(tokens, ref tokenIndexStart);
            }
        }

        private static int GetTokenIndexAfterPosition(List<Token> tokens, int positionInScript)
        {
            for (var i = 0; i < tokens.Count -1; i++)
            {
                if (tokens[i + 1].PositionInDslScript > positionInScript)
                    return i;
            }
            return -1;
        }
    }
}
