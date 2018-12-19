using System;
using Rhetos.Dsl;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using RhetosLSP.Dsl;

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
            return new ParsedDslScript(script, new Uri("http://test.com"), new DslParser(new IConceptInfo[] { new SimpleConcept(), new SimpleChildConcept(), new SimpleChildOfChildConcept() }));
        }

        [TestMethod]
        public void ContextStartsAt0Test()
        {
            var parsedDslScript = GenerateParsedDslScript(
                @"S TestConcept
{
    Sc
}");

            Assert.AreEqual("SimpleConcept TestConcept", parsedDslScript.GetConceptAtPosition(2, 6).GetKey());
        }

        [TestMethod]
        public void ContextInsideAConceptTest()
        {
            var parsedDslScript = GenerateParsedDslScript(
                @"S T1 {
    SC T2 { SCC T}
}");

            Assert.AreEqual("SimpleChildConcept T1.T2", parsedDslScript.GetConceptAtPosition(1, 17).GetKey());
        }

        [TestMethod]
        public void TwoConceptsInsideSameContextTest()
        {
            var parsedDslScript = GenerateParsedDslScript(
                @"S T1 {
    SC T2;
    SC T3
}");

            Assert.AreEqual("SimpleConcept T1", parsedDslScript.GetConceptAtPosition(2, 8).GetKey());
        }
    }
}
