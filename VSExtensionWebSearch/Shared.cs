using System;
using System.Collections.Generic;

namespace VSExtensionWebSearch
{
    /// <summary>
    /// Общие свойства. Все GUID должны быть скопированы из .vsct
    /// </summary>
    internal static class Constants
    {
        public const string PACKAGE_GUID_STRING = "67a97189-970c-4250-bfba-865d9b9bd0d6";
        public const string GUID_CONTEXT_MENU_CMD_SET = "555AE8D4-5A34-4809-B9FD-E6FD621E27EA";
        public const string GUID_ACTIVATION_CONTEXT_STRING = "E66E4D80-9569-40A6-9B35-2EEB6C5B643C";
        // Скопировано из .vsct Symbols-GuidSymbol-IDSymbol-name=ContextMenuList
        // Это начальный идентификатор для первой кнопки. Остальные кнопки инкрементируются.
        public const int BASE_COMMAND_ID = 0x0100;

        /// <summary>
        /// Шаблон по умолчанию. Используется как новый элемент в настройках и как шаблон по умолчанию при пустом списке.
        /// </summary>
        public static readonly Tuple<string, string> DefaultTemplate =
            new Tuple<string, string>("Google", "https://www.google.com/search?q={{QUERY}}");

        /// <summary>
        /// Шаблоны по умолчанию для установки в DialogPage.
        /// </summary>
        public static List<TemplateInfo> DefaultTemplateCollection => new List<TemplateInfo>()
        {
            // Через Google
            new TemplateInfo("Google", "https://www.google.com/search?q={{QUERY}}", true),
            new TemplateInfo("Microsoft",  "https://www.google.com/search?as_q={{QUERY}}&as_sitesearch=microsoft.com", true),
            new TemplateInfo("StackOverflow", "https://www.google.com/search?as_q={{QUERY}}&as_sitesearch=stackoverflow.com", true),
            // ... отключено
            new TemplateInfo("MDNWeb", "https://www.google.com/search?as_q={{QUERY}}&as_sitesearch=developer.mozilla.org", false),
            new TemplateInfo("NodeJS", "https://www.google.com/search?as_q={{QUERY}}&as_sitesearch=nodejs.org", false),
            new TemplateInfo("RustLang", "https://www.google.com/search?as_q={{QUERY}}&as_sitesearch=doc.rust-lang.org", false),
            new TemplateInfo("Python", "https://www.google.com/search?as_q={{QUERY}}&as_sitesearch=docs.python.org", false),
            new TemplateInfo("GoLang", "https://www.google.com/search?as_q={{QUERY}}&as_sitesearch=go.dev", false),
            // Собственные поисковые системы
            new TemplateInfo("Microsoft",  "https://www.microsoft.com/en-us/search/explore?q={{QUERY}}", false),
            new TemplateInfo("StackOverflow", "https://stackoverflow.com/search?q={{QUERY}}", false)
        };
    }
}
