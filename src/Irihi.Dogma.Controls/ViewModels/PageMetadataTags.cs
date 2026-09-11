using Irihi.Dogma.Controls.Localizations;

namespace Irihi.Dogma.Controls.ViewModels;

/// <summary>
/// 常用能力的预设标签工厂（文案来自库内 Lingua 资源，随语言切换）。
/// 页面不需要的标签不添加即可；自定义标签直接 new PageMetadataTagViewModel。
/// </summary>
public static class PageMetadataTags
{
    public static PageMetadataTagViewModel MvvmSupport() => new()
    {
        Text = LanguageManager.Instance.Tag_MvvmSupport,
        Classes = "Blue"
    };

    public static PageMetadataTagViewModel InlineXaml() => new()
    {
        Text = LanguageManager.Instance.Tag_InlineXamlSupport,
        Classes = "Orange"
    };

    public static PageMetadataTagViewModel AvaloniaExclusive() => new()
    {
        Text = LanguageManager.Instance.Tag_AvaloniaExclusive,
        Classes = "Indigo"
    };
}
