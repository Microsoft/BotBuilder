using System;
using System.Collections.Generic;
using System.Text;
using Antlr4.Runtime.Tree;

namespace Microsoft.Botframework.LUParser.parser
{
    public static class Visitor
    {
        class AuxEntity
        {
            public string EntityName { get; set; }
            public string Role { get; set; }

            public List<object> EntityValue = null;
            public AuxEntity Parent = null;

        }
        public static UtteranceAndEntitiesMap VisitNormalIntentStringContext(LUFileParser.NormalIntentStringContext context)
        {
            var utterance = String.Empty;
            var entities = new List<EntityElement>();
            var errorMessages = new List<string>();

            // TODO: Check that this interface is actually implemented in the iterable
            foreach (ITerminalNode innerNode in context.children)
            {
                switch (innerNode.Symbol.Type)
                {
                    case LUFileParser.DASH:
                        break;
                    case LUFileParser.EXPRESSION:
                        var tokenizedUterance = TokenizeUtterance(innerNode.GetText().Trim());
                        utterance = RecursivelyResolveTokenizedUtterance(tokenizedUterance, entities, errorMessages, utterance.TrimStart());
                        break;
                    default:
                        utterance += innerNode.GetText();
                        break;
                }
            }

            return new UtteranceAndEntitiesMap {
                Utterance = utterance.Trim(), 
                Entities = entities, 
                ErrorMsgs = errorMessages
            };
        }

        static string RecursivelyResolveTokenizedUtterance(List<object> tokenizedUterance, List<EntityElement> entities, List<string> errorMessages, string srcUtterance)
        {
            char[] invalidCharsInIntentOrEntityName = { '<', '>', '*', '%', '&', ':', '\\', '$' };
            foreach (var item in tokenizedUterance)
            {
                if (item is AuxEntity)
                {
                    var auxEntity = (AuxEntity)item;
                    var entityName = auxEntity.EntityName.Trim();
                    if (!String.IsNullOrEmpty(entityName) && entityName.IndexOfAny(invalidCharsInIntentOrEntityName) >= 0)
                    {
                        errorMessages.Add($"Invalid utterance line, entity name {entityName} cannot contain any of the following characters: [<, >, *, %, &, :, \\, $]");
                        continue;
                    }

                    if (auxEntity.EntityValue == null)
                    {
                        // we have a pattern.any entity
                        var patternStr = !String.IsNullOrEmpty(auxEntity.Role) ? $"{{{auxEntity.EntityName}:{auxEntity.Role}}}" : $"{{{auxEntity.EntityName}}}";
                        srcUtterance += patternStr;
                        entities.Add(
                            new EntityElement
                            { 
                                Type = TypeEnum.PatternAnyEntities,
                                Entity = auxEntity.EntityName.Trim(),
                                Role = auxEntity.Role.Trim()
                            }
                        );
                    } else
                    {
                        // we have a new entity
                        var newEntity = new EntityElement
                        {
                            Type = TypeEnum.Entities,
                            Entity = auxEntity.EntityName.Trim(),
                            Role = auxEntity.Role.Trim(),
                            StartPos = srcUtterance.Length,
                            EndPos = null
                        };
                        if (auxEntity.EntityValue == null)
                        {
                            errorMessages.Add($"Composite entity {auxEntity.Parent.EntityName} includes pattern.any entity {auxEntity.EntityName}.\r\n\tComposites cannot include pattern.any entity as a child.");
                        } else
                        {
                            srcUtterance = RecursivelyResolveTokenizedUtterance(auxEntity.EntityValue, entities, errorMessages, srcUtterance).TrimStart();
                            newEntity.EndPos = srcUtterance.Length - 1;
                            entities.Add(newEntity);
                        }
                    }
                } else
                {
                    char charItem = (char)item;
                    srcUtterance += charItem;
                }
            }
            return srcUtterance;
        }

        static List<object> TokenizeUtterance(string expression)
        {
            var splitString = new List<object>();
            var currentList = splitString;
            AuxEntity currentEntity = null;
            var entityNameCapture = false;
            var entityValueCapture = false;
            var entityRoleCapture = false;

            foreach (char character in expression)
            {
                switch (character)
                {
                    case '{':
                        var newEntity = new AuxEntity
                        {
                            EntityName = String.Empty,
                            Role = String.Empty,
                            EntityValue = null,
                            Parent = currentEntity
                        };
                        currentList.Add(newEntity);
                        currentEntity = newEntity;
                        entityNameCapture = true;
                        entityValueCapture = false;
                        entityRoleCapture = false;
                        break;
                    case '}':
                        currentEntity = currentEntity.Parent;
                        currentList = currentEntity != null ? currentEntity.EntityValue : splitString;
                        entityNameCapture = false;
                        entityValueCapture = false;
                        entityRoleCapture = false;
                        break;
                    case '=':
                        currentEntity.EntityValue = new List<object>();
                        currentList = currentEntity.EntityValue;
                        entityNameCapture = false;
                        entityValueCapture = true;
                        entityRoleCapture = false;
                        break;
                    case ':':
                        if (currentEntity != null && currentEntity.EntityName != String.Empty && entityNameCapture)
                        {
                            entityNameCapture = false;
                            entityValueCapture = false;
                            entityRoleCapture = true;
                        }
                        else
                        {
                            currentList.Add(character);
                        }
                        break;
                    default:
                        if (entityNameCapture)
                        {
                            currentEntity.EntityName += character;
                        } else if (entityValueCapture)
                        {
                            if (character == ' ')
                            {
                                // we do not want leading spaces
                                if (currentList.Count != 0)
                                {
                                    currentList.Add(character);
                                }
                            } else
                            {
                                currentList.Add(character);
                            }
                        } else if (entityRoleCapture)
                        {
                            currentEntity.Role += character;
                        } else
                        {
                            currentList.Add(character);
                        }
                        break;
                }
            }

            return splitString;
        }
    }
}
