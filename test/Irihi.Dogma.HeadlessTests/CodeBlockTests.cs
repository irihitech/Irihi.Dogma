using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Styling;
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

    [AvaloniaFact]
    public void Runs_Carry_Token_Classes()
    {
        var block = new CodeBlock { Code = "public class Foo", Language = CodeLanguage.CSharp };
        var window = new Window { Content = block };
        window.Show();

        var text = block.GetVisualDescendants().OfType<SelectableTextBlock>().First();
        var runs = text.Inlines!.OfType<Run>().ToList();

        var keyword = runs.First(r => r.Text == "public");
        Assert.Contains("token-keyword", keyword.Classes);

        var type = runs.First(r => r.Text == "Foo");
        Assert.Contains("token-type", type.Classes);

        var identifier = runs.First(r => r.Text == "class");
        // class 是关键字（也带 token-keyword），此处验证 Foo 的类别是 token-type 即可
        Assert.DoesNotContain("token-type", keyword.Classes);
    }

    [AvaloniaFact]
    public void Follows_RequestedThemeVariant()
    {
        var block = new CodeBlock { Code = "x" };
        var window = new Window { Content = block, RequestedThemeVariant = ThemeVariant.Dark };
        window.Show();

        var container = block.GetVisualDescendants().OfType<Border>()
            .First(b => b.Name == "PART_Container");
        var text = block.GetVisualDescendants().OfType<SelectableTextBlock>().First();

        Assert.Equal(CodePalette.Dark.Background, container.Background);
        Assert.Equal(CodePalette.Dark.Selection, text.SelectionBrush);

        window.RequestedThemeVariant = ThemeVariant.Light;
        Assert.Equal(CodePalette.Light.Background, container.Background);
        Assert.Equal(CodePalette.Light.Selection, text.SelectionBrush);
    }

    [AvaloniaFact]
    public void ThemeVariant_Switch_Rebuilds_Inlines_With_New_Palette()
    {
        var block = new CodeBlock { Code = "public class Foo", Language = CodeLanguage.CSharp };
        var window = new Window { Content = block, RequestedThemeVariant = ThemeVariant.Dark };
        window.Show();

        var text = block.GetVisualDescendants().OfType<SelectableTextBlock>().First();
        var darkKeyword = text.Inlines!.OfType<Run>().First(r => r.Text == "public");
        var darkColor = darkKeyword.Foreground;

        window.RequestedThemeVariant = ThemeVariant.Light;
        var lightKeyword = text.Inlines!.OfType<Run>().First(r => r.Text == "public");

        Assert.NotEqual(darkColor, lightKeyword.Foreground);
        // round-trip 保持
        Assert.Equal("public class Foo", text.Inlines!.Text);
    }

    [AvaloniaFact]
    public void Palette_Property_Overrides_Default()
    {
        var custom = new CodePalette { Background = Brushes.Purple, Foreground = Brushes.White };
        var block = new CodeBlock { Code = "x", Palette = custom };
        var window = new Window
        {
            Content = block,
            RequestedThemeVariant = ThemeVariant.Dark,
        };
        window.Show();

        var container = block.GetVisualDescendants().OfType<Border>()
            .First(b => b.Name == "PART_Container");
        Assert.Equal(Brushes.Purple, container.Background);

        var text = block.GetVisualDescendants().OfType<SelectableTextBlock>().First();
        Assert.Equal(Brushes.White, text.Foreground);

        // 显式 Palette 不随主题切换
        window.RequestedThemeVariant = ThemeVariant.Light;
        Assert.Equal(Brushes.Purple, container.Background);
    }
}
