using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Metadata;
using Irihi.Dogma.Controls.Localizations;
using Irihi.Dogma.Controls.ViewModels;

namespace Irihi.Dogma.Controls;

/// <summary>
/// 文档页 Demo 段落控件：标题 + 描述（Markdown）+ 标签 + 内容 + 代码片段 Tab。
/// 模板见 Themes/DemoSectionView.axaml（经 Themes/Index.axaml 引入）。
/// </summary>
public class DemoSectionView : TemplatedControl
{
    public static readonly StyledProperty<IObservable<string?>?> SectionTagDisplayTextProperty =
        AvaloniaProperty.Register<DemoSectionView, IObservable<string?>?>(nameof(SectionTagDisplayText));

    public static readonly StyledProperty<DemoSectionViewModel?> SectionContextProperty =
        AvaloniaProperty.Register<DemoSectionView, DemoSectionViewModel?>(nameof(SectionContext));

    public static readonly StyledProperty<object?> ContentProperty =
        AvaloniaProperty.Register<DemoSectionView, object?>(nameof(Content));

    static DemoSectionView()
    {
        SectionContextProperty.Changed.AddClassHandler<DemoSectionView>((sender, e) =>
        {
            var sectionContext = e.NewValue as DemoSectionViewModel;
            sender.UpdateSectionTagDisplay(sectionContext?.SectionTag ?? DemoSectionTag.None);
        });
    }

    public DemoSectionViewModel? SectionContext
    {
        get => GetValue(SectionContextProperty);
        set => SetValue(SectionContextProperty, value);
    }

    public IObservable<string?>? SectionTagDisplayText
    {
        get => GetValue(SectionTagDisplayTextProperty);
        private set => SetValue(SectionTagDisplayTextProperty, value);
    }

    /// <summary>演示内容，直接作为 XAML 子内容书写。</summary>
    [Content]
    public object? Content
    {
        get => GetValue(ContentProperty);
        set => SetValue(ContentProperty, value);
    }

    public DemoSectionView()
    {
        SectionContext = new DemoSectionViewModel();
        UpdateSectionTagDisplay(SectionContext.SectionTag);
    }

    private void UpdateSectionTagDisplay(DemoSectionTag sectionTag)
    {
        SectionTagDisplayText = sectionTag switch
        {
            DemoSectionTag.Function => LanguageManager.Instance.DemoSection_Tag_Function,
            DemoSectionTag.Style => LanguageManager.Instance.DemoSection_Tag_Style,
            DemoSectionTag.Others => LanguageManager.Instance.DemoSection_Tag_Others,
            _ => null
        };
    }
}
