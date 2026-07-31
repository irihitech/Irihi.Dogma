using CommunityToolkit.Mvvm.ComponentModel;

namespace Irihi.Dogma.Demo.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _greeting = "Welcome to Irihi.Dogma!";
}
