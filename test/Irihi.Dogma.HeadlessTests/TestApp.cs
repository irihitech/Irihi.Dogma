using Avalonia;
using Avalonia.Headless;

[assembly: AvaloniaTestApplication(typeof(Irihi.Dogma.HeadlessTests.TestApp))]

namespace Irihi.Dogma.HeadlessTests;

public class TestApp : Application
{
    public override void Initialize()
    {
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<TestApp>()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions());
}
