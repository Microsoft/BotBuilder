using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Botframework.LUParser.parser
{
    partial class SectionEntity
    {
        // TODO: pass this constant to a helper class.
        private char[] invalidCharsInIntentOrEntityName = { '<', '>', '*', '%', '&', ':', '\\', '$' };
        public SectionEntity(LUFileParser.EntitySectionContext parseTree)
        {
            SectionType = SectionType.EntitySection;
            Errors = new List<Error>();
            Name = ExtractName(parseTree);
            Type = ExtractType(parseTree);
            SynonymsOrPhraseList = ExtractSynonymsOrPhraseList(parseTree);
            string secTypeStr = $"{SectionType}";
            Id = $"{char.ToLower(secTypeStr[0]) + secTypeStr.Substring(1)}_{Name}";
            var startPosition = new Position { Line = parseTree.Start.Line, Character = parseTree.Start.Column };
            var stopPosition = new Position { Line = parseTree.Stop.Line, Character = parseTree.Stop.Column + parseTree.Stop.Text.Length };
            Range = new Range { Start = startPosition, End = stopPosition };
        }

        public string ExtractName(LUFileParser.EntitySectionContext parseTree)
        {
            var entityName = String.Empty;
            if (parseTree.entityDefinition().entityLine().entityName() != null)
            {
                entityName = parseTree.entityDefinition().entityLine().entityName().GetText().Trim();
            }
            else
            {
                Errors.Add(
                    Diagnostic.BuildDiagnostic(
                        message: "Invalid entity line, did you miss entity name after $?",
                        context: parseTree.entityDefinition().entityLine()
                    )
                );
            }

            if (!String.IsNullOrEmpty(entityName) && entityName.IndexOfAny(invalidCharsInIntentOrEntityName) >= 0)
            {
                Errors.Add(
                    Diagnostic.BuildDiagnostic(
                        message: $"Invalid entity line, entity name {entityName} cannot contain any of the following characters: [<, >, *, %, &, :, \\, $]",
                        context: parseTree.entityDefinition().entityLine() // TODO: In JS this is newEntityDefinition, probably a bug over there.
                    )
                );
                return null;
            } else
            {
                return entityName;
            }
        }

        public string ExtractType(LUFileParser.EntitySectionContext parseTree)
        {
            if (parseTree.entityDefinition().entityLine().entityType() != null)
            {
                return parseTree.entityDefinition().entityLine().entityType().GetText().Trim();
            }
            else
            {
                Errors.Add(
                    Diagnostic.BuildDiagnostic(
                        message: "Invalid entity line, did you miss entity type after $?",
                        context: parseTree.entityDefinition().entityLine()
                    )
                );
            }

            return null;
        }

        public List<string> ExtractSynonymsOrPhraseList(LUFileParser.EntitySectionContext parseTree)
        {
            var synonymsOrPhraseList = new List<string>();
            if (parseTree.entityDefinition().entityListBody() != null)
            {
                foreach (var errorItemStr in parseTree.entityDefinition().entityListBody().errorString())
                {
                    if (!String.IsNullOrEmpty(errorItemStr.GetText().Trim()))
                    {
                        Errors.Add(
                            Diagnostic.BuildDiagnostic(
                                message: "Invalid list entity line, did you miss '-' at line begin",
                                context: errorItemStr
                            )
                        );
                    }
                }
                foreach (var normalItemStr in parseTree.entityDefinition().entityListBody().normalItemString())
                {
                    var itemStr = normalItemStr.GetText().Trim();
                    synonymsOrPhraseList.Add(itemStr.Substring(1).Trim());
                }
            }

            if (!String.IsNullOrEmpty(Type) && Type.IndexOf('=') > -1 && synonymsOrPhraseList.Count == 0)
            {
                var errorMsg = $"no synonyms list found for list entity definition: \"{ parseTree.entityDefinition().entityLine().GetText()}\"";
                var error = Diagnostic.BuildDiagnostic(
                    message: errorMsg,
                    context: parseTree.entityDefinition().entityLine(),
                    severity: DiagnosticSeverity.Warn
                );
                Errors.Add(error);
            }
            return synonymsOrPhraseList;
        }
    }
}
