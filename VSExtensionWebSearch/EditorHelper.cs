using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Microsoft;
using Microsoft.VisualStudio.Shell;

namespace VSExtensionWebSearch
{
    internal class EditorHelper
    {
        private static readonly Regex s_regexClearSpaces = new Regex(@"\s+");

        public static EditorHelper Instance { get; private set; } = null;
        private readonly EnvDTE80.DTE2 dte;

        private EditorHelper(EnvDTE80.DTE2 dte) => this.dte = dte;

        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);
            EnvDTE80.DTE2 dte = await package.GetServiceAsync(typeof(EnvDTE.DTE)) as EnvDTE80.DTE2;
            Assumes.Present<EnvDTE80.DTE2>(dte);
            Instance = new EditorHelper(dte);
        }

        public bool IsValue { get; private set; } = false;
        public string Value { get; private set; } = string.Empty;

        /// <summary>
        /// Получение выбранного в редакторе текста.
        /// </summary>
        public bool SelectedText()
        {
            var tsRaw = dte?.ActiveDocument?.Selection;
            if (tsRaw == null)
            {
                IsValue = false;
                Value = string.Empty;
                return false;
            }

            var textSel = (tsRaw as EnvDTE.TextSelection)?.Text;
            if (string.IsNullOrEmpty(textSel))
            {
                IsValue = false;
                Value = string.Empty;
                return false;
            }

            Value = s_regexClearSpaces.Replace(textSel.Trim(), " ");
            IsValue = !string.IsNullOrEmpty(Value);
            return IsValue;
        }
    }
}
