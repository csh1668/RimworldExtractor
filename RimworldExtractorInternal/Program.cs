using DocumentFormat.OpenXml.Vml.Office;
using RimworldExtractorInternal.Records;

namespace RimworldExtractorInternal
{
    internal class Program
    {
        static void Main(string[] args)
        {
        //var path = Console.ReadLine()!;
        //// var otherPath = Console.ReadLine()!;
        //var lst = Extractor.ExtractDefs(path, null).ToList();
        //Console.WriteLine(lst.Count);

        //lst.AddRange(Extractor.ExtractPatches(Console.ReadLine()!));
        //Console.WriteLine(lst.Count);
        //IO.ToExcel(lst);

        //Entry:
        //    Console.Clear();
        //    Console.WriteLine("데모 / GUI 미완성");
        //    Console.WriteLine("원하는 작업을 입력하세요");
        //    Console.WriteLine("1. 번역 데이터 추출");
        //    Console.WriteLine("2. xlsx -> xml");

        //    switch (Console.ReadLine())
        //    {
        //        case "1":
        //            goto Work1;
        //            break;
        //        case "2":
        //            goto Work2;
        //            break;

        //    }

        //    return;
        //Work1:
        //    Console.WriteLine("모드의 루트 폴더를 입력하세요.");
        //    var path = Console.ReadLine()!;
        //    var metaData = ModMetaDataReader.GetModIdentifier(path);
        //    Console.WriteLine(metaData);

        //    Console.WriteLine("참조 모드의 'Defs' 폴더를 입력하세요. 다 입력했으면 공백 상태에서 엔터");
        //    List<string> refPaths = new List<string>();
        //    while (true)
        //    {
        //        var refPath = Console.ReadLine()!;
        //        if (refPath == "")
        //        {
        //            break;
        //        }
        //        refPaths.Add(refPath);
        //    }

        //    List<TranslationEntry> extraction = new List<TranslationEntry>();

        //    foreach (var extractableFolder in ModMetaDataReader.GetExtractableFolders(path))
        //    {
        //        Console.WriteLine(extractableFolder);
        //        Console.WriteLine("이 폴더를 추출할까요? y or n");
        //        var input = Console.ReadLine();
        //        var rootPath = Path.Combine(path, extractableFolder.folderName);
        //        if (input != null && input == "y")
        //        {
        //            switch (Path.GetFileName(extractableFolder.folderName))
        //            {
        //                case "Defs":
        //                    extraction.AddRange(Extractor.ExtractDefs(rootPath, refPaths));
        //                    break;
        //                case "Keyed":
        //                    extraction.AddRange(Extractor.ExtractKeyed(rootPath));
        //                    break;
        //                case "Strings":
        //                    extraction.AddRange(Extractor.ExtractStrings(rootPath));
        //                    break;
        //                case "Patches":
        //                    extraction.AddRange(Extractor.ExtractPatches(rootPath));
        //                    break;
        //                default:
        //                    Console.WriteLine("지원하지 않는 폴더입니다.");
        //                    continue;
        //            }
        //        }
        //    }

        //    Console.WriteLine($"count: {extraction.Count}");

        //    Console.WriteLine("출력 형식을 선택하세요");
        //    Console.WriteLine("1. xlsx");
        //    Console.WriteLine("2. xml");

        //    switch (Console.ReadLine())
        //    {
        //        case "1":
        //            IO.ToExcel(extraction, Path.Combine(metaData.Identifier.StripInvaildChars(), metaData.Identifier.StripInvaildChars()));
        //            break;
        //        case "2":
        //            IO.ToLanguageXml(extraction, true, metaData.Identifier.StripInvaildChars(), metaData.Identifier.StripInvaildChars());
        //            break;
        //    }
        //    Console.WriteLine("완료되었습니다. 아무키나 눌러 처음 화면으로 돌아갑니다");
        //    Console.ReadKey();
        //    goto Entry;

        //Work2:
        //    Console.WriteLine("xlsx의 파일 경로를 입력하세요. (전체 경로 + 파일 확장자 이름까지)");
        //    var xlsxPath = Console.ReadLine()!;
        //    var fileName = Path.GetFileNameWithoutExtension(xlsxPath);
        //    var lst = IO.FromExcel(xlsxPath);
        //    IO.ToLanguageXml(lst, true, fileName, fileName);
        //    Console.WriteLine("완료되었습니다. 아무키나 눌러 처음 화면으로 돌아갑니다");
        //    Console.ReadKey();
        //    goto Entry;
        }
    }
}