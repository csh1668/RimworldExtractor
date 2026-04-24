using System.Diagnostics;
using System.Reflection;

namespace RimworldExtractorGUI
{
    /*
    * TODO: https://github.com/RimWorldKorea/RMK/discussions/496#discussioncomment-8651025
    */
    internal static class Program
    {
        /// <summary>
        /// Github Action에 의해 게시 전 자동으로 생성
        /// </summary>
        internal const string VERSION = "";
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
