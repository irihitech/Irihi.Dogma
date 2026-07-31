using Irihi.Dogma.Controls;
using Irihi.Dogma.Controls.Lexers;
using Xunit;

namespace Irihi.Dogma.Tests;

public class AxamlLexerTests
{
    private static IReadOnlyList<CodeToken> Lex(string code) => AxamlLexer.Tokenize(code);

    private static void AssertRoundTrip(string code)
    {
        var tokens = Lex(code);
        Assert.Equal(code, string.Concat(tokens.Select(t => t.Text)));
    }

    private static void AssertKind(string code, TokenKind kind, string expected)
    {
        var tokens = Lex(code);
        Assert.Contains(tokens, t => t.Kind == kind && t.Text == expected);
    }

    [Fact]
    public void Empty_Input_Produces_No_Tokens()
    {
        Assert.Empty(Lex(string.Empty));
    }

    [Fact]
    public void Simple_Element_RoundTrip_And_Types()
    {
        const string code = """<TextBlock Text="Hi"/>""";
        AssertRoundTrip(code);

        var tokens = Lex(code);
        Assert.Equal(
            new[]
            {
                new CodeToken(TokenKind.XmlPunctuation, "<"),
                new CodeToken(TokenKind.XmlElementName, "TextBlock"),
                new CodeToken(TokenKind.XmlText, " "),
                new CodeToken(TokenKind.XmlAttributeName, "Text"),
                new CodeToken(TokenKind.XmlPunctuation, "="),
                new CodeToken(TokenKind.XmlAttributeValue, "\""),
                new CodeToken(TokenKind.XmlAttributeValue, "Hi"),
                new CodeToken(TokenKind.XmlAttributeValue, "\""),
                new CodeToken(TokenKind.XmlPunctuation, "/"),
                new CodeToken(TokenKind.XmlPunctuation, ">"),
            },
            tokens);
    }

    [Fact]
    public void Namespaced_Attributes_And_Text_Content()
    {
        const string code = """
            <Window xmlns="https://github.com/avaloniaui"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                    x:Class="Irihi.Dogma.Demo.Views.MainWindow">
                <StackPanel/>
            </Window>
            """;
        AssertRoundTrip(code);

        var tokens = Lex(code);
        Assert.Contains(tokens, t => t.Kind == TokenKind.XmlAttributeName && t.Text == "xmlns:x");
        Assert.Contains(tokens, t => t.Kind == TokenKind.XmlAttributeName && t.Text == "x:Class");
        Assert.Contains(tokens, t => t.Kind == TokenKind.XmlElementName && t.Text == "StackPanel");
        // 元素间文本（缩进/换行）保留为 XmlText
        Assert.Contains(tokens, t => t.Kind == TokenKind.XmlText && t.Text.Contains('\n'));
    }

    [Fact]
    public void Binding_MarkupExtension_Is_Decomposed()
    {
        const string code = """<Button Command="{Binding GreetCommand}"/>""";
        AssertRoundTrip(code);

        var tokens = Lex(code);
        Assert.Equal(
            new[]
            {
                new CodeToken(TokenKind.XmlPunctuation, "<"),
                new CodeToken(TokenKind.XmlElementName, "Button"),
                new CodeToken(TokenKind.XmlText, " "),
                new CodeToken(TokenKind.XmlAttributeName, "Command"),
                new CodeToken(TokenKind.XmlPunctuation, "="),
                new CodeToken(TokenKind.XmlAttributeValue, "\""),
                new CodeToken(TokenKind.MarkupExtensionBrace, "{"),
                new CodeToken(TokenKind.MarkupExtensionName, "Binding"),
                new CodeToken(TokenKind.MarkupExtensionParameter, " GreetCommand"),
                new CodeToken(TokenKind.MarkupExtensionBrace, "}"),
                new CodeToken(TokenKind.XmlAttributeValue, "\""),
                new CodeToken(TokenKind.XmlPunctuation, "/"),
                new CodeToken(TokenKind.XmlPunctuation, ">"),
            },
            tokens);
    }

    [Fact]
    public void Nested_MarkupExtension_Is_Decomposed()
    {
        const string code = """<TextBlock Text="{Binding Name, Converter={StaticResource C}}"/>""";
        AssertRoundTrip(code);

        var tokens = Lex(code);
        var braces = tokens.Where(t => t.Kind == TokenKind.MarkupExtensionBrace).Select(t => t.Text).ToList();
        Assert.Equal(new[] { "{", "{", "}", "}" }, braces);

        Assert.Contains(tokens, t => t.Kind == TokenKind.MarkupExtensionName && t.Text == "Binding");
        Assert.Contains(tokens, t => t.Kind == TokenKind.MarkupExtensionName && t.Text == "StaticResource");
        Assert.Contains(tokens, t => t.Kind == TokenKind.MarkupExtensionParameter && t.Text.Contains("Converter"));
    }

    [Fact]
    public void Escaped_Empty_Extension_Is_Plain_Value()
    {
        const string code = """<TextBlock Text="{}"/>""";
        AssertRoundTrip(code);

        var tokens = Lex(code);
        Assert.DoesNotContain(tokens, t => t.Kind == TokenKind.MarkupExtensionBrace);
        Assert.Contains(tokens, t => t.Kind == TokenKind.XmlAttributeValue && t.Text == "{}");
    }

    [Fact]
    public void Comment_Is_Single_Token()
    {
        const string code = "<!-- 注释 -->";
        AssertRoundTrip(code);
        AssertKind(code, TokenKind.XmlComment, "<!-- 注释 -->");
    }

    [Fact]
    public void CData_Is_Single_Token()
    {
        const string code = "<![CDATA[<b>bold</b>]]>";
        AssertRoundTrip(code);
        AssertKind(code, TokenKind.XmlCData, code);
    }

    [Fact]
    public void XmlDeclaration_Is_Single_Token()
    {
        const string code = """<?xml version="1.0" encoding="utf-8"?>""";
        AssertRoundTrip(code);
        AssertKind(code, TokenKind.XmlDeclaration, code);
    }

    [Fact]
    public void EndTag_Is_Recognized()
    {
        const string code = "</Window>";
        AssertRoundTrip(code);

        var tokens = Lex(code);
        Assert.Equal(
            new[]
            {
                new CodeToken(TokenKind.XmlPunctuation, "</"),
                new CodeToken(TokenKind.XmlElementName, "Window"),
                new CodeToken(TokenKind.XmlPunctuation, ">"),
            },
            tokens);
    }

    [Fact]
    public void Full_Document_RoundTrip()
    {
        const string code = """
            <Window xmlns="https://github.com/avaloniaui"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                    x:Class="Demo.MainWindow" Title="Demo" Width="900" Height="700">
                <!-- 主界面 -->
                <ScrollViewer>
                    <StackPanel Spacing="12">
                        <TextBlock Text="{Binding Greeting}" FontSize="16"/>
                        <Button Content="Click"
                                Command="{Binding GreetCommand}"
                                IsVisible="{Binding !IsBusy}"/>
                        <TextBlock Text="{}"/>
                    </StackPanel>
                </ScrollViewer>
            </Window>
            """;
        AssertRoundTrip(code);
        Assert.Contains(Lex(code), t => t.Kind == TokenKind.MarkupExtensionName && t.Text == "Binding");
        Assert.Contains(Lex(code), t => t.Kind == TokenKind.XmlComment);
    }
}
