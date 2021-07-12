using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Microsoft.Botframework.LUParser.parser
{
    public class SimpleIntentSection: Section
    {
        public SimpleIntentSection()
        {
        }

        public SimpleIntentSection(LUFileParser.SimpleIntentSectionContext parseTree, string content)
        {
            SectionType = SectionType.SimpleIntentSection;
            UtteranceAndEntitiesMap = new List<UtteranceAndEntitiesMap>();
            Entities = new List<SectionEntity>();
            Errors = new List<Error>();
            Body = String.Empty;

            if (parseTree != null)
            {
                Name = ExtractName(parseTree);
                IntentNameLine = ExtractIntentNameLine(parseTree);
                var result = ExtractUtterancesAndEntitiesMap(parseTree);
                UtteranceAndEntitiesMap = result.utterances;
                Errors = result.errors;
                string secTypeStr = $"{SectionType}";
                Id = $"{char.ToLower(secTypeStr[0]) + secTypeStr.Substring(1)}_{Name}";
                var startPosition = new Position { Line = parseTree.Start.Line, Character = parseTree.Start.Column };
                var stopPosition = new Position { Line = parseTree.Stop.Line, Character = parseTree.Stop.Column + parseTree.Stop.Text.Length };
                Range = new Range { Start = startPosition, End = stopPosition };
            }
        }

        public string ExtractName(LUFileParser.SimpleIntentSectionContext parseTree)
        {
            return parseTree.intentDefinition().intentNameLine().intentName().GetText().Trim();
        }

        public string ExtractIntentNameLine(LUFileParser.SimpleIntentSectionContext parseTree)
        {
            return parseTree.intentDefinition().intentNameLine().GetText().Trim();
        }

        public (List<UtteranceAndEntitiesMap> utterances, List<Error> errors) ExtractUtterancesAndEntitiesMap(LUFileParser.SimpleIntentSectionContext parseTree)
        {
            var utterancesAndEntitiesMap = new List<UtteranceAndEntitiesMap>();
            var errors = new List<Error>();

            if (parseTree.intentDefinition().intentBody() != null && parseTree.intentDefinition().intentBody().normalIntentBody() != null)
            {
                foreach (var errorIntentStr in parseTree.intentDefinition().intentBody().normalIntentBody().errorString())
                {
                    if (!String.IsNullOrEmpty(errorIntentStr.GetText().Trim()))
                    {
                        errors.Add(
                            Diagnostic.BuildDiagnostic(
                                message: "Invalid intent body line, did you miss '-' at line begin?",
                                context: errorIntentStr)
                        );
                    }
                }

                foreach (var normalIntentStr in parseTree.intentDefinition().intentBody().normalIntentBody().normalIntentString())
                {
                    UtteranceAndEntitiesMap utteranceAndEntities = null;

                    try
                    {
                        utteranceAndEntities = Visitor.VisitNormalIntentStringContext(normalIntentStr);
                    }
                    catch
                    {
                        errors.Add(
                            Diagnostic.BuildDiagnostic(
                                message: "Invalid utterance definition found. Did you miss a '{' or '}'?",
                                context: normalIntentStr
                            )
                        );
                    }
                    if (utteranceAndEntities != null)
                    {
                        utteranceAndEntities.ContextText = normalIntentStr.GetText();
                        var startPos = new Position { Line = normalIntentStr.Start.Line, Character = normalIntentStr.Start.Column };
                        var stopPos = new Position { Line = normalIntentStr.Stop.Line, Character = normalIntentStr.Stop.Column + normalIntentStr.Stop.Text.Length };
                        utteranceAndEntities.Range = new Range { Start = startPos, End = stopPos };

                        var markdownUrlMatch = Regex.Match(utteranceAndEntities.Utterance, @"^\[(?:[^\[]+)\]\((.*)\)$");

                        if (markdownUrlMatch.Success)
                        {
                            utteranceAndEntities.References = new Reference() { Source = markdownUrlMatch.Groups[1].Value };
                        }

                        utterancesAndEntitiesMap.Add(utteranceAndEntities);
                        foreach (var errorMsg in utteranceAndEntities.ErrorMsgs)
                        {
                            errors.Add(
                                Diagnostic.BuildDiagnostic(
                                    message: errorMsg,
                                    context: normalIntentStr
                                )
                            );
                        }
                    }
                }
            }

            if (utterancesAndEntitiesMap.Count == 0)
            {
                var errorMsg = $"no utterances found for intent definition: \"# {this.Name}\"";
                var error = Diagnostic.BuildDiagnostic(
                    message: errorMsg,
                    context: parseTree.intentDefinition().intentNameLine(),
                    severity: DiagnosticSeverity.Warn
                );

                errors.Add(error);
            }

            return (utterances: utterancesAndEntitiesMap, errors);
        }
    }
}
