//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     ANTLR Version: 4.8
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

// Generated from .\LUFileParser.g4 by ANTLR 4.8

// Unreachable code detected
#pragma warning disable 0162
// The variable '...' is assigned but its value is never used
#pragma warning disable 0219
// Missing XML comment for publicly visible type or member '...'
#pragma warning disable 1591
// Ambiguous reference in cref attribute
#pragma warning disable 419

using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using IToken = Antlr4.Runtime.IToken;

/// <summary>
/// This interface defines a complete generic visitor for a parse tree produced
/// by <see cref="LUFileParser"/>.
/// </summary>
/// <typeparam name="Result">The return type of the visit operation.</typeparam>
[System.CodeDom.Compiler.GeneratedCode("ANTLR", "4.8")]
[System.CLSCompliant(false)]
public interface ILUFileParserVisitor<Result> : IParseTreeVisitor<Result> {
	/// <summary>
	/// Visit a parse tree produced by <see cref="LUFileParser.file"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitFile([NotNull] LUFileParser.FileContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="LUFileParser.paragraph"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitParagraph([NotNull] LUFileParser.ParagraphContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="LUFileParser.newline"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitNewline([NotNull] LUFileParser.NewlineContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="LUFileParser.errorString"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitErrorString([NotNull] LUFileParser.ErrorStringContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="LUFileParser.nestedIntentSection"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitNestedIntentSection([NotNull] LUFileParser.NestedIntentSectionContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="LUFileParser.nestedIntentNameLine"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitNestedIntentNameLine([NotNull] LUFileParser.NestedIntentNameLineContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="LUFileParser.nestedIntentName"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitNestedIntentName([NotNull] LUFileParser.NestedIntentNameContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="LUFileParser.nameIdentifier"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitNameIdentifier([NotNull] LUFileParser.NameIdentifierContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="LUFileParser.nestedIntentBodyDefinition"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitNestedIntentBodyDefinition([NotNull] LUFileParser.NestedIntentBodyDefinitionContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="LUFileParser.subIntentDefinition"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitSubIntentDefinition([NotNull] LUFileParser.SubIntentDefinitionContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="LUFileParser.simpleIntentSection"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitSimpleIntentSection([NotNull] LUFileParser.SimpleIntentSectionContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="LUFileParser.intentDefinition"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitIntentDefinition([NotNull] LUFileParser.IntentDefinitionContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="LUFileParser.intentNameLine"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitIntentNameLine([NotNull] LUFileParser.IntentNameLineContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="LUFileParser.intentName"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitIntentName([NotNull] LUFileParser.IntentNameContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="LUFileParser.intentBody"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitIntentBody([NotNull] LUFileParser.IntentBodyContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="LUFileParser.normalIntentBody"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitNormalIntentBody([NotNull] LUFileParser.NormalIntentBodyContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="LUFileParser.normalIntentString"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitNormalIntentString([NotNull] LUFileParser.NormalIntentStringContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="LUFileParser.newEntitySection"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitNewEntitySection([NotNull] LUFileParser.NewEntitySectionContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="LUFileParser.newEntityDefinition"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitNewEntityDefinition([NotNull] LUFileParser.NewEntityDefinitionContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="LUFileParser.newEntityListbody"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitNewEntityListbody([NotNull] LUFileParser.NewEntityListbodyContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="LUFileParser.newEntityLine"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitNewEntityLine([NotNull] LUFileParser.NewEntityLineContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="LUFileParser.newCompositeDefinition"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitNewCompositeDefinition([NotNull] LUFileParser.NewCompositeDefinitionContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="LUFileParser.newRegexDefinition"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitNewRegexDefinition([NotNull] LUFileParser.NewRegexDefinitionContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="LUFileParser.newEntityType"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitNewEntityType([NotNull] LUFileParser.NewEntityTypeContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="LUFileParser.newEntityRoles"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitNewEntityRoles([NotNull] LUFileParser.NewEntityRolesContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="LUFileParser.newEntityUsesFeatures"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitNewEntityUsesFeatures([NotNull] LUFileParser.NewEntityUsesFeaturesContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="LUFileParser.newEntityRoleOrFeatures"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitNewEntityRoleOrFeatures([NotNull] LUFileParser.NewEntityRoleOrFeaturesContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="LUFileParser.newEntityName"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitNewEntityName([NotNull] LUFileParser.NewEntityNameContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="LUFileParser.newEntityNameWithWS"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitNewEntityNameWithWS([NotNull] LUFileParser.NewEntityNameWithWSContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="LUFileParser.entitySection"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitEntitySection([NotNull] LUFileParser.EntitySectionContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="LUFileParser.entityDefinition"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitEntityDefinition([NotNull] LUFileParser.EntityDefinitionContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="LUFileParser.entityLine"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitEntityLine([NotNull] LUFileParser.EntityLineContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="LUFileParser.entityName"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitEntityName([NotNull] LUFileParser.EntityNameContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="LUFileParser.entityType"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitEntityType([NotNull] LUFileParser.EntityTypeContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="LUFileParser.compositeEntityIdentifier"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitCompositeEntityIdentifier([NotNull] LUFileParser.CompositeEntityIdentifierContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="LUFileParser.regexEntityIdentifier"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitRegexEntityIdentifier([NotNull] LUFileParser.RegexEntityIdentifierContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="LUFileParser.entityListBody"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitEntityListBody([NotNull] LUFileParser.EntityListBodyContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="LUFileParser.normalItemString"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitNormalItemString([NotNull] LUFileParser.NormalItemStringContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="LUFileParser.importSection"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitImportSection([NotNull] LUFileParser.ImportSectionContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="LUFileParser.importDefinition"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitImportDefinition([NotNull] LUFileParser.ImportDefinitionContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="LUFileParser.qnaSection"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitQnaSection([NotNull] LUFileParser.QnaSectionContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="LUFileParser.qnaDefinition"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitQnaDefinition([NotNull] LUFileParser.QnaDefinitionContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="LUFileParser.qnaSourceInfo"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitQnaSourceInfo([NotNull] LUFileParser.QnaSourceInfoContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="LUFileParser.qnaIdMark"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitQnaIdMark([NotNull] LUFileParser.QnaIdMarkContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="LUFileParser.qnaQuestion"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitQnaQuestion([NotNull] LUFileParser.QnaQuestionContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="LUFileParser.questionText"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitQuestionText([NotNull] LUFileParser.QuestionTextContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="LUFileParser.moreQuestionsBody"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitMoreQuestionsBody([NotNull] LUFileParser.MoreQuestionsBodyContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="LUFileParser.moreQuestion"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitMoreQuestion([NotNull] LUFileParser.MoreQuestionContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="LUFileParser.errorQuestionString"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitErrorQuestionString([NotNull] LUFileParser.ErrorQuestionStringContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="LUFileParser.qnaAnswerBody"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitQnaAnswerBody([NotNull] LUFileParser.QnaAnswerBodyContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="LUFileParser.filterSection"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitFilterSection([NotNull] LUFileParser.FilterSectionContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="LUFileParser.promptSection"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitPromptSection([NotNull] LUFileParser.PromptSectionContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="LUFileParser.filterLine"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitFilterLine([NotNull] LUFileParser.FilterLineContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="LUFileParser.errorFilterLine"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitErrorFilterLine([NotNull] LUFileParser.ErrorFilterLineContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="LUFileParser.multiLineAnswer"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitMultiLineAnswer([NotNull] LUFileParser.MultiLineAnswerContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="LUFileParser.modelInfoSection"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitModelInfoSection([NotNull] LUFileParser.ModelInfoSectionContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="LUFileParser.modelInfoDefinition"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitModelInfoDefinition([NotNull] LUFileParser.ModelInfoDefinitionContext context);
}
