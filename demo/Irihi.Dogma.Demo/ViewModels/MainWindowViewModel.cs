using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using Irihi.Dogma.Docs;

namespace Irihi.Dogma.Demo.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    /// <summary>是否使用亮色主题（演示 Avalonia 原生 RequestedThemeVariant 切换）。</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RequestedTheme))]
    private bool _useLightTheme;

    public ThemeVariant RequestedTheme => UseLightTheme ? ThemeVariant.Light : ThemeVariant.Dark;

    /// <summary>搜索文本（变化时过滤页面列表）。</summary>
    [ObservableProperty]
    private string _searchText = string.Empty;

    /// <summary>当前选中的页面。</summary>
    [ObservableProperty]
    private DocPageNode? _selectedPage;

    /// <summary>当前内容区呈现的页面 VM。</summary>
    [ObservableProperty]
    private object? _currentContent;

    /// <summary>左侧列表：空搜索显示全部页面，否则走 DocSite 搜索。</summary>
    public IReadOnlyList<DocPageNode> VisiblePages =>
        string.IsNullOrWhiteSpace(SearchText)
            ? DocSite.Instance.AllPages.ToList()
            : DocSite.Instance.Search(SearchText).ToList();

    partial void OnSearchTextChanged(string value) => OnPropertyChanged(nameof(VisiblePages));

    partial void OnSelectedPageChanged(DocPageNode? value)
    {
        if (value is not null)
        {
            // 经 provider 获取 VM（默认每次新建；宿主可注入缓存/DI）
            CurrentContent = DocSite.Instance.ViewModelProvider.GetViewModel(value);
        }
    }
}
