using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Irihi.Dogma.Controls.ViewModels;

public partial class PageMetadataViewModel: ObservableObject
{
    [ObservableProperty] public partial IObservable<string?>? Title { get; set; }
    [ObservableProperty] public partial IObservable<string?>? Description { get; set; }
    [ObservableProperty] public partial IReadOnlyList<BreadcrumbItemData>? Breadcrumbs { get; set; }

    /// <summary>能力/状态标签；留空则不渲染（如 Introduction 这类页面）。</summary>
    public IReadOnlyList<PageMetadataTagViewModel> Tags { get; set; } = [];
}
