using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using Irihi.Dogma.Docs;

namespace Irihi.Dogma.Demo.ViewModels;

/// <summary>TreeView 适配节点：分类节点（含子项与共存页面）。</summary>
public sealed class DocTreeItem
{
    private readonly DocCategoryNode _node;

    public DocTreeItem(DocCategoryNode node)
    {
        _node = node;
    }

    /// <summary>标题 = 共存页面的 Title（无页面容器为 null，菜单可显示空/隐藏）。</summary>
    public IObservable<string?>? Title => _node.Page?.Title;

    public bool IsPage => _node.Page is not null;

    public DocPageNode? Page => _node.Page;

    public IReadOnlyList<DocTreeItem> Children { get; set; } = [];
}

public partial class MainWindowViewModel : ObservableObject
{
    private readonly DocSite _site;

    public MainWindowViewModel()
    {
        _site = DemoDocSite.Default;
    }

    /// <summary>是否使用亮色主题（演示 Avalonia 原生 RequestedThemeVariant 切换）。</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RequestedTheme))]
    private bool _useLightTheme;

    public ThemeVariant RequestedTheme => UseLightTheme ? ThemeVariant.Light : ThemeVariant.Dark;

    /// <summary>当前选中的树节点（TreeView 菜单用）。</summary>
    [ObservableProperty]
    private DocTreeItem? _selectedTreeItem;

    /// <summary>当前内容区呈现的页面 VM。</summary>
    [ObservableProperty]
    private object? _currentContent;

    /// <summary>左侧 TreeView 的多层菜单（从本实例 Roots 递归构建）。</summary>
    public IReadOnlyList<DocTreeItem> TreeItems =>
        _site.Roots.Select(BuildTreeItem).ToList();

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
            // 经本实例的 provider 获取 VM（默认每次新建；宿主可注入缓存/DI）
            CurrentContent = _site.ViewModelProvider.GetViewModel(page);
        }
    }

    private static DocTreeItem BuildTreeItem(DocCategoryNode node)
    {
        var item = new DocTreeItem(node);
        item.Children = node.Children.Select(BuildTreeItem).ToList();
        return item;
    }
}
