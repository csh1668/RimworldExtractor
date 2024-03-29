using RimworldExtractorInternal;

namespace RimworldExtractorTest
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        { 
            Prefabs.Init();
            Prefabs.PathRimworld = "C:\\Games\\Steam\\steamapps\\common\\RimWorld";
            Prefabs.PathWorkshop = "C:\\Games\\Steam\\steamapps\\workshop\\content\\294100";
            foreach (var path in new[]
                     {
                         "Data\\2997308585\\a.xlsx", "Data\\test\\2997308585.xlsx",
                         "Data\\Nephilim Xenotype - 2997308585\\a.xlsx",
                         "Data\\test\\Nephilim Xenotype - 2997308585.xlsx"
                     })
            {
                var modMetadata = TranslationAnalyzerTool.GetModMetadataFromFilePath(path);
                Assert.IsNotNull(modMetadata);
                Console.WriteLine(modMetadata);
            }
            var targetPath =
                "Data\\2997308585\\a.xlsx";
            
        }
    }
}