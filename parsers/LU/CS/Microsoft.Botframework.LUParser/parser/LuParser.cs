using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Antlr4.Runtime;
using Newtonsoft.Json;

namespace Microsoft.Botframework.LUParser.parser
{
    public class LuParser
    {
        private static Object ParseWithRef(string text, LuResource luResource)
        {
            if (String.IsNullOrEmpty(text))
            {
                return new LuResource(new List<Section>(), String.Empty, new List<Error>());
            }

            // TODO: bool? sectionEnabled = luResource != null ? IsSectionEnabled(luResource.Sections) : null;

            return null;
        }

        public static LuResource parse(string text)
        {
            if (String.IsNullOrEmpty(text))
            {
                // return new LuResource(new Section[] { }, String.Empty, new Error[] { });
            }

            var fileContent = GetFileContent(text);

            return ExtractFileContent((LUFileParser.FileContext)fileContent, text, new List<Error>());
        }

        private static LuResource ExtractFileContent(LUFileParser.FileContext fileContent, string content, List<Error> errors)
        {
            var sections = new List<Section>();

            try
            {
                var modelInfoSections = ExtractModelInfoSections(fileContent);
                foreach (var section in modelInfoSections)
                {
                    errors.AddRange(section.Errors);
                }
                sections.AddRange(modelInfoSections);
            }
            catch (Exception err)
            {
                errors.Add(
                    Diagnostic.BuildDiagnostic(
                        message: $"Error happened when parsing model info section: {err.Message}"
                    )
                );
            }

            try
            {
                var isSectionEnabled = IsSectionEnabled(sections);

                var nestedIntentSections = ExtractNestedIntentSections(fileContent, content);
                foreach (var section in nestedIntentSections)
                {
                    errors.AddRange(section.Errors);
                }
                if (isSectionEnabled)
                {
                    sections.AddRange(nestedIntentSections);
                }
                else
                {
                    foreach (var section in nestedIntentSections)
                    {
                        var emptyIntentSection = new SimpleIntentSection();
                        emptyIntentSection.Name = section.Name;
                        emptyIntentSection.Id = $"{emptyIntentSection.SectionType}_{emptyIntentSection.Name}";

                        // get the end character index
                        // this is default value
                        // it will be reset in function extractSectionBody()
                        var endCharacter = section.Name.Length + 2;

                        var range = new Range { Start = section.Range.Start, End = new Position { Line = section.Range.Start.Line, Character = endCharacter } };
                        emptyIntentSection.Range = range;
                        var errorMsg = $"no utterances found for intent definition: \"# {emptyIntentSection.Name}\"";
                        var error = Diagnostic.BuildDiagnostic(
                            message: errorMsg,
                            range: emptyIntentSection.Range,
                            severity: DiagnosticSeverity.Warn
                        );

                        errors.Add(error);
                        sections.Add(emptyIntentSection);

                        foreach (var subSection in section.SimpleIntentSections)
                        {
                            sections.Add(subSection);
                            errors.AddRange(subSection.Errors);
                        }
                    }
                }
            }
            catch (Exception err)
            {
                errors.Add(
                    Diagnostic.BuildDiagnostic(
                        message: $"Error happened when parsing nested intent section: {err.Message}"
                    )
                );
            }

            try
            {
                var simpleIntentSections = ExtractSimpleIntentSections(fileContent, content);
                foreach (var section in simpleIntentSections)
                {
                    errors.AddRange(section.Errors);
                }
                sections.AddRange(simpleIntentSections);
            }
            catch (Exception err)
            {
                errors.Add(
                    Diagnostic.BuildDiagnostic(
                        message: $"Error happened when parsing simple intent section: {err.Message}"
                    )
                );
            }

            try
            {
                var entitySections = ExtractEntitiesSections(fileContent);
                foreach (var section in entitySections)
                {
                    errors.AddRange(section.Errors);
                }
                sections.AddRange(entitySections);
            }
            catch (Exception err)
            {
                errors.Add(
                    Diagnostic.BuildDiagnostic(
                        message: $"Error happened when parsing entities: {err.Message}"
                    )
                );
            }

            try
            {
                var newEntitySections = ExtractNewEntitiesSections(fileContent);
                foreach (var section in newEntitySections)
                {
                    errors.AddRange(section.Errors);
                }
                sections.AddRange(newEntitySections);
            }
            catch (Exception err)
            {
                errors.Add(
                    Diagnostic.BuildDiagnostic(
                        message: $"Error happened when parsing new entities: {err.Message}"
                    )
                );
            }

            try
            {
                var importSections = ExtractImportSections(fileContent);
                foreach (var section in importSections)
                {
                    errors.AddRange(section.Errors);
                }
                sections.AddRange(importSections);
            }
            catch (Exception err)
            {
                errors.Add(
                    Diagnostic.BuildDiagnostic(
                        message: $"Error happened when parsing import section: {err.Message}"
                    )
                );
            }

            try
            {
                var qnaSections = ExtractQnaSections(fileContent);
                foreach (var section in qnaSections)
                {
                    errors.AddRange(section.Errors);
                }
                sections.AddRange(qnaSections);
            }
            catch (Exception err)
            {
                errors.Add(
                    Diagnostic.BuildDiagnostic(
                        message: $"Error happened when parsing qna section: {err.Message}"
                    )
                );
            }

            sections = ReconstructIntentSections(sections);

            ExtractSectionBody(sections, content);
            var result = new LuResource(sections, content, errors);
            Console.WriteLine(JsonConvert.SerializeObject(result));

            return result;
        }


        private static List<Section> ReconstructIntentSections(List<Section> sections)
        {
            var newSections = new List<Section>();
            sections.Sort((a, b) => a.Range.Start.Line - b.Range.Start.Line);

            for (int index = 0; index < sections.Count; index++)
            {
                var section = sections[index];
                if (sections.Count == index + 1)
                {
                    newSections.Add(section);
                    break;
                }

                if (section is NestedIntentSection nestedSection)
                {
                    if (sections[index+1].SectionType == SectionType.EntitySection || sections[index + 1].SectionType == SectionType.NewEntitySection)
                    {
                        var simpleIntentSections = nestedSection.SimpleIntentSections;
                        simpleIntentSections[simpleIntentSections.Count - 1].Entities.Add(sections[index + 1] as SectionEntity);
                        simpleIntentSections[simpleIntentSections.Count - 1].Errors.AddRange(sections[index + 1].Errors);
                        index++;

                        while (index + 1 < sections.Count
                            && (sections[index + 1] is SectionEntity
                            || (sections[index + 1].SectionType == SectionType.SimpleIntentSection && sections[index + 1].IntentNameLine.Contains("##"))))
                        {
                            if (sections[index + 1] is SectionEntity entitySection)
                            {
                                simpleIntentSections[simpleIntentSections.Count - 1].Entities.Add(entitySection);
                                simpleIntentSections[simpleIntentSections.Count - 1].Errors.AddRange(entitySection.Errors);
                            }
                            else
                            {
                                simpleIntentSections.Add(sections[index + 1] as SimpleIntentSection);
                            }

                            index++;
                        }

                        simpleIntentSections.ForEach(s => nestedSection.Errors.AddRange(s.Errors));
                        nestedSection.SimpleIntentSection = simpleIntentSections;
                    }
                }
                else if (section is SimpleIntentSection simpleIntentSection)
                {
                    while (index + 1 < sections.Count && (sections[index + 1] is SectionEntity entitySection))
                    {
                        section.Entities.Add(entitySection);
                        section.Errors.AddRange(entitySection.Errors);
                        index++;
                    }
                }
                newSections.Add(section);
            }

            return newSections;
        }

        private static List<ModelInfoSection> ExtractModelInfoSections(LUFileParser.FileContext fileContext)
        {
            if (fileContext == null)
            {
                return new List<ModelInfoSection>();
            }
            var context = fileContext;
            var modelInfoSections = context.paragraph().Select(x => x.modelInfoSection()).Where(x => x != null);

            var modelInfoSectionList = modelInfoSections.Select(x => new ModelInfoSection(x)).ToList();

            return modelInfoSectionList;
        }

        private static List<NestedIntentSection> ExtractNestedIntentSections(LUFileParser.FileContext fileContext, string content)
        {
            if (fileContext == null)
            {
                return new List<NestedIntentSection>();
            }

            var nestedIntentSections = fileContext.paragraph().Select(x => x.nestedIntentSection()).Where(x => x != null);
            var nestedIntentSectionsList = nestedIntentSections.Select(x => new NestedIntentSection(x, content)).ToList();

            return nestedIntentSectionsList;
        }

        private static List<SimpleIntentSection> ExtractSimpleIntentSections(LUFileParser.FileContext fileContext, string content)
        {
            if (fileContext == null)
            {
                return new List<SimpleIntentSection>();
            }

            var simpleIntentSections = fileContext.paragraph().Select(x => x.simpleIntentSection()).Where(x => x != null && x.intentDefinition() != null);
            var simpleIntentSectionsList = simpleIntentSections.Select(x => new SimpleIntentSection(x, content)).ToList();

            return simpleIntentSectionsList;
        }

        private static List<SectionEntity> ExtractEntitiesSections(LUFileParser.FileContext fileContext)
        {
            if (fileContext == null)
            {
                return new List<SectionEntity>();
            }

            var aux = fileContext.paragraph();
            var entitySections = fileContext.paragraph().Select(x => x.entitySection()).Where(x => x != null && x.entityDefinition() != null);
            var entitySectionsList = entitySections.Select(x => new SectionEntity(x)).ToList();

            return entitySectionsList;
        }

        private static List<NewEntitySection> ExtractNewEntitiesSections(LUFileParser.FileContext fileContext)
        {
            if (fileContext == null)
            {
                return new List<NewEntitySection>();
            }

            var newEntitySections = fileContext.paragraph().Select(x => x.newEntitySection()).Where(x => x != null && x.newEntityDefinition() != null);
            var newEntitySectionsList = newEntitySections.Select(x => new NewEntitySection(x)).ToList();

            return newEntitySectionsList;
        }

        private static List<ImportSection> ExtractImportSections(LUFileParser.FileContext fileContext)
        {
            if (fileContext == null)
            {
                return new List<ImportSection>();
            }

            var importSections = fileContext.paragraph().Select(x => x.importSection()).Where(x => x != null);
            var importSectionsList = importSections.Select(x => new ImportSection(x)).ToList();

            return importSectionsList;
        }

        private static List<QnaSection> ExtractQnaSections(LUFileParser.FileContext fileContext)
        {
            if (fileContext == null)
            {
                return new List<QnaSection>();
            }

            var qnaSections = fileContext.paragraph().Select(x => x.qnaSection()).Where(x => x != null);
            var qnaSectionsList = qnaSections.Select(x => new QnaSection(x)).ToList();

            return qnaSectionsList;
        }

        private static LUFileParser.FileContext GetFileContent(string text)
        {
            var chars = new AntlrInputStream(text);
            var lexer = new LUFileLexer(chars);
            var tokens = new CommonTokenStream(lexer);
            var parser = new LUFileParser(tokens);
            parser.BuildParseTree = true;
            return parser.file();

        }

        private static void ExtractSectionBody(List<Section> sections, string content)
        {
            var originList = Regex.Split(content, @"\r?\n");
            var qnaSectionIndex = 0;
            foreach (var sectionIter in sections.Select((x, i) => new { Value = x, Index = i }))
            {
                var section = sectionIter.Value;
                if (section is SimpleIntentSection || section is NestedIntentSection || section is QnaSection)
                {
                    var startLine = section.Range.Start.Line - 1;
                    int stopLine = -1;
                    if (sectionIter.Index + 1 < sections.Count)
                    {
                        stopLine = sections[sectionIter.Index + 1].Range.Start.Line - 1;
                        if (startLine < 0 || startLine > stopLine)
                        {
                            throw new IndexOutOfRangeException();
                        }
                    }
                    else
                    {
                        stopLine = originList.Length;
                    }
                    section.Range.End.Line = stopLine;
                    section.Range.End.Character = originList[stopLine - 1].Length;

                    var destList = new List<string>();
                    if (section is QnaSection)
                    {
                        destList = originList.Skip(startLine).Take(stopLine - startLine).ToList();
                        // TODO: maybe change the model so Id is int
                        section.Id = qnaSectionIndex.ToString();
                        qnaSectionIndex++;
                    }
                    else
                    {
                        destList = originList.Skip(startLine + 1).Take(stopLine - (startLine + 1)).ToList();
                    }

                    section.Body = String.Join(Environment.NewLine, destList);

                    if (section is NestedIntentSection nestedIntentSection)
                    {
                        // TODO: check if this downcast can re-cast
                        var sectionList = nestedIntentSection.SimpleIntentSections.ToList<Section>();
                        ExtractSectionBody(sectionList, String.Join(Environment.NewLine, originList.Skip(0).Take(stopLine)));
                    }
                }
            }
        }

        private static bool IsSectionEnabled(List<Section> sections)
        {
            var modelInfoSections = sections.Where(s => s.SectionType == SectionType.ModelInfoSection);
            bool enableSections = false;

            if (modelInfoSections.Any())
            {
                foreach (ModelInfoSection modelInfo in modelInfoSections)
                {
                    var line = modelInfo.ModelInfo;
                    var kvPair = Regex.Split(line, @"@\b(enableSections).(.*)=").Select(item => item.Trim()).ToArray();
                    if (kvPair.Length == 4)
                    {
                        if (String.Equals(kvPair[1], "enableSections", StringComparison.InvariantCultureIgnoreCase) && String.Equals(kvPair[3], "true", StringComparison.InvariantCultureIgnoreCase))
                        {
                            enableSections = true;
                            break;
                        }
                    }
                }
            }

            // TODO: this is a mock behavior
            return enableSections;
        }
    }
}
