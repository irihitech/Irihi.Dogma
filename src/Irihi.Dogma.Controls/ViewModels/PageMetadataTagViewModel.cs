using System;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Irihi.Dogma.Controls.ViewModels;

/// <summary>
/// 页面元信息上的单个标签 chip：文案可观察（随语言切换），Classes 映射主题色调
/// （如 Semi Label 的 "Blue"/"Orange"/"Indigo"），Icon 可选。
/// </summary>
public partial class PageMetadataTagViewModel : ObservableObject
{
    [ObservableProperty] public partial IObservable<string?>? Text { get; set; }
    [ObservableProperty] public partial StreamGeometry? Icon { get; set; }
    [ObservableProperty] public partial string? Classes { get; set; }
}
