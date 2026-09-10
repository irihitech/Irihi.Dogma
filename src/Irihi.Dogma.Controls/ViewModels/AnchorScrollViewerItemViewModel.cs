using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Irihi.Dogma.Controls.ViewModels;

public partial class AnchorScrollViewerItemViewModel: ObservableObject
{
    [ObservableProperty] public partial IObservable<string?>? Header { get; set; }
    [ObservableProperty] public partial string? AnchorId { get; set; }

    public ObservableCollection<AnchorScrollViewerItemViewModel> Children { get; set; } = [];
}
