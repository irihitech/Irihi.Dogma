using Avalonia;
using Ursa.Controls;

namespace Irihi.Dogma.Controls;

/// <summary>
/// 文档站外壳窗口（基于 <see cref="UrsaWindow"/>，自带 OverlayDialogHost 等能力）：
/// NavMenu 侧栏 + 页面元信息 + 内容区。
/// 配合 <see cref="ViewModels.DocShellViewModel"/> 使用：
/// new DocShellWindow { DataContext = new DocShellViewModel(site, manager) { ... } }。
/// 样式依赖 Themes/Index.axaml 引入的 Semi/Ursa 主题链。
/// </summary>
public partial class DocShellWindow : UrsaWindow
{
    /// <summary>侧栏头部的图标（与 Title 并排显示，如 PathIcon / Image / 任意控件）。</summary>
    public static readonly StyledProperty<object?> HeaderIconProperty =
        AvaloniaProperty.Register<DocShellWindow, object?>(nameof(HeaderIcon));

    public DocShellWindow()
    {
        InitializeComponent();
    }

    public object? HeaderIcon
    {
        get => GetValue(HeaderIconProperty);
        set => SetValue(HeaderIconProperty, value);
    }
}
