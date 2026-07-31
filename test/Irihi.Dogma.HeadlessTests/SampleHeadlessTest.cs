using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Xunit;

namespace Irihi.Dogma.HeadlessTests;

public class SampleHeadlessTest
{
    [AvaloniaFact]
    public void Window_Should_Show()
    {
        var window = new Window();
        window.Show();
        Assert.True(window.IsVisible);
    }
}
