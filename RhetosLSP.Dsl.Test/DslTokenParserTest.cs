using System;
using Rhetos.Dsl;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhetos.Utilities;

namespace RhetosLSP.Dsl.Test
{
    [TestClass]
    public class DslTokenParserTest
    {
        [ConceptKeyword("S")]
        public class SimpleConcept : IConceptInfo
        {
            [ConceptKey]
            public string ConceptName { get; set; }

            public string CodeSnippet { get; set; }
        }

        [ConceptKeyword("SC")]
        public class SimpleCildConcept : IConceptInfo
        {
            [ConceptKey]
            public string ConceptName { get; set; }
        }

        public static ParsedResults ParseScript(string dslScript)
        {
            var parser = new DslTokenParser(new IConceptInfo[] { new SimpleConcept() }, new ConsoleLogProvider());
            var tokens = ContentTokenizer.TokenizeContent(dslScript, new Uri("//test"));
            return parser.Parse(tokens);
        }

        [TestMethod]
        public void UnclosedSnippetTest()
        {
            var parsedResults = ParseScript(@"S Test 'some code snippet");

            Assert.AreEqual(1, parsedResults.Errors.Count);
            Assert.AreEqual(7, parsedResults.Errors[0].Location.Position);
            Assert.IsTrue(parsedResults.Errors[0].Error.Contains("Unexpected end of script within quoted string"));
        }

        [TestMethod]
        public void TryRecoverFromParsingErrorTest()
        {
            var parsedResults = ParseScript(@"S Test '';
B Test '';
S Test 2 '';");

            Assert.AreEqual(1, parsedResults.Errors.Count);
            Assert.AreEqual(3, parsedResults.Concepts);
            Assert.AreEqual(11, parsedResults.Errors[0].Location.Position);
        }

        [TestMethod]
        public void TryRecoverFromParsingErrorInContextTest()
        {
            var parsedResults = ParseScript(
@"S Test ''{
    SC Test
}
S Test2 '';");

            Assert.AreEqual(1, parsedResults.Errors.Count);
            Assert.AreEqual(3, parsedResults.Concepts);
            Assert.AreEqual(15, parsedResults.Errors[0].Location.Position);
        }

        [TestMethod]
        public void UnfinishedConceptTest()
        {
            var parsedResults = ParseScript(@"S Test '';
S Ta;");

            Assert.AreEqual(1, parsedResults.Errors.Count);
            Assert.IsTrue(parsedResults.Errors[0].Error.Contains("Invalid parameters after keyword"));
        }
    }
}
