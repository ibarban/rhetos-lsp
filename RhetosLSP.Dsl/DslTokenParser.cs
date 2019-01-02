using System;
using Rhetos.Dsl;
using Rhetos.Logging;
using Rhetos.Utilities;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RhetosLSP.Dsl
{
    public class DslTokenParser
    {
        protected readonly IConceptInfo[] _conceptInfoPlugins;
        protected readonly ILogger _logger;

        public DslTokenParser(IConceptInfo[] conceptInfoPlugins, ILogProvider logProvider)
        {
            _conceptInfoPlugins = conceptInfoPlugins;
            _logger = logProvider.GetLogger("DslParser");
        }

        public ParsedResults Parse(List<Token> tokens)
        {
            IEnumerable<IConceptParser> parsers = CreateGenericParsers();
            var parsedConcepts = ExtractConcepts(parsers, tokens);
            var alternativeInitializationGeneratedReferences = InitializeAlternativeInitializationConcepts(parsedConcepts.Concepts);
            parsedConcepts.Concepts.Add(CreateInitializationConcept());
            parsedConcepts.Concepts.AddRange(alternativeInitializationGeneratedReferences);
            return parsedConcepts;
        }

        //=================================================================

        private ConceptInfoWithMetadata CreateInitializationConcept()
        {
            return new ConceptInfoWithMetadata
            {
                Concept = new InitializationConcept
                {
                    RhetosVersion = SystemUtility.GetRhetosVersion()
                },
                Location = new LocationInScript()
            };
        }

        protected IEnumerable<IConceptParser> CreateGenericParsers()
        {
            var conceptMetadata = _conceptInfoPlugins
                .Select(conceptInfo => conceptInfo.GetType())
                .Distinct()
                .Select(conceptInfoType => new
                {
                    conceptType = conceptInfoType,
                    conceptKeyword = ConceptInfoHelper.GetKeyword(conceptInfoType)
                })
                .Where(cm => cm.conceptKeyword != null)
                .ToList();

            var result = conceptMetadata.Select(cm => new GenericParser(cm.conceptType, cm.conceptKeyword)).ToList<IConceptParser>();
            return result;
        }

        protected ParsedResults ExtractConcepts(IEnumerable<IConceptParser> conceptParsers, List<Token> tokens)
        {
            var results = new ParsedResults
            {
                Concepts = new List<ConceptInfoWithMetadata>(),
                Errors = new List<ParserError>()
            };
            TokenReader tokenReader = new TokenReader(tokens, 0);

            List<ConceptInfoWithMetadata> newConcepts = new List<ConceptInfoWithMetadata>();
            Stack<IConceptInfo> context = new Stack<IConceptInfo>();

            tokenReader.SkipEndOfFile();
            while (!tokenReader.EndOfInput)
            {
                var conceptLocation = tokenReader.GetLocation();
                try
                {
                    ConceptInfoWithMetadata conceptInfo = ParseNextConcept(tokenReader, context, conceptParsers);
                    newConcepts.Add(conceptInfo);
                    results.Concepts.Add(conceptInfo);
                    UpdateContextForNextConcept(tokenReader, context, conceptInfo.Concept);
                }
                catch (DslSyntaxException e)
                {
                    // When error occurs stop further parsing
                    results.Errors.Add(new ParserError
                    {
                        Location = tokenReader.GetLocation(),
                        Error = e.Message
                    });
                    context.Clear();
                    break;
                }

                if (context.Count == 0)
                    tokenReader.SkipEndOfFile();
            }

            if (context.Count > 0)
                throw new DslSyntaxException(string.Format(
                    ReportErrorContext(context.Peek(), tokenReader)
                    + "Expected \"}\" at the end of the script to close concept \"{0}\".", context.Peek()));

            return results;
        }

        class Interpretation { public IConceptInfo ConceptInfo; public TokenReader NextPosition; }

        protected ConceptInfoWithMetadata ParseNextConcept(TokenReader tokenReader, Stack<IConceptInfo> context, IEnumerable<IConceptParser> conceptParsers)
        {
            var errors = new List<string>();
            List<Interpretation> possibleInterpretations = new List<Interpretation>();
            LocationInScript conceptLocation = null;

            foreach (var conceptParser in conceptParsers)
            {
                TokenReader nextPosition = new TokenReader(tokenReader);
                conceptLocation = tokenReader.GetLocation();
                var conceptInfoOrError = conceptParser.Parse(nextPosition, context);

                if (!conceptInfoOrError.IsError)
                    possibleInterpretations.Add(new Interpretation
                    {
                        ConceptInfo = conceptInfoOrError.Value,
                        NextPosition = nextPosition
                    });
                else if (!string.IsNullOrEmpty(conceptInfoOrError.Error)) // Empty error means that this parser is not for this keyword.
                    errors.Add(string.Format("{0}: {1}\r\n{2}", conceptParser.GetType().Name, conceptInfoOrError.Error, tokenReader.ReportPosition()));
            }

            if (possibleInterpretations.Count == 0)
            {
                var nextToken = new TokenReader(tokenReader).ReadText(); // Peek, without changing the original tokenReader's position.
                string keyword = nextToken.IsError ? null : nextToken.Value;

                if (errors.Count > 0)
                {
                    string errorsReport = string.Join("\r\n", errors).Limit(500, "...");
                    throw new DslSyntaxException($"Invalid parameters after keyword '{keyword}'. {tokenReader.ReportPosition()}\r\n\r\nPossible causes:\r\n{errorsReport}");
                }
                else if (!string.IsNullOrEmpty(keyword))
                    throw new DslSyntaxException($"Unrecognized concept keyword '{keyword}'. {tokenReader.ReportPosition()}");
                else
                    throw new DslSyntaxException($"Invalid DSL script syntax. {tokenReader.ReportPosition()}");
            }

            int largest = possibleInterpretations.Max(i => i.NextPosition.PositionInTokenList);
            possibleInterpretations.RemoveAll(i => i.NextPosition.PositionInTokenList < largest);
            if (possibleInterpretations.Count > 1)
            {
                string msg = "Ambiguous syntax. " + tokenReader.ReportPosition()
                    + "\r\n Possible interpretations: "
                    + string.Join(", ", possibleInterpretations.Select(i => i.ConceptInfo.GetType().Name))
                    + ".";
                throw new DslSyntaxException(msg);
            }

            tokenReader.CopyFrom(possibleInterpretations.Single().NextPosition);
            var concept = new ConceptInfoWithMetadata
            {
                Concept = possibleInterpretations.Single().ConceptInfo,
                Location = conceptLocation
            };
            return concept;
        }

        protected string ReportErrorContext(IConceptInfo conceptInfo, TokenReader tokenReader)
        {
            var sb = new StringBuilder();
            sb.AppendLine(tokenReader.ReportPosition());
            if (conceptInfo != null)
            {
                sb.AppendFormat("Previous concept: {0}", conceptInfo.GetUserDescription()).AppendLine();
                var properties = conceptInfo.GetType().GetProperties().ToList();
                properties.ForEach(it =>
                    sb.AppendFormat("Property {0} ({1}) = {2}",
                        it.Name,
                        it.PropertyType.Name,
                        it.GetValue(conceptInfo, null) ?? "<null>")
                        .AppendLine());
            }
            return sb.ToString();
        }

        protected void UpdateContextForNextConcept(TokenReader tokenReader, Stack<IConceptInfo> context, IConceptInfo conceptInfo)
        {
            if (tokenReader.TryRead("{"))
                context.Push(conceptInfo);
            else if (!tokenReader.TryRead(";"))
            {
                var sb = new StringBuilder();
                sb.Append(ReportErrorContext(conceptInfo, tokenReader));
                sb.AppendFormat("Expected \";\" or \"{{\".");
                throw new DslSyntaxException(sb.ToString());
            }

            while (tokenReader.TryRead("}"))
            {
                if (context.Count == 0)
                    throw new DslSyntaxException(tokenReader.ReportPosition() + "\r\nUnexpected \"}\". ");
                context.Pop();
            }
        }

        protected IEnumerable<ConceptInfoWithMetadata> InitializeAlternativeInitializationConcepts(IEnumerable<ConceptInfoWithMetadata> parsedConcepts)
        {
            var newConcepts = AlternativeInitialization.InitializeNonparsableProperties(parsedConcepts.Select(x => x.Concept), _logger);
            return newConcepts.Select(x => new ConceptInfoWithMetadata { Concept = x, Location = null });
        }
    }
}
