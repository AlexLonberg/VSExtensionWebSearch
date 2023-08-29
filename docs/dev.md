
# Детали проекта

Эта документация не относится к использованию расширения и описывает некоторые детали и заметки проекта.

> NOTE: После экспериментов с расширениями сбрасываем VS-Instance-Debug. Набираем в поиске меню пуск `Reset the Visual Studio 2022 Experimental Instance` и вызываем скрипт. Следующий запуск VS-Instance-Debug будет как в первый раз.

К сожалению VS не считает некоторые расширения файлов программным кодом и соответственно не может активировать контекстное меню для некоторых типов файлов. В `VSExtensionWebSearchPackage.vsct` есть решением для `XAML`, для `JSON` решение не найдено.

Нерешенные задачи указаны тегом `TODO`.

## Параметры расширения | `class Options : DialogPage`.

При использовании простых типов(`string|bool|и т.п.`) свойства не вызываю проблем при сохранении/загрузки. С использованием списков возникает проблема:

* Пока VS запущен изменение параметров на странице настроек хранятся в памяти и могут быть использованы расширением.
* После перезапуска IDE параметры восстанавливаются в первоначальные значения, как буд-то их нет в Storage.

Для решения можно использовать 2 подхода:

* Переопределить методы `DialogPage` `SaveSettingsToStorage()/LoadSettingsFromStorage()` с явным получением доступа к Storage. [Пример Storage | stackoverflow](https://stackoverflow.com/a/32808238/12915495)
* Реализация `TypeConverter` для сложных типов свойств. [Пример TypeConverter | stackoverflow](https://stackoverflow.com/q/24291249/12915495).

Вариант `TypeConverter` конвертирует свойство целиком к формату пригодному для хранения и обратно.

---

Ниже описан порядок вызовов конвертера для списка `List<TemplateInfo>` при различных вариантах запуска и редактирования элементов списка. Порядок вызовов может немного изменяться, но эксперимент показал - VS и расширение не запоминают предыдущие запросы и очень часто повторно вызывают методы `CanConvertFrom/CanConvertTo(...)`.

1.1. При запуске VS и первой установке расширения(среда VS-Instance-Debug предварительно сброшена) используются параметры по умолчанию и `TypeConverter` не вызывается.

2.1. Повторный запуск VS так же не вызывает методы `TypeConverter`, если параметры не редактировались.

2.2. Открываем параметры и кликаем на раздел расширения `Web Search`.

2.3. Вызывается 2 раза `CanConvertFrom(ITypeDescriptorContext context, Type sourceType)`, где:
  + sourceType: `{Name = "String" FullName = "System.String"}`
  + context.Value: `System.Collections.Generic.List<VSExtensionWebSearch.TemplateInfo>` - это свойство видно в Debugger, но недоступно в коде.

2.4. Вызывается `ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)`, где:
  + destinationType: `{Name = "String" FullName = "System.String"}`
  + value: `System.Collections.Generic.List<VSExtensionWebSearch.TemplateInfo>`
  + Возвращаем конвертированный в строку результат `return JsonSerializer.Serialize(a)`

2.5. Как и в 2.3 `CanConvertFrom(...)`, но вызывается 1 раз.

2.6. Как и в 2.4 `ConvertTo(...)`, но нужно убрать точку останова, по неизвестной причине на breakpoint функция зацикливается, как буд-то постоянно идет запрос к этой функции. В дальнейшем этот breakpoint, придется везде устанавливать и убирать.

2.7. Открывается окно параметров расширения, кликаем на список и открываем отдельное окно редактирования списка. При редактировании методы не вызываются.

2.8. После изменения свойства в отдельном окне редактирования списка и клика Ok, вызывается `CanConvertFrom(...)` и сразу `ConvertTo(...)`, так же временно убираем breakpoint. Окно зарывается.

2.9. В окне основных настроек кликаем Ok, вызывается `CanConvertTo(...)`, `ConvertTo(...)` все с типами String.

2.10. Вызывается `CanConvertFrom(...)` и `ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)`, где value является валидным JSON и конвертируется `JsonSerializer.Deserialize<List<TemplateInfo>>(s)`.

2.11. Вызывается `CanConvertFrom(...)` и `ConvertFrom(...)`.

2.12. Вызывается `CanConvertTo(...)`. Окно параметров закрывается. Контекстное меню изменено.

2.13. Закрываем VS, вызывается `ConvertTo(...)`, возвращается `JsonSerializer.Serialize(a)` и вызывается `CanConvertFrom(...)`. VS завершает работу.

3.1. Открытие VS с ранее отредактированным свойством.

3.2. При попытке использовать контекстное меню вызывается `CanConvertFrom(...)` и `ConvertFrom(...)`. Строка конвертируется к `JsonSerializer.Deserialize<List<TemplateInfo>>(s)` и меню открывается с ранее сохраненными настройками. Эти методы могут быть вызваны и до использования контекстного меню, если VS завершил все фоновые процессы запуска и решил активировать расширение.

3.3. Если не менять параметры, после закрытия VS ничего не происходит.

---

В ходе первых экспериментов с реализацией `TypeConverter` и различными вариантами конвертаций, один раз `CanConvertFrom(..., sourceType)` потребовал конвертировать в `{Name = "JArray" FullName = "Newtonsoft.Json.Linq.JArray"}`. В конечной реализации этого не происходит. На заметку оставил вариант такой проверки и [конвертации | stackoverflow](https://stackoverflow.com/a/13565373/12915495):

```cs
public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) {
  bool can = sourceType == typeof(Newtonsoft.Json.Linq.JArray);
  // ...
  return can;
}

public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) {
  var a = value as Newtonsoft.Json.Linq.JArray;
  if (a != null) return a.ToObject<List<TemplateInfo>>();
  // ...
}
```

## Файлы

Файл `VSExtensionWebSearch.csproj.user` не сохранен в репозитории, но на Windows11 имеет такое содержимое:

```xml
<?xml version="1.0" encoding="utf-8"?>
<Project ToolsVersion="Current" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <PropertyGroup Condition="'$(Configuration)|$(Platform)' == 'Debug|x64'">
    <StartProgram>C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE\devenv.exe</StartProgram>
    <StartArguments>/rootsuffix Exp</StartArguments>
    <StartAction>Program</StartAction>
  </PropertyGroup>
  <PropertyGroup Condition="'$(Configuration)|$(Platform)' == 'Release|x64'">
    <StartProgram>C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE\devenv.exe</StartProgram>
    <StartArguments>/rootsuffix Exp</StartArguments>
    <StartAction>Program</StartAction>
  </PropertyGroup>
</Project>
```
