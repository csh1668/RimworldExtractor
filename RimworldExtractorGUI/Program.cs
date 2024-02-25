using System.Diagnostics;
using System.Reflection;

namespace RimworldExtractorGUI
{
    /*
    * TODO:
    * Patches 자동 생성 방식 개선
    */


    /*
     * 작업 내용:
     * Internal)
     * 1. Translation Handle 태그의 앞에 '*'가 붙으면 이는 Type 타입으로 간주하여, 노말라이징을 다르게 수행하도록 변경 (바익 탈영병 MVCF.Comp_VerbProps 관련 문제)
     * 2. PatchOperationAdd 수행 시, xpath로 있는 Def이 참조 모드의 Def이면 추출을 못하는 문제 해결 (바익 페르소나 무기 관련 문제)
     * 3. Common 폴더 고려
     * 4. XMLExtension의 설정이 Patches에 있을 경우도 고려 ([FSF] FrozenSnowFox Tweaks 관련 문제)
     * 5. Patches 자동 생성 방식 개선 (매번 FindMod -> Replace가 아니라, FindMod가 같은 애들끼리는 하나의 Sequence에서 동작하도록)
     * 6. 저장방식 기본값을 Languages XML로 변경 (알파 추출기 기본값과 동일하게 하기 위함)
     *
     * GUI)
     * 1. UI 개선
     * 2. 
     */
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomainOnAssemblyResolve;
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            if (!File.Exists("Prefabs.dat"))
            {
                var formInitialPathSelect = new FormInitialPathSelect();
                formInitialPathSelect.StartPosition = FormStartPosition.CenterScreen;
                if (formInitialPathSelect.ShowDialog() != DialogResult.OK)
                {
                    MessageBox.Show("폴더 지정을 완료해주세요.");
                    return;
                }
                // Application.Run();
            }

            var formMain = new FormMain();
            formMain.StartPosition = FormStartPosition.CenterScreen;
            Application.Run(formMain);
        }

        private static Assembly? CurrentDomainOnAssemblyResolve(object? sender, ResolveEventArgs args)
        {
            // Ignore missing resources
            if (args.Name.Contains(".resources"))
                return null;

            // check for assemblies already loaded
            Assembly? assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.FullName == args.Name);
            if (assembly != null)
                return assembly;

            string filename = args.Name.Split(',')[0] + ".dll".ToLower();
            var assemblyFilePath = Path.Combine("bin", filename);

            if (File.Exists(assemblyFilePath))
            {
                try
                {
                    return Assembly.LoadFrom(assemblyFilePath);
                }
                catch
                {
                    return null;
                }
            }
            return null;
        }
    }
}