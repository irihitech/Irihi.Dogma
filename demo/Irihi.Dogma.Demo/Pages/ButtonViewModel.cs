using Irihi.Dogma.Docs;

namespace Irihi.Dogma.Demo.Pages;

[DocCategory("Docs_Buttons", Parent = "Docs_Controls", Order = 1)]
[DocPage("Docs_Button_Title", View = typeof(ButtonView), Keywords = new[] { "click", "button" })]
public sealed partial class ButtonViewModel
{
    /// <summary>示例 AXAML 源码（用 CodeBlock 高亮展示）。</summary>
    public string SampleCode { get; } = """
        <StackPanel Spacing="8">
            <Button Content="Click me"
                    Command="{Binding GreetCommand}"/>
            <Button Classes="primary"
                    IsEnabled="{Binding !IsBusy}"/>
        </StackPanel>
        """;
}
