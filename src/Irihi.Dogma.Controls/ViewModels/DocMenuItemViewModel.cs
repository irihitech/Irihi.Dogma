using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Irihi.Dogma.Docs;
using Irihi.Lingua;

namespace Irihi.Dogma.Controls.ViewModels;

/// <summary>
/// DocSite 分类树节点的菜单适配 VM（供 NavMenu / TreeView 等绑定）。
/// 标题经 Lingua 解析器按 TitleKey 解析；未提供解析器时回退 FallbackTitle 字面量。
/// </summary>
public partial class DocMenuItemViewModel : ObservableObject
{
    public IObservable<string?>? MenuHeader { get; set; }
    public string? Key { get; set; }
    public string? Status { get; set; }
    public DocCategoryNode Node { get; }

    public bool IsSeparator { get; set; }
    public ObservableCollection<DocMenuItemViewModel> Children { get; set; } = [];

    [ObservableProperty] public partial bool IsVisible { get; set; } = true;

    public DocMenuItemViewModel(DocCategoryNode node, Func<string, IObservable<string?>?>? titleResolver = null)
    {
        var titleKey = node.Page?.Metadata.TitleKey;
        MenuHeader = titleKey is null
            ? null
            : titleResolver?.Invoke(titleKey) ??
              LinguaObservableString.FromLiteral(node.Page!.Metadata.FallbackTitle ?? titleKey);
        Key = node.Metadata.Key;
        Status = node.Metadata.Tags.FirstOrDefault();
        Node = node;
    }
}
