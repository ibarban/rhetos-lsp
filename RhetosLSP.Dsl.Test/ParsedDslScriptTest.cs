using System;
using Rhetos.Dsl;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using RhetosLSP.Dsl;
using RhetosLSP.Contracts;

namespace RhetosLSP.Dsl.Test
{
    [TestClass]
    public class ParsedDslScriptTest
    {
        [ConceptKeyword("S")]
        public class SimpleConcept : IConceptInfo
        {
            [ConceptKey]
            public string Name { get; set; }
        }

        [ConceptKeyword("SC")]
        public class SimpleChildConcept : IConceptInfo
        {
            [ConceptKey]
            public SimpleConcept Parent { get; set; }

            [ConceptKey]
            public string Name { get; set; }
        }

        [ConceptKeyword("SCC")]
        public class SimpleChildOfChildConcept : IConceptInfo
        {
            [ConceptKey]
            public SimpleChildConcept Parent { get; set; }

            [ConceptKey]
            public string Name { get; set; }
        }

        public static ParsedDslScript GenerateParsedDslScript(string script)
        {
            var textDocumentItem = new TextDocumentItem { Text = script, Uri = new Uri("http://test.com") };
            IConceptInfo[] conceptPrototypes = new IConceptInfo[] { new SimpleConcept(), new SimpleChildConcept(), new SimpleChildOfChildConcept() };
            ConceptsInfoMetadata conceptsInfoMetadata = new ConceptsInfoMetadata(conceptPrototypes, new ConceptDescriptionProvider(null, conceptPrototypes));
            return new ParsedDslScript(textDocumentItem, 
                new DslParser(conceptPrototypes),
                conceptsInfoMetadata);
        }

        [TestMethod]
        public void ContextStartsAt0Test()
        {
            var parsedDslScript = GenerateParsedDslScript(
                @"S TestConcept
{
    Sc
}");

            Assert.AreEqual("SimpleConcept TestConcept", parsedDslScript.GetContextAtPositionAsync(2, 6).Result.GetKey());
        }

        [TestMethod]
        public void ContextInsideAConceptTest()
        {
            var parsedDslScript = GenerateParsedDslScript(
                @"S T1 {
    SC T2 { SCC T}
}");

            Assert.AreEqual("SimpleChildConcept T1.T2", parsedDslScript.GetContextAtPositionAsync(1, 17).Result.GetKey());
        }

        [TestMethod]
        public void ConceptAfterEmptyContextTest()
        {
            var parsedDslScript = GenerateParsedDslScript(
                @"S P1
{
    SC C1{ }
    SC C2{
        S
    }
}
");

            Assert.AreEqual("SimpleChildConcept P1.C2", parsedDslScript.GetContextAtPositionAsync(4, 11).Result.GetKey());
        }

        [TestMethod]
        public void TwoConceptsInsideSameContextTest()
        {
            var parsedDslScript = GenerateParsedDslScript(
                @"S T1 {
    SC T2;
    SC T3
}");

            Assert.AreEqual("SimpleConcept T1", parsedDslScript.GetContextAtPositionAsync(2, 8).Result.GetKey());
        }

        [TestMethod]
        public void DetectKeywordAtBeginningWhenCursorIsAtEndTest()
        {
            var parsedDslScript = GenerateParsedDslScript(
                @"TestKey");

            Assert.AreEqual(true, parsedDslScript.IsKeywordAtPositionAsync(0, 6).Result);
        }

        [TestMethod]
        public void DetectKeywordInsideContextTest()
        {
            var parsedDslScript = GenerateParsedDslScript(
                @"S T1 {
    SC
}");

            Assert.AreEqual(true, parsedDslScript.IsKeywordAtPositionAsync(1, 6).Result);
        }

        [TestMethod]
        public void FindKeywordAfterIt()
        {
            var parsedDslScript = GenerateParsedDslScript(
                @"S T1 {
    SC
}");
            var actual = parsedDslScript.GetWordSignatureHelpOnPositionAsync(0, 1).Result;
            Assert.AreEqual("S", actual.Word);
        }

        [TestMethod]
        public void FindKeywordAfterItsParameter()
        {
            var parsedDslScript = GenerateParsedDslScript(
                @"S T1 {
    SC
}");
            var actual = parsedDslScript.GetWordSignatureHelpOnPositionAsync(0, 4).Result;
            Assert.AreEqual("S", actual.Word);
        }

        [TestMethod]
        public void FindKeywordInsideBracket()
        {
            var parsedDslScript = GenerateParsedDslScript(
                @"S T1 {

}");
            var actual = parsedDslScript.GetWordSignatureHelpOnPositionAsync(1, 1).Result;
            Assert.AreEqual(null, actual);
        }
    }
}
