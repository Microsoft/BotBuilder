using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace Microsoft.Botframework.LUParser.parser
{
    public class QnaSection : Section
    {
        const string QNA_GENERIC_SOURCE = "custom editorial";

        // TODO: not sure if serialization is needed for pairs
        public class QnaTuple
        {
            [JsonProperty("key")]
            public string key { get; set; }

            [JsonProperty("value")]
            public string value { get; set; }
        }

        public class PromptDefinition
        {
            [JsonProperty("displayText")]
            public string displayText { get; set; }

            [JsonProperty("linkedQuestion")]
            public string linkedQuestion { get; set; }

            [JsonProperty("contextOnly")]
            public string contextOnly { get; set; }
        }

        [JsonProperty("Questions")]
        public List<string> Questions { get; set; }

        [JsonProperty("FilterPairs")]
        public List<QnaTuple> FilterPairs { get; set; }

        [JsonProperty("Answer")]
        public string Answer { get; set; }

        [JsonProperty("QAPairId")]
        public string QAPairId { get; set; }

        [JsonProperty("prompts")]
        public List<PromptDefinition> prompts { get; set; }

        [JsonProperty("promptsText")]
        public List<string> promptsText { get; set; }

        [JsonProperty("source")]
        public string source { get; set; }

        public QnaSection(LUFileParser.QnaSectionContext parseTree)
        {
            SectionType = SectionType.QnaSection;
            Questions = new List<string>() { ExtractQuestion(parseTree) };
            var result = ExtractMoreQuestions(parseTree);
            Questions.AddRange(result.questions);
            Errors = result.errors;
            var result2 = ExtractFilterPairs(parseTree);
            FilterPairs = result2.filterPairs;
            Errors.AddRange(result2.errors);
            Answer = ExtractAnswer(parseTree);
            var result3 = ExtractPrompts(parseTree);
            prompts = result3.promptDefinitions;
            promptsText = result3.promptTextList;
            Errors.AddRange(result3.errors);
            QAPairId = ExtractAssignedId(parseTree);
            source = ExtractSourceInfo(parseTree);
            var startPosition = new Position { Line = parseTree.Start.Line, Character = parseTree.Start.Column };
            var stopPosition = new Position { Line = parseTree.Stop.Line, Character = parseTree.Stop.Column + parseTree.Stop.Text.Length };
            Range = new Range { Start = startPosition, End = stopPosition };
        }

        public string ExtractQuestion(LUFileParser.QnaSectionContext parseTree)
        {
            return parseTree.qnaDefinition().qnaQuestion().questionText().GetText().Trim();
        }

        public (List<string> questions, List<Error> errors) ExtractMoreQuestions(LUFileParser.QnaSectionContext parseTree)
        {
            var questions = new List<string>();
            var errors = new List<Error>();
            var questionsBody = parseTree.qnaDefinition().moreQuestionsBody();
            foreach (var errorQuestionStr in questionsBody.errorQuestionString())
            {
                if (!String.IsNullOrEmpty(errorQuestionStr.GetText().Trim()))
                {
                    errors.Add(
                        Diagnostic.BuildDiagnostic(
                            message: $"Invalid QnA question line, did you miss '-' at line begin",
                            context: errorQuestionStr
                        )
                    );
                }
            }

            foreach (var question in questionsBody.moreQuestion())
            {
                var questionText = question.GetText().Trim();
                questions.Add(questionText.Substring(1).Trim());
            }

            return (questions, errors);
        }

        public (List<QnaTuple> filterPairs, List<Error> errors) ExtractFilterPairs(LUFileParser.QnaSectionContext parseTree)
        {
            var filterPairs = new List<QnaTuple>();
            var errors = new List<Error>();
            var filterSection = parseTree.qnaDefinition().qnaAnswerBody().filterSection();
            if (filterSection != null)
            {
                if (filterSection.errorFilterLine() != null)
                {
                    foreach (var errorFilterLineStr in filterSection.errorFilterLine())
                    {
                        if (!String.IsNullOrEmpty(errorFilterLineStr.GetText().Trim()))
                        {
                            errors.Add(
                                Diagnostic.BuildDiagnostic(
                                    message: $"Invalid QnA filter line, did you miss '-' at line begin",
                                    context: errorFilterLineStr
                                )
                            );
                        }
                    }
                }

                foreach (var filterLine in filterSection.filterLine())
                {
                    var filterLineText = filterLine.GetText().Trim();
                    filterLineText = filterLineText.Substring(1).Trim();
                    var filterPair = filterLineText.Split('=');
                    var key = filterPair[0].Trim();
                    var value = filterPair[1].Trim();
                    filterPairs.Add(new QnaTuple{ key = key, value = value});
                }
            }

            return (filterPairs, errors);
        }

        public string ExtractAnswer(LUFileParser.QnaSectionContext parseTree)
        {
            var multiLineAnswer = parseTree.qnaDefinition().qnaAnswerBody().multiLineAnswer().GetText().Trim();
            // trim first and last line
            // TODO: validate this regex
            var answerRegexp = new Regex(@"^```(markdown)?\r*\n(?<answer>(.|\n|\r\n|\t| )*)\r?\n.*?```$", RegexOptions.IgnoreCase | RegexOptions.Multiline);
            var answer_match = answerRegexp.Match(multiLineAnswer);
            var answer_group = answer_match.Groups["answer"];
            return answer_group.Success ? answer_group.Value : String.Empty;
        }

        public (List<PromptDefinition> promptDefinitions, List<string> promptTextList, List<Error> errors) ExtractPrompts(LUFileParser.QnaSectionContext parseTree)
        {
            var promptDefinitions = new List<PromptDefinition>();
            var promptTextList = new List<string>();
            var errors = new List<Error>();
            var promptSection = parseTree.qnaDefinition().promptSection();

            if (promptSection == null)
            {
                return (promptDefinitions, promptTextList: null, errors);
            }
            if (promptSection.errorFilterLine() != null)
            {
                foreach (var errorFilterLineStr in promptSection.errorFilterLine())
                {
                    if (!String.IsNullOrEmpty(errorFilterLineStr.GetText().Trim()))
                    {
                        errors.Add(
                            Diagnostic.BuildDiagnostic(
                                message: $"Invalid QnA prompt line, expecting '-' prefix for each line.",
                                context: errorFilterLineStr
                            )
                        );
                    }
                }
            }

            foreach (var promptLine in promptSection.filterLine())
            {
                var filterLineText = promptLine.GetText().Trim();
                filterLineText = filterLineText.Substring(1).Trim();
                promptTextList.Add(filterLineText);
                var promptConfigurationRegExp = new Regex(@"^\[(?<displayText>.*?)]\([ ]*\#[ ]*[ ?]*(?<linkedQuestion>.*?)\)[ ]*(?<contextOnly>\`context-only\`)?.*?$", RegexOptions.Multiline | RegexOptions.IgnoreCase);
                var splitLineMatch = promptConfigurationRegExp.Match(filterLineText);
                if (!splitLineMatch.Success)
                {
                    errors.Add(
                        Diagnostic.BuildDiagnostic(
                            message: $"Invalid QnA prompt definition. Unable to parse prompt. Please verify syntax as well as question link.",
                            context: promptLine
                        )
                    );
                }
                promptDefinitions.Add(
                    new PromptDefinition()
                    {
                        displayText = splitLineMatch.Groups["displayText"].Value,
                        linkedQuestion = splitLineMatch.Groups["linkedQuestion"].Value,
                        contextOnly = splitLineMatch.Groups["linkedQuestion"].Value
                    }
                );
            }

            return (promptDefinitions, promptTextList, errors);
        }

        public string ExtractAssignedId(LUFileParser.QnaSectionContext parseTree)
        {
            var idAssignment = parseTree.qnaDefinition().qnaIdMark();
            if (idAssignment != null)
            {
                var idTextRegExp = new Regex(@"^\<a[ ]*id[ ]*=[ ]*[""\'](?<idCaptured>.*?)[""\'][ ]*>[ ]*\<\/a\>$", RegexOptions.Multiline | RegexOptions.IgnoreCase);


                var idTextMatch = idTextRegExp.Match(idAssignment.GetText().Trim());
                return idTextMatch.Groups["idCaptured"].Success ? idTextMatch.Groups["idCaptured"].Value : null;
            }
            return null;
        }

        public string ExtractSourceInfo(LUFileParser.QnaSectionContext parseTree)
        {
            var srcAssignment = parseTree.qnaDefinition().qnaSourceInfo();
            if (srcAssignment != null)
            {
                var srcRegExp = new Regex(@"^[ ]*\>[ ]*!#[ ]*@qna.pair.source[ ]*=[ ]*(?<sourceInfo>.*?)$", RegexOptions.Multiline | RegexOptions.IgnoreCase);


                var srcMatch = srcRegExp.Match(srcAssignment.GetText().Trim());
                return srcMatch.Groups["sourceInfo"].Success ? srcMatch.Groups["sourceInfo"].Value : QNA_GENERIC_SOURCE;
            }
            return QNA_GENERIC_SOURCE;
        }
    }
}
