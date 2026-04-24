using RimworldExtractor.Domain.Entities;
using RimworldExtractor.Domain.Enums;
using RimworldExtractor.Domain.ValueObjects;
using RimworldExtractor.Infrastructure.Xml;

namespace RimworldExtractor.Infrastructure.Output;

public class LanguagesOutputStrategy : IOutputStrategy
{
    private readonly IXmlLanguagesWriter _writer;
    protected virtual bool CommentOriginal => false;

    public LanguagesOutputStrategy(IXmlLanguagesWriter writer) => _writer = writer;

    public virtual ExtractionFormat Format => ExtractionFormat.Languages;

    public Task WriteAsync(
        IReadOnlyList<TranslationEntry> translations,
        string outputRoot,
        string baseFileName,
        LanguageCode originalLanguage,
        LanguageCode translationLanguage,
        CancellationToken cancellationToken = default)
    {
        return _writer.WriteAsync(
            translations,
            outputRoot,
            translationLanguage,
            baseFileName,
            skipNoTranslation: false,
            commentOriginal: CommentOriginal,
            fullListTags: Array.Empty<string>(),
            cancellationToken);
    }
}

public sealed class LanguagesWithCommentsOutputStrategy : LanguagesOutputStrategy
{
    public LanguagesWithCommentsOutputStrategy(IXmlLanguagesWriter writer) : base(writer) { }

    protected override bool CommentOriginal => true;
    public override ExtractionFormat Format => ExtractionFormat.LanguagesWithComments;
}
