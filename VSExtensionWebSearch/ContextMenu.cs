using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;

using Microsoft;
using Microsoft.VisualStudio.Shell;

using Task = System.Threading.Tasks.Task;

namespace VSExtensionWebSearch
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class ContextMenu
    {
        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid GuidCommandSet = new Guid(Constants.GUID_CONTEXT_MENU_CMD_SET);

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;

        /// <summary>
        /// Выбор выделенного текста в редакторе.
        /// </summary>
        private readonly EditorHelper editorHelper;
        /// <summary>
        /// Коллекция шаблонов.
        /// Каждый шаблон сопоставляется с id команды.
        /// </summary>
        private readonly TemplateCollection templateCollection;
        /// <summary>
        /// Зарегистрированные в настоящий момент команды - id:Title.
        /// При обновлении удаляются команды не находящиеся в templateCollection и добавляются(либо изменяется заголовок) новые.
        /// </summary>
        private readonly SortedList<int, OleMenuCommand> commandList = new SortedList<int, OleMenuCommand>();

        /// <summary>
        /// Initializes a new instance of the <see cref="ContextMenu"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="editorHelper"></param>
        private ContextMenu(AsyncPackage package, EditorHelper editorHelper)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            this.editorHelper = editorHelper ?? throw new ArgumentNullException(nameof(editorHelper));

            var option = package.GetDialogPage(typeof(Options)) as Options;
            Assumes.Present<Options>(option);

            templateCollection = TemplateCollection.Instance;
            option.TemplateChanged += (object _, IReadOnlyList<IReadonlyTemplateInfo> list) => templateCollection.UpdateFrom(list);
            templateCollection.TitleItemsChanged += OnTitleItemsChanged;
            templateCollection.UpdateFrom(option.Templates);
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static ContextMenu Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async Task InitializeAsync(AsyncPackage package)
        {
            // Switch to the main thread - the call to AddCommand in ContextMenu's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);
            await EditorHelper.InitializeAsync(package);
            Assumes.Present<EditorHelper>(EditorHelper.Instance);
            Instance = new ContextMenu(package, EditorHelper.Instance);
        }

        private void OnTitleItemsChanged(object _, SortedList<int, string> list)
        {
            _ = Task.Run(async () =>
            {
                OleMenuCommandService commandService = (OleMenuCommandService)await package.GetServiceAsync(typeof(IMenuCommandService));
                Update(commandService, list);
            });
        }

        private void Update(OleMenuCommandService cs, SortedList<int, string> list)
        {
            // Копируем все имеющиеся ключи.
            var keys = commandList.Keys.ToList();

            // Добавляем или обновляем заголовок.
            foreach (var kv in list)
            {
                var id = kv.Key;
                var title = kv.Value;
                if (!commandList.ContainsKey(id))
                {
                    var menuCommandID = new CommandID(GuidCommandSet, id);
                    var menuItem = new OleMenuCommand(this.Execute, menuCommandID, title);
                    commandList.Add(id, menuItem);
                    // NOTE: Этот метод вызывается при каждом открытии меню, и по несколько раз за одно открытие для каждой кнопки.
                    menuItem.BeforeQueryStatus += CmdQueryStatus;
                    cs.AddCommand(menuItem);
                }
                else if (commandList[id].Text != title)
                {
                    commandList[id].Text = title;
                }
                keys.Remove(id);
            }

            // Удаляем лишнее
            foreach (var key in keys)
            {
                if (commandList.TryGetValue(key, out var menuItem))
                {
                    commandList.Remove(key);
                    menuItem.BeforeQueryStatus -= CmdQueryStatus;
                    cs.RemoveCommand(menuItem);
                }
            }
        }

        private void CmdQueryStatus(object sender, EventArgs e)
        {
            if (!(sender is OleMenuCommand command)) return;
            // Скрываем видимость пункта меню если нет текста. Скрытие всех пунктов скрывает меню полностью.
            // После определения текста на первом пункте с BaseCommandId, для остальных пунктов исключаем вызов SelectedText().
            // TODO: Не найдено решение как избежать повторных вызовов SelectedText(),
            //       BeforeQueryStatus вызывается несколько раз для одного пункта.
            command.Visible = command.CommandID.ID == templateCollection.BaseCommandId
                ? editorHelper.SelectedText()
                : editorHelper.IsValue;
        }

        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (!editorHelper.IsValue ||
                !(sender is OleMenuCommand command) || !(command.CommandID?.ID is int id) ||
                !templateCollection.TryGetUrl(id, editorHelper.Value, out string url)) return;

            System.Diagnostics.Process.Start(url);

            // Только для визуальной проверки.
            //string title = "Search Text";
            //VsShellUtilities.ShowMessageBox(
            //    this.package,
            //    url,
            //    title,
            //    OLEMSGICON.OLEMSGICON_INFO,
            //    OLEMSGBUTTON.OLEMSGBUTTON_OK,
            //    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }
    }
}
