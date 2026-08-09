using Irihi.Dogma.Docs;

namespace Irihi.Dogma.Demo.Pages;

[DocCategory("Docs_Input", Parent = "Docs_Controls", Order = 2)]
[DocPage("Docs_Input_Title", View = typeof(InputView), Keywords = new[] { "textbox", "input" })]
public sealed partial class InputViewModel
{
    /// <summary>示例 AXAML 源码（用 CodeBlock 高亮展示）。</summary>
    public string SampleCode { get; } = """
        <StackPanel Spacing="8">
            <TextBox Watermark="Enter your name"/>
            <TextBox Text="{Binding Name, Mode=TwoWay}"/>
            <PasswordBox PasswordChar="●"/>
        </StackPanel>
        """;
}
