using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Microsoft.Botframework.LUParser.parser
{
    public class NestedIntentSection: Section
    {
        public NestedIntentSection(LUFileParser.NestedIntentSectionContext parseTree, string content)
        {
            this.SectionType = SectionType.NestedIntentSection;
            this.Name = ExtractName(parseTree);
            this.Body = String.Empty;
            SimpleIntentSections = ExtractSimpleIntentSections(parseTree, content);
            Errors = new List<Error>();
            if (SimpleIntentSections != null && SimpleIntentSections.Count > 0)
            {
                SimpleIntentSections.ForEach(section =>
                {
                    Errors.AddRange(section.Errors);
                });
            }
            string secTypeStr = $"{SectionType}";
            Id = $"{char.ToLower(secTypeStr[0]) + secTypeStr.Substring(1)}_{Name}";
            var startPosition = new Position { Line = parseTree.Start.Line, Character = parseTree.Start.Column };
            var stopPosition = new Position { Line = parseTree.Stop.Line, Character = parseTree.Stop.Column + parseTree.Stop.Text.Length };
            Range = new Range { Start = startPosition, End = stopPosition };
        }

        public string ExtractName(LUFileParser.NestedIntentSectionContext parseTree)
        {
            return parseTree.nestedIntentNameLine().nestedIntentName().GetText().Trim();
        }

        public List<SimpleIntentSection> ExtractSimpleIntentSections(LUFileParser.NestedIntentSectionContext parseTree, string content)
        {
            var simpleIntentSections = new List<SimpleIntentSection>();
            foreach (var subIntentDefinition in parseTree.nestedIntentBodyDefinition().subIntentDefinition())
            {
                var simpleIntentSection = new SimpleIntentSection(subIntentDefinition.simpleIntentSection(), content);
                simpleIntentSection.Range.Start.Character = 0;
                simpleIntentSections.Add(simpleIntentSection);
            }

            return simpleIntentSections;
        }
    }
}
