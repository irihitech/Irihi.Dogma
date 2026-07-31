using Avalonia;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;

[assembly: AvaloniaTestApplication(typeof(Irihi.Dogma.HeadlessTests.TestApp))]

namespace Irihi.Dogma.HeadlessTests;

public class TestApp : Application
{
    public override void Initialize()
    {
        // 加载 CodeBlock 控件模板样式，供 headless 测试使用。
        Styles.Add(new StyleInclude(new Uri("avares://Irihi.Dogma/"))
        {
            Source = new Uri("avares://Irihi.Dogma/CodeBlock/CodeBlock.axaml"),
        });
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<TestApp>()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions());
}
