using System;
using Rhetos.Dsl;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace RhetosLSP.Utilities.Test
{
    [TestClass]
    public class TokenHelperTest
    {
        public static List<Token> ComplementTokens(List<Token> tokens)
        {
            var complementedTokens = new List<Token>();
            var positionInScript = 0;
            foreach (var token in tokens)
            {
                complementedTokens.Add(new Token
                {
                    Value = token.Value,
                    Type = token.Type,
                    PositionInDslScript = positionInScript,
                    DslScript = new DslScript {
                        Name = "",
                        Script = "",
                        Path = ""
                    }
                });
                positionInScript = positionInScript + token.Value.Length + 1;
            }

            return complementedTokens;
        }

        [TestMethod]
        public void NestedContextTest()
        {
            var tokens = new List<Token>
            {
                new Token{ Value = "M", Type = TokenType.Text, PositionInDslScript = 0},
                new Token{ Value = "{", Type = TokenType.Special, PositionInDslScript = 2},
                new Token{ Value = "E1", Type = TokenType.Text, PositionInDslScript = 4},
                new Token{ Value = "{", Type = TokenType.Special, PositionInDslScript = 6},
                new Token{ Value = "}", Type = TokenType.Special, PositionInDslScript = 8},
                new Token{ Value = "E2", Type = TokenType.Text, PositionInDslScript = 10},
                new Token{ Value = "}", Type = TokenType.Special, PositionInDslScript = 12}
            };

            Assert.AreEqual(1, TokenHelper.GetTokenIndexOfStartContext(ComplementTokens(tokens), 11));
        }

        [TestMethod]
        public void NoContextFoundTest()
        {
            var tokens = new List<Token>
            {
                new Token{ Value = "M1", Type = TokenType.Text, PositionInDslScript = 0},
                new Token{ Value = "{", Type = TokenType.Special, PositionInDslScript = 2},
                new Token{ Value = "}", Type = TokenType.Special, PositionInDslScript = 4},
                new Token{ Value = "M2", Type = TokenType.Text, PositionInDslScript = 6},
            };

            Assert.AreEqual(-1, TokenHelper.GetTokenIndexOfStartContext(ComplementTokens(tokens), 7));
        }
    }
}
