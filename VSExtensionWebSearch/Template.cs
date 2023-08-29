using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;

namespace VSExtensionWebSearch
{
    public interface IReadonlyTemplateInfo
    {
        string Title { get; }
        string Template { get; }
        bool Enabled { get; }
    }

    public class TemplateInfo : IReadonlyTemplateInfo
    {
        public string Title { get; set; } = Constants.DefaultTemplate.Item1;
        public string Template { get; set; } = Constants.DefaultTemplate.Item2;
        public bool Enabled { get; set; } = true;

        public TemplateInfo() { }

        public TemplateInfo(string title, string template, bool enabled)
        {
            Title = title;
            Template = template;
            Enabled = enabled;
        }
    }

    internal interface IReadonlyTemplateItem : IReadonlyTemplateInfo
    {
        bool IsError { get; }
        string Error { get; }

        bool TryGetUrl(string selectedText, out string url);
        IReadonlyTemplateItem Clone();
    }

    internal class TemplateItem : TemplateInfo, IReadonlyTemplateItem
    {
        private static readonly Regex s_splitter = new Regex(@"{{QUERY}}");
        public bool IsError { get; private set; } = false;
        public string Error { get; private set; } = string.Empty;
        private readonly string templateStart = string.Empty;
        private readonly string templateEnd = string.Empty;

        public TemplateItem(string title, string template, bool enabled)
        {
            Title = title;
            Template = template;
            Enabled = enabled;

            string[] se = s_splitter.Split(Template, 2);
            IsError = se.Length < 2;
            if (IsError)
            {
                Error = $"Search query template:\n\n    \"{Template}\"\n\ndoes not have a required:\n\n    \"{@"{{QUERY}}"}\".\n";
            }
            else
            {
                templateStart = se[0];
                templateEnd = se[1];
            }
        }

        public bool TryGetUrl(string queryString, out string url)
        {
            if (IsError)
            {
                url = Error;
                return false;
            }
            url = $"{templateStart}{HttpUtility.UrlEncode(queryString)}{templateEnd}";
            return true;
        }

        public IReadonlyTemplateItem Clone() => new TemplateItem(Title, Template, Enabled);
    }

    internal class TemplateCollection
    {
        public int BaseCommandId => Constants.BASE_COMMAND_ID;

        private static TemplateCollection s_ins = null;
        public static TemplateCollection Instance => s_ins ?? (s_ins = new TemplateCollection());
        private TemplateCollection() { }

        private SortedList<int, TemplateItem> templateItems = new SortedList<int, TemplateItem>();

        /// <summary>
        /// Список шаблонов(точнее только заголовков), где Enabled:true.
        /// Ключи всегда начинаются с BaseCommandId и инкрементируются как +1,
        /// это означает что равные по длине списки всегда будут иметь одинаковые ключи.
        /// </summary>
        public SortedList<int, string> TitleItems
        {
            get
            {
                var sl = new SortedList<int, string>();
                foreach (var item in templateItems)
                {
                    sl.Add(item.Key, item.Value.Title);
                }
                return sl;
            }
        }

        /// <summary>
        /// Этот метод необходимо вызывать после активации расширения и после каждого изменения на странице опций.
        /// Инициирует событие TitleItemsChanged, если добавлен/удален/изменен хотя бы один заголовок.
        /// </summary>
        public void UpdateFrom(IReadOnlyList<IReadonlyTemplateInfo> list)
        {
            // Если удалены все шаблоны, установим один элемент по умолчанию.
            if (list == null || list.Count == 0 || list.All(x => !x.Enabled)) list = new List<TemplateInfo>(new TemplateInfo[] { new TemplateInfo() });
            var sl = new SortedList<int, TemplateItem>();
            var id = BaseCommandId;
            foreach (var item in list)
            {
                if (!item.Enabled) continue;
                sl.Add(id, new TemplateItem(item.Title, item.Template, item.Enabled));
                id++;
            }

            var changed = sl.Count != templateItems.Count;
            if (changed)
            {
                templateItems = sl;
            }
            else
            {
                var keys = sl.Keys;
                foreach (var key in keys)
                {
                    if (!changed) changed = templateItems[key].Title != sl[key].Title;
                    templateItems[key] = sl[key];
                }
            }

            if (changed) OnChanged();
        }

        /// <summary>
        /// Сопоставляет Id команды и:
        ///   + в случае успеха, устанавливает строку поискового запроса.
        ///   + при неудаче возвращает строку с ошибкой.
        /// </summary>
        public bool TryGetUrl(int id, string queryString, out string url)
        {
            if (templateItems.ContainsKey(id))
            {
                return templateItems[id].TryGetUrl(queryString, out url);
            }
            url = $"Oops :(\n\nCommand id:{id} not found.\n";
            return false;
        }

        /// <summary>
        /// Вызывает событие изменения список команд(кнопок поиска).
        /// </summary>
        public event EventHandler<SortedList<int, string>> TitleItemsChanged = null;
        private void OnChanged() => TitleItemsChanged?.Invoke(this, TitleItems);
    }
}
