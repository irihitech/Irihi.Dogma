using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using Irihi.Dogma.Controls.ViewModels;
using Irihi.Dogma.Docs;

namespace Irihi.Dogma.Demo.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly DocSite _site;

    public MainWindowViewModel()
    {
        _site = DemoDocSite.Default;
        // 菜单标题经 Lingua 按资源键解析；缺失键时库内自动回退 FallbackTitle
        Menu = new DocMenuViewModel(_site, key => LanguageManager.Instance.GetObservable(key));
    }

    /// <summary>左侧导航菜单（由 DocSite 分类树生成，供 u:NavMenu 绑定）。</summary>
    public DocMenuViewModel Menu { get; }

    /// <summary>当前选中的菜单项。</summary>
    [ObservableProperty]
    private DocMenuItemViewModel? _selectedMenuItem;

    /// <summary>菜单搜索文本（过滤 DocMenuViewModel）。</summary>
    [ObservableProperty]
    private string? _searchText;

    /// <summary>是否使用亮色主题（演示 Avalonia 原生 RequestedThemeVariant 切换）。</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RequestedTheme))]
    private bool _useLightTheme;

    public ThemeVariant RequestedTheme => UseLightTheme ? ThemeVariant.Light : ThemeVariant.Dark;

    /// <summary>当前内容区呈现的页面 VM。</summary>
    [ObservableProperty]
    private object? _currentContent;

    /// <summary>当前页面的元信息（页面 VM 实现 IPageMetadataProvider 时提供，否则为 null）。</summary>
    [ObservableProperty]
    private PageMetadataViewModel? _pageMetadata;

    partial void OnSearchTextChanged(string? value)
    {
        Menu.FilterMenuItems(value);
    }

    partial void OnSelectedMenuItemChanged(DocMenuItemViewModel? value)
    {
        // 非可点击分类 / 无页面节点不导航
        if (value?.Node.Page is { } page)
        {
            Navigate(page);
        }
    }

    private void Navigate(DocPageNode page)
    {
        // 经本实例的 provider 获取 VM（默认每次新建；宿主可注入缓存/DI）
        CurrentContent = _site.ViewModelProvider.GetViewModel(page);
        PageMetadata = CurrentContent is IPageMetadataProvider provider ? provider.PageMetadata : null;
    }
}
