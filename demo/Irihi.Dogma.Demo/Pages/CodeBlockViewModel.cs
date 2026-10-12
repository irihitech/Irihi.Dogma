using Irihi.Dogma.Docs;

namespace Irihi.Dogma.Demo.Pages;

[DocCategory("Docs_CodeBlock", Parent = "Docs_DocumentationControls", Order = 0)]
[DocPage("Docs_CodeBlock_Title", View = typeof(CodeBlockView),
    Keywords = new[] { "codeblock", "syntax", "axaml", "csharp", "bash" })]
public sealed partial class CodeBlockViewModel
{
    public string AxamlCode { get; } = """
        <!-- A simple Avalonia layout -->
        <StackPanel Spacing="8">
            <TextBlock Text="{Binding Greeting}" />
            <Button Content="Say hello" Command="{Binding GreetCommand}" />
        </StackPanel>
        """;

    public string CSharpCode { get; } = """
        // Build a greeting for the demo.
        public class Greeter
        {
            public string Greet(string name)
            {
                var count = 3;
                return $"Hello, {name}! Count: {count}";
            }
        }
        """;

    public string BashCode { get; } = """
        #!/bin/bash
        # Display a greeting for each language.
        for language in AXAML CSharp Bash; do
            echo "Hello, $language!"
        done

        count=3
        if [ "$count" -gt 0 ]; then
            printf '%s\n' 'All three languages are supported.'
        fi
        """;
}
