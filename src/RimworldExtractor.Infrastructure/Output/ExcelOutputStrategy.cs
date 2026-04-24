using RimworldExtractor.Domain.Entities;
using RimworldExtractor.Domain.Enums;
using RimworldExtractor.Domain.ValueObjects;
using RimworldExtractor.Infrastructure.Excel;

namespace RimworldExtractor.Infrastructure.Output;

public sealed class ExcelOutputStrategy : IOutputStrategy
{
    private readonly IExcelWriter _writer;

    public ExcelOutputStrategy(IExcelWriter writer) => _writer = writer;

    public ExtractionFormat Format => ExtractionFormat.Excel;

    public Task WriteAsync(
        IReadOnlyList<TranslationEntry> translations,
        string outputRoot,
        string baseFileName,
        LanguageCode originalLanguage,
        LanguageCode translationLanguage,
        CancellationToken cancellationToken = default)
    {
        var path = Path.Combine(outputRoot, baseFileName + ".xlsx");
        return _writer.WriteAsync(translations, path, originalLanguage, translationLanguage,
            markNoTranslation: false, cancellationToken);
    }
}
