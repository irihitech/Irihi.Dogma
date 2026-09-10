using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using Irihi.Dogma.Docs;
using Irihi.Lingua;

namespace Irihi.Dogma.Controls.ViewModels;

/// <summary>
/// 文档站外壳 VM：由 DocSite 生成菜单（NavMenu 绑定）、管理导航、页面元信息与主题切换。
/// 页面视图经宿主注入的 <see cref="ViewLocator"/>（各 app 的 GeneratedViewLocator）解析。
/// </summary>
public partial class DocShellViewModel : ObservableObject
{
    private readonly DocSite _site;

    public DocShellViewModel(DocSite site, ILinguaManager? manager = null)
    {
        _site = site;
        if (manager is not null)
        {
            Managers = [manager];
        }
        Menu = new DocMenuViewModel(site, manager);
    }

    /// <summary>关联的 DocSite 实例。</summary>
    public DocSite Site => _site;

    /// <summary>左侧导航菜单（由 DocSite 分类树生成，供 u:NavMenu 绑定）。</summary>
    public DocMenuViewModel Menu { get; }

    /// <summary>CulturePicker 的 manager 列表（构造时注入的 Lingua manager）。</summary>
    public IList<ILinguaManager> Managers { get; set; } = [];

    /// <summary>CulturePicker 可切换的文化列表（默认仅 InvariantCulture，由宿主覆盖）。</summary>
    public IList<LinguaCulture> Cultures { get; set; } = [LinguaCulture.InvariantCulture];

    /// <summary>窗口标题（可观察，随语言切换更新）。</summary>
    public IObservable<string?>? Title { get; set; }

    /// <summary>当前选中的菜单项。</summary>
    [ObservableProperty]
    private DocMenuItemViewModel? _selectedMenuItem;

    /// <summary>菜单搜索文本（过滤 DocMenuViewModel）。</summary>
    [ObservableProperty]
    private string? _searchText;
    
    /// <summary>当前页面 VM。</summary>
    [ObservableProperty]
    private object? _currentContent;

    /// <summary>页面 VM→View 的定位器（宿主注入 app 的 GeneratedViewLocator）。</summary>
    public IDataTemplate? ViewLocator { get; set; }

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
        // 经 site 的 provider 获取 VM（默认每次新建；宿主可在 site 上注入缓存/DI）
        var vm = _site.ViewModelProvider.GetViewModel(page);
        CurrentContent = vm;
        PageMetadata = vm is IPageMetadataProvider provider ? provider.PageMetadata : null;
    }
}
