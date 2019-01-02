/*
    Copyright (C) 2014 Omega software d.o.o.

    This file is part of Rhetos.

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU Affero General Public License as
    published by the Free Software Foundation, either version 3 of the
    License, or (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU Affero General Public License for more details.

    You should have received a copy of the GNU Affero General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using Rhetos.Dsl;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RhetosLSP.Dsl.Test
{
    class TestDslParser : RhetosLSP.Dsl.DslTokenParser
    {
        private readonly Tokenizer _tokenizer;

        public IEnumerable<IConceptInfo> ParsedConcepts {
            get {
                var result =  this.Parse(_tokenizer.GetTokens());
                if (result.Errors.Any())
                    throw new DslSyntaxException(result.Errors.First().Error);
                return result.Concepts.Select(x => x.Concept);
            }
        }

        public TestDslParser(string dsl, IConceptInfo[] conceptInfoPlugins = null) :
            base(conceptInfoPlugins, new ConsoleLogProvider())
        {
            _tokenizer = new Tokenizer(new MockDslScriptsProvider(dsl));
        }

        public IEnumerable<IConceptInfo> ExtractConcepts(IEnumerable<IConceptParser> conceptParsers)
        {
            var result =  base.ExtractConcepts(conceptParsers, _tokenizer.GetTokens());
            if (result.Errors.Any())
                throw new DslSyntaxException(result.Errors.First().Error);
            return result.Concepts.Select(x => x.Concept);
        }

        new public IConceptInfo ParseNextConcept(TokenReader tokenReader, Stack<IConceptInfo> context, IEnumerable<IConceptParser> conceptParsers)
        {
            return base.ParseNextConcept(tokenReader, context, conceptParsers).Concept;
        }
    }
}
