using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text.Json;

using Microsoft.VisualStudio.Shell;

namespace VSExtensionWebSearch
{
    // DOC: DialogPage https://learn.microsoft.com/en-us/dotnet/api/microsoft.visualstudio.shell.dialogpage?view=visualstudiosdk-2022
    public class Options : DialogPage
    {
        [Category("Templates")]
        [DisplayName("Query Templates")]
        [Description("Open and edit the collection ...\nExample:\n  Title: \"Google with Microsoft\"\n  Template: \"https://www.google.com/search?as_q={{QUERY}}&as_sitesearch=microsoft.com\"")]
        [TypeConverter(typeof(ListConverter))]
        public List<TemplateInfo> Templates { get; set; } = Constants.DefaultTemplateCollection;

        public override void ResetSettings() => Templates = Constants.DefaultTemplateCollection;

        // Вызывается при сохранении - клика на Ok основного окна параметров.
        protected override void OnApply(PageApplyEventArgs e)
        {
            base.OnApply(e);
            var list = Templates.Select(x => new TemplateInfo(x.Title, x.Template, x.Enabled)).ToList();
            OnChanged(list);
        }

        public event EventHandler<IReadOnlyList<IReadonlyTemplateInfo>> TemplateChanged = null;
        private void OnChanged(IReadOnlyList<IReadonlyTemplateInfo> list) => TemplateChanged?.Invoke(this, list);
    }

    // DOC: TypeConverter - Для сериализации свойства Options.Templates используем TypeConverter,
    // в противном случае свойство не сохранится и после перезагрузки VS будет использован список по умолчанию.
    // Справка TypeConverter https://learn.microsoft.com/en-us/dotnet/api/system.componentmodel.typeconverter?view=netframework-4.7.2
    //
    // NOTE: В ходе экспериментов было выяснено, что использование атрибута на свойстве Options.Templates
    //   [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
    //   public List<TemplateInfo> Templates ...
    // изменяет поведение TypeConverter, методы CanConvertTo(...) и ConvertFrom(...) не вызываются,
    // список не сохраняется и не восстанавливается после перезапуска VS.
    //
    // Для исследования вызовов(устоновкой breakpoint) не используем краткие записи - растягиваем код на несколько строк.
    // Эксперирмент показал - destinationType всегда требует {Name = "String" FullName = "System.String"}
    public class ListConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            bool can = sourceType == typeof(string);
            if (can)
            {
                return true;
            }
            can = base.CanConvertFrom(context, sourceType);
            return can;
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            bool can = destinationType == typeof(string);
            if (can)
            {
                return true;
            }
            can = base.CanConvertFrom(context, destinationType);
            return can;
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            var s = value as string;
            if (s != null)
            {
                return JsonSerializer.Deserialize<List<TemplateInfo>>(s);
            }
            return base.ConvertFrom(context, culture, value);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            var a = value as List<TemplateInfo>;
            if (a != null && destinationType == typeof(string))
            {
                return JsonSerializer.Serialize(a);
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}
