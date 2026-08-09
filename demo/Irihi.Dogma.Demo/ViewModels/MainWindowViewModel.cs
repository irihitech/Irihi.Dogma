using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using Irihi.Dogma.Docs;

namespace Irihi.Dogma.Demo.ViewModels;

/// <summary>TreeView 适配节点：分类节点（含子项与页面）或页面节点。</summary>
public sealed class DocTreeItem
{
    private readonly DocCategoryNode? _category;
    private readonly DocPageNode? _page;

    public DocTreeItem(DocCategoryNode? category, DocPageNode? page)
    {
        _category = category;
        _page = page;
    }

    /// <summary>分类标题（GetObservable）或页面标题。</summary>
    public IObservable<string?> Title => _page?.Title ?? _category!.Title;

    public bool IsPage => _page is not null;

    public DocPageNode? Page => _page;

    public IReadOnlyList<DocTreeItem> Children { get; set; } = [];
}

public partial class MainWindowViewModel : ObservableObject
{
    /// <summary>是否使用亮色主题（演示 Avalonia 原生 RequestedThemeVariant 切换）。</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RequestedTheme))]
    private bool _useLightTheme;

    public ThemeVariant RequestedTheme => UseLightTheme ? ThemeVariant.Light : ThemeVariant.Dark;

    /// <summary>搜索文本（非空时左侧切换为搜索结果列表）。</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSearching))]
    private string _searchText = string.Empty;

    /// <summary>当前选中的页面（搜索结果列表用）。</summary>
    [ObservableProperty]
    private DocPageNode? _selectedPage;

    /// <summary>当前选中的树节点（TreeView 菜单用）。</summary>
    [ObservableProperty]
    private DocTreeItem? _selectedTreeItem;

    /// <summary>当前内容区呈现的页面 VM。</summary>
    [ObservableProperty]
    private object? _currentContent;

    /// <summary>是否处于搜索模式（隐藏 TreeView，显示结果列表）。</summary>
    public bool IsSearching => !string.IsNullOrWhiteSpace(SearchText);

    /// <summary>左侧 TreeView 的多层菜单（从 DocSite.Roots 递归构建）。</summary>
    public IReadOnlyList<DocTreeItem> TreeItems =>
        DocSite.Instance.Roots.Select(BuildTreeItem).ToList();

    /// <summary>搜索结果列表。</summary>
    public IReadOnlyList<DocPageNode> VisiblePages =>
        DocSite.Instance.Search(SearchText).ToList();

    partial void OnSearchTextChanged(string value)
    {
        OnPropertyChanged(nameof(IsSearching));
        OnPropertyChanged(nameof(TreeItems));
        OnPropertyChanged(nameof(VisiblePages));
    }

    partial void OnSelectedPageChanged(DocPageNode? value) => Navigate(value);

    partial void OnSelectedTreeItemChanged(DocTreeItem? value)
    {
        // 分类节点本身不可点击/无页面时不导航；选中页面节点才显示内容
        if (value?.Page is { } page)
        {
            Navigate(page);
        }
    }

    private void Navigate(DocPageNode? page)
    {
        if (page is not null)
        {
            // 经 provider 获取 VM（默认每次新建；宿主可注入缓存/DI）
            CurrentContent = DocSite.Instance.ViewModelProvider.GetViewModel(page);
        }
    }

    private static DocTreeItem BuildTreeItem(DocCategoryNode node)
    {
        var item = new DocTreeItem(node, null);
        var children = new List<DocTreeItem>();
        foreach (var child in node.Children)
        {
            children.Add(BuildTreeItem(child));
        }

        if (node.Page is { } page)
        {
            children.Add(new DocTreeItem(null, page));
        }

        item.Children = children;
        return item;
    }
}
