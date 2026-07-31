using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Irihi.Dogma.Controls;
using Xunit;

namespace Irihi.Dogma.HeadlessTests;

public class CodeBlockTests
{
    [AvaloniaFact]
    public void Inlines_RoundTrip_To_OriginalCode()
    {
        var code = "hello\nworld";
        var block = new CodeBlock { Code = code, Language = CodeLanguage.CSharp };
        var window = new Window { Content = block };
        window.Show();

        var text = block.GetVisualDescendants().OfType<SelectableTextBlock>().First();
        Assert.Equal(code, text.Inlines?.Text);
    }

    [AvaloniaFact]
    public void LineNumbers_Are_1ToN()
    {
        var block = new CodeBlock { Code = "a\nb\nc" };
        var window = new Window { Content = block };
        window.Show();

        var lineNumbers = block.GetVisualDescendants().OfType<TextBlock>()
            .First(t => t.Name == "PART_LineNumbers");
        Assert.Equal("1\n2\n3", lineNumbers.Text);
    }

    [AvaloniaFact]
    public void LineNumbers_Hidden_When_Disabled()
    {
        var block = new CodeBlock { Code = "a\nb", ShowLineNumbers = false };
        var window = new Window { Content = block };
        window.Show();

        var lineNumbers = block.GetVisualDescendants().OfType<TextBlock>()
            .First(t => t.Name == "PART_LineNumbers");
        Assert.False(lineNumbers.IsVisible);
    }

    [AvaloniaFact]
    public void CopyButton_Present_And_Toggleable()
    {
        var block = new CodeBlock { Code = "x" };
        var window = new Window { Content = block };
        window.Show();

        var button = block.GetVisualDescendants().OfType<Button>()
            .First(b => b.Name == "PART_CopyButton");
        Assert.NotNull(button);

        block.ShowCopyButton = false;
        Assert.False(button.IsVisible);

        block.ShowCopyButton = true;
        Assert.True(button.IsVisible);
    }

    [AvaloniaFact]
    public void CopyButton_Click_DoesNot_Throw()
    {
        var block = new CodeBlock { Code = "x" };
        var window = new Window { Content = block };
        window.Show();

        var button = block.GetVisualDescendants().OfType<Button>()
            .First(b => b.Name == "PART_CopyButton");
        button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
    }

    [AvaloniaFact]
    public void Multiline_Code_Measures_Taller_Than_SingleLine()
    {
        // 验证 Run 内含 \n 确实换行（3 行文本测量高度应大于 1 行）。
        var single = new CodeBlock { Code = "x" };
        var multi = new CodeBlock { Code = "x\ny\nz" };
        var window = new Window { Content = new StackPanel { Children = { single, multi } } };
        window.Show();

        var singleText = single.GetVisualDescendants().OfType<SelectableTextBlock>().First();
        var multiText = multi.GetVisualDescendants().OfType<SelectableTextBlock>().First();

        singleText.Measure(Size.Infinity);
        multiText.Measure(Size.Infinity);

        Assert.True(multiText.DesiredSize.Height > singleText.DesiredSize.Height);
    }
}
