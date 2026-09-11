using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Irihi.Dogma.Docs;
using Irihi.Lingua;

namespace Irihi.Dogma.Controls.ViewModels;

/// <summary>
/// 由 <see cref="DocSite"/> 的分类树生成菜单 VM（含搜索过滤），供 NavMenu / TreeView 绑定。
/// </summary>
public class DocMenuViewModel
{
    private readonly ILinguaManager? _manager;

    public DocMenuViewModel(DocSite site, ILinguaManager? manager = null)
    {
        _manager = manager;
        MenuItems = BuildTreeItems(site.Roots);
    }

    public ObservableCollection<DocMenuItemViewModel> MenuItems { get; }

    /// <summary>按分类 Key 深度优先查找菜单项；找不到返回 null。</summary>
    public DocMenuItemViewModel? GetMenuItem(string key)
    {
        return FindItem(MenuItems, key);
    }

    private static DocMenuItemViewModel? FindItem(IEnumerable<DocMenuItemViewModel> items, string key)
    {
        foreach (var item in items)
        {
            if (string.Equals(item.Key, key, StringComparison.Ordinal))
                return item;
            var found = FindItem(item.Children, key);
            if (found is not null)
                return found;
        }
        return null;
    }

    /// <summary>按搜索文本过滤菜单（标题或子项命中则可见）；空文本恢复全部可见。</summary>
    public void FilterMenuItems(string? searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            SetAllVisible(MenuItems);
            return;
        }
        ApplyFilter(MenuItems, searchText);
    }

    private ObservableCollection<DocMenuItemViewModel> BuildTreeItems(IReadOnlyList<DocCategoryNode> nodes)
    {
        var children = nodes
            .OrderBy(a => a.Metadata.Order)
            .Select(BuildTreeItem);
        return new ObservableCollection<DocMenuItemViewModel>(children);
    }

    private DocMenuItemViewModel BuildTreeItem(DocCategoryNode node)
    {
        var item = new DocMenuItemViewModel(node, _manager);
        var children = BuildTreeItems(node.Children);
        item.Children = new ObservableCollection<DocMenuItemViewModel>(children);
        return item;
    }

    private static void SetAllVisible(IEnumerable<DocMenuItemViewModel> items)
    {
        foreach (var item in items)
        {
            item.IsVisible = true;
            if (item.Children.Count > 0)
                SetAllVisible(item.Children);
        }
    }

    private static bool ApplyFilter(IEnumerable<DocMenuItemViewModel> items, string searchText)
    {
        var anyVisible = false;
        foreach (var item in items)
        {
            if (item.IsSeparator)
            {
                item.IsVisible = false;
                continue;
            }

            var headerText = (item.MenuHeader as LinguaObservableString)?.CurrentValue;
            var selfMatches = headerText?.Contains(searchText, StringComparison.OrdinalIgnoreCase) == true;
            var childrenVisible = item.Children.Count > 0 && ApplyFilter(item.Children, searchText);

            item.IsVisible = selfMatches || childrenVisible;
            if (item.IsVisible) anyVisible = true;
        }
        return anyVisible;
    }
}
