using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Antlr4.Runtime;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.Botframework.LUParser.parser
{
    public partial class LuResource
    {
        [JsonProperty("sections")]
        public List<Section> Sections { get; set; }
        [JsonProperty("content")]
        public string Content { get; set; }
        [JsonProperty("errors")]
        public List<Error> Errors { get; set; }

        public LuResource(List<Section> sections, string content, List<Error> errors)
        {
            Sections = sections;
            Content = content;
            Errors = errors;
        }
    }
    public partial class Error
    {
        [JsonProperty("Message")]
        public string Message { get; set; }
        [JsonProperty("Range")]
        public Range Range { get; set; }
        [JsonProperty("Severity")]
        public string Severity { get; set; }
    }
    public partial class Range
    {
        [JsonProperty("Start")]
        public Position Start { get; set; }
        [JsonProperty("End")]
        public Position End { get; set; }

        public string StringMessage()
        {
            var result = Start.StringMessage();
            if (Start.Line <= End.Line && Start.Character < End.Character)
            {
                result += " - ";
                result += End.StringMessage();
            }

            return result;
        }
    }
    public partial class Position
    {
        [JsonProperty("Line")]
        public int Line { get; set; }
        [JsonProperty("Character")]
        public int Character { get; set; }

        public string StringMessage()
        {
            return $"line {Line}:{Character}";
        }
    }
    public partial class Section
    {
        [JsonProperty("Errors")]
        public List<Error> Errors { get; set; } = new List<Error>();
        [JsonProperty("SectionType")]
        [JsonConverter(typeof(StringEnumConverter))]
        public SectionType SectionType { get; set; }
        [JsonProperty("Id")]
        public string Id { get; set; } = String.Empty;
        [JsonProperty("Body")]
        public string Body { get; set; } = String.Empty;
        [JsonProperty("UtteranceAndEntitiesMap", NullValueHandling = NullValueHandling.Ignore)]
        public List<UtteranceAndEntitiesMap> UtteranceAndEntitiesMap { get; set; }
        [JsonProperty("Entities", NullValueHandling = NullValueHandling.Ignore)]
        public List<SectionEntity> Entities { get; set; }
        [JsonProperty("Name", NullValueHandling = NullValueHandling.Ignore)]
        public string Name { get; set; }
        [JsonProperty("IntentNameLine", NullValueHandling = NullValueHandling.Ignore)]
        public string IntentNameLine { get; set; }
        [JsonProperty("Range")]
        public Range Range { get; set; }
        [JsonProperty("ModelInfo", NullValueHandling = NullValueHandling.Ignore)]
        public string ModelInfo { get; set; }
        [JsonProperty("SimpleIntentSections", NullValueHandling = NullValueHandling.Ignore)]
        public List<SimpleIntentSection> SimpleIntentSections { get; set; }
        [JsonProperty("SimpleIntentSection", NullValueHandling = NullValueHandling.Ignore)]
        public List<SimpleIntentSection> SimpleIntentSection { get; set; }
        [JsonProperty("Description", NullValueHandling = NullValueHandling.Ignore)]
        public string Description { get; set; }
        [JsonProperty("Path", NullValueHandling = NullValueHandling.Ignore)]
        public string Path { get; set; }
    }
    public partial class SectionEntity : Section
    {
        [JsonProperty("Type", NullValueHandling = NullValueHandling.Ignore)]
        public string Type { get; set; }
        [JsonProperty("Roles", NullValueHandling = NullValueHandling.Ignore)]
        public string Roles { get; set; }
        [JsonProperty("ListBody", NullValueHandling = NullValueHandling.Ignore)]
        public List<string> ListBody { get; set; }
        [JsonProperty("CompositeDefinition", NullValueHandling = NullValueHandling.Ignore)]
        public string CompositeDefinition { get; set; }
        [JsonProperty("RegexDefinition", NullValueHandling = NullValueHandling.Ignore)]
        public string RegexDefinition { get; set; }
        [JsonProperty("SynonymsOrPhraseList", NullValueHandling = NullValueHandling.Ignore)]
        public List<string> SynonymsOrPhraseList { get; set; }
        [JsonProperty("Features", NullValueHandling = NullValueHandling.Ignore)]
        public string Features { get; set; }
        [JsonProperty("SynonymsList", NullValueHandling = NullValueHandling.Ignore)]
        public List<NewEntitySection.SynonymElement> SynonymsList { get; set; } = null;

        public SectionEntity() {}
    }
    public partial class UtteranceAndEntitiesMap
    {
        [JsonProperty("utterance")]
        public string Utterance { get; set; }
        [JsonProperty("entities")]
        public List<EntityElement> Entities { get; set; }
        [JsonProperty("errorMsgs")]
        public List<string> ErrorMsgs { get; set; }
        [JsonProperty("contextText")]
        public string ContextText { get; set; }
        [JsonProperty("range")]
        public Range Range { get; set; }
        [JsonProperty("references", NullValueHandling = NullValueHandling.Ignore)]
        public Reference References { get; set; }
    }
    public partial class Reference
    {
        [JsonProperty("source")]
        public string Source { get; set; }
    }
    public partial class EntityElement
    {
        [JsonProperty("type")]
        [JsonConverter(typeof(StringEnumConverter))]
        public TypeEnum Type { get; set; }
        [JsonProperty("entity")]
        public string Entity { get; set; }
        [JsonProperty("role")]
        public string Role { get; set; }
        [JsonProperty("startPos", NullValueHandling = NullValueHandling.Ignore)]
        public int? StartPos { get; set; }
        [JsonProperty("endPos", NullValueHandling = NullValueHandling.Ignore)]
        public int? EndPos { get; set; }
    }
    public enum SectionType { 
        SimpleIntentSection,
        NestedIntentSection,
        EntitySection,
        NewEntitySection,
        ImportSection,
        ModelInfoSection,
        QnaSection
    };
    public enum TypeEnum {
        Intent,
        Entities,
        PatternAnyEntities,
        ClosedLists,
        Prebuilt,
        Utterance,
        Patterns,
        Regex,
        Composites,
        MachineLearned
    };
    internal static class Converter
    {
        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
            Converters =
            {
                SectionTypeConverter.Singleton,
                TypeEnumConverter.Singleton,
                new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal }
            },
        };
    }
    internal class SectionTypeConverter : JsonConverter
    {
        public override bool CanConvert(Type t) => t == typeof(SectionType) || t == typeof(SectionType?);
        public override object ReadJson(JsonReader reader, Type t, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return null;
            var value = serializer.Deserialize<string>(reader);
            switch (value)
            {
                case "entitySection":
                    return SectionType.EntitySection;
                case "newEntitySection":
                    return SectionType.NewEntitySection;
                case "simpleIntentSection":
                    return SectionType.SimpleIntentSection;
                case "nestedIntentSection":
                    return SectionType.NestedIntentSection;
                case "importSection":
                    return SectionType.ImportSection;
                case "modelInfoSection":
                    return SectionType.ModelInfoSection;
                case "qnaSection":
                    return SectionType.QnaSection;
            }
            throw new Exception("Cannot unmarshal type SectionType");
        }
        public override void WriteJson(JsonWriter writer, object untypedValue, JsonSerializer serializer)
        {
            if (untypedValue == null)
            {
                serializer.Serialize(writer, null);
                return;
            }
            var value = (SectionType)untypedValue;
            switch (value)
            {
                case SectionType.EntitySection:
                    serializer.Serialize(writer, "entitySection");
                    return;
                case SectionType.NewEntitySection:
                    serializer.Serialize(writer, "newEntitySection");
                    return;
                case SectionType.SimpleIntentSection:
                    serializer.Serialize(writer, "simpleIntentSection");
                    return;
                case SectionType.NestedIntentSection:
                    serializer.Serialize(writer, "nestedIntentSection");
                    return;
                case SectionType.ImportSection:
                    serializer.Serialize(writer, "importSection");
                    return;
                case SectionType.ModelInfoSection:
                    serializer.Serialize(writer, "modelInfoSection");
                    return;
                case SectionType.QnaSection:
                    serializer.Serialize(writer, "qnaSection");
                    return;
            }
            throw new Exception("Cannot marshal type EntitySectionType");
        }
        public static readonly SectionTypeConverter Singleton = new SectionTypeConverter();
    }
    internal class TypeEnumConverter : JsonConverter
    {
        public override bool CanConvert(Type t) => t == typeof(TypeEnum) || t == typeof(TypeEnum?);
        public override object ReadJson(JsonReader reader, Type t, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return null;
            var value = serializer.Deserialize<string>(reader);
            switch (value)
            {
                case "intents":
                    break;
                case "entities":
                    return TypeEnum.Entities;
                case "patternAnyEntities":
                    return TypeEnum.PatternAnyEntities;
                case "closedLists":
                    return TypeEnum.ClosedLists;
                case "prebuiltEntities":
                    return TypeEnum.Prebuilt;
                case "utterances":
                    return TypeEnum.Utterance;
                case "patterns":
                    return TypeEnum.Patterns;
                case "regex_entities":
                    return TypeEnum.Regex;
                case "composites":
                    return TypeEnum.Composites;
                case "ml":
                    return TypeEnum.MachineLearned;
            }
            throw new Exception("Cannot unmarshal type TypeEnum");
        }
        public override void WriteJson(JsonWriter writer, object untypedValue, JsonSerializer serializer)
        {
            if (untypedValue == null)
            {
                serializer.Serialize(writer, null);
                return;
            }
            var value = (TypeEnum)untypedValue;
            switch (value)
            {
                case TypeEnum.Intent:
                    serializer.Serialize(writer, "intents");
                    return;
                case TypeEnum.Entities:
                    serializer.Serialize(writer, "entities");
                    return;
                case TypeEnum.PatternAnyEntities:
                    serializer.Serialize(writer, "patternAnyEntities");
                    return;
                case TypeEnum.ClosedLists:
                    serializer.Serialize(writer, "closedLists");
                    return;
                case TypeEnum.Prebuilt:
                    serializer.Serialize(writer, "prebuiltEntities");
                    return;
                case TypeEnum.Utterance:
                    serializer.Serialize(writer, "utterances");
                    return;
                case TypeEnum.Patterns:
                    serializer.Serialize(writer, "patterns");
                    return;
                case TypeEnum.Regex:
                    serializer.Serialize(writer, "regex_entities");
                    return;
                case TypeEnum.Composites:
                    serializer.Serialize(writer, "composites");
                    return;
                case TypeEnum.MachineLearned:
                    serializer.Serialize(writer, "ml");
                    return;
            }
            throw new Exception("Cannot marshal type TypeEnum");
        }
        public static readonly TypeEnumConverter Singleton = new TypeEnumConverter();
    }
}
