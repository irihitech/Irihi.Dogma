using Irihi.Dogma.Controls;
using Irihi.Dogma.Controls.Lexers;
using Xunit;

namespace Irihi.Dogma.Tests;

public class CSharpLexerTests
{
    private static IReadOnlyList<CodeToken> Lex(string code) => CSharpLexer.Tokenize(code);

    private static void AssertRoundTrip(string code)
    {
        var tokens = Lex(code);
        Assert.Equal(code, string.Concat(tokens.Select(t => t.Text)));
    }

    private static void AssertNoRoundTripFailures(params string[] samples)
    {
        foreach (var s in samples)
        {
            AssertRoundTrip(s);
        }
    }

    [Fact]
    public void Keywords_And_Identifiers()
    {
        const string code = "public static void Main()";
        AssertRoundTrip(code);

        var tokens = Lex(code);
        Assert.Equal(TokenKind.Keyword, tokens[0].Kind);
        Assert.Equal("public", tokens[0].Text);
        Assert.Equal(TokenKind.Keyword, tokens[2].Kind);
        Assert.Equal("static", tokens[2].Text);
        Assert.Equal(TokenKind.Keyword, tokens[4].Kind);
        Assert.Equal("void", tokens[4].Text);
        Assert.Equal(TokenKind.Identifier, tokens[6].Kind);
        Assert.Equal("Main", tokens[6].Text);
        Assert.Equal(TokenKind.Operator, tokens[7].Kind);
        Assert.Equal("(", tokens[7].Text);
    }

    [Fact]
    public void Contextual_Keywords_Are_Keywords()
    {
        var tokens = Lex("var async await record init required");
        var texts = tokens.Where(t => t.Kind == TokenKind.Keyword).Select(t => t.Text).ToList();
        Assert.Equal(new[] { "var", "async", "await", "record", "init", "required" }, texts);
    }

    [Fact]
    public void Verbatim_Identifier_Is_Identifier()
    {
        AssertRoundTrip("@class @if @name");
        var tokens = Lex("@class");
        Assert.Equal(TokenKind.Identifier, tokens[0].Kind);
        Assert.Equal("@class", tokens[0].Text);
    }

    [Fact]
    public void Numbers_All_Forms()
    {
        const string code = "42 0xFF 0b1010 1_000 3.14 1.5e-3 10f 100UL 0x1F_00u";
        AssertRoundTrip(code);

        var tokens = Lex(code);
        var numbers = tokens.Where(t => t.Kind == TokenKind.Number).Select(t => t.Text).ToList();
        Assert.Equal(new[] { "42", "0xFF", "0b1010", "1_000", "3.14", "1.5e-3", "10f", "100UL", "0x1F_00u" }, numbers);
    }

    [Fact]
    public void Plain_String_With_Escapes()
    {
        const string code = """var s = "a\"b\\n";""";
        AssertRoundTrip(code);

        var tokens = Lex(code);
        Assert.Contains(tokens, t => t.Kind == TokenKind.String && t.Text == "a\\\"b\\\\n");
    }

    [Fact]
    public void Verbatim_String_With_DoubleQuote()
    {
        const string code = """var s = @"C:\tmp\file""name.txt";""";
        AssertRoundTrip(code);

        var tokens = Lex(code);
        var strings = tokens.Where(t => t.Kind == TokenKind.String).Select(t => t.Text).ToList();
        // 开引号+内容+闭引号（verbatim 的 "" 是内容里的转义）
        Assert.Contains("C:\\tmp\\file\"\"name.txt", strings);
    }

    [Fact]
    public void Interpolated_String_Expression_Is_Tokenized()
    {
        const string code = """var s = $"Hello {name}!";""";
        AssertRoundTrip(code);

        var tokens = Lex(code);
        Assert.Contains(tokens, t => t.Kind == TokenKind.String && t.Text == "$\"");
        Assert.Contains(tokens, t => t.Kind == TokenKind.String && t.Text == "Hello ");
        Assert.Contains(tokens, t => t.Kind == TokenKind.Operator && t.Text == "{");
        Assert.Contains(tokens, t => t.Kind == TokenKind.Identifier && t.Text == "name");
        Assert.Contains(tokens, t => t.Kind == TokenKind.Operator && t.Text == "}");
        Assert.Contains(tokens, t => t.Kind == TokenKind.String && t.Text == "!");
    }

    [Fact]
    public void Interpolated_String_With_Format_And_Method_Call()
    {
        const string code = """var s = $"{x:F2} {GetName("a}b")}";""";
        AssertRoundTrip(code);

        var tokens = Lex(code);
        // 表达式 x:F2
        Assert.Contains(tokens, t => t.Kind == TokenKind.Identifier && t.Text == "x");
        Assert.Contains(tokens, t => t.Kind == TokenKind.Operator && t.Text == ":");
        Assert.Contains(tokens, t => t.Kind == TokenKind.Identifier && t.Text == "F2");
        // 表达式 GetName("a}b")：字符串内的 } 不应提前结束插值
        Assert.Contains(tokens, t => t.Kind == TokenKind.String && t.Text == "a}b");
        // 两个表达式都闭合
        var closeBraces = tokens.Where(t => t.Kind == TokenKind.Operator && t.Text == "}").Count();
        Assert.Equal(2, closeBraces);
    }

    [Fact]
    public void Interpolated_Verbatim_String()
    {
        const string code = """var s = $@"{name}";""";
        AssertRoundTrip(code);
        Assert.Contains(Lex(code), t => t.Kind == TokenKind.String && t.Text == "$@\"");
    }

    [Fact]
    public void Interpolated_Escaped_Braces()
    {
        const string code = """var s = $"{{literal}}";""";
        AssertRoundTrip(code);

        // {{ }} 是转义，不应产生 Operator { }
        Assert.DoesNotContain(Lex(code), t => t.Kind == TokenKind.Operator && t.Text == "{");
        Assert.Contains(Lex(code), t => t.Kind == TokenKind.String && t.Text.Contains("{{literal}}"));
    }

    [Fact]
    public void Char_Literals()
    {
        const string code = "char a = 'x'; char b = '\\n'; char c = '\\'';";
        AssertRoundTrip(code);

        var chars = Lex(code).Where(t => t.Kind == TokenKind.Char).Select(t => t.Text).ToList();
        Assert.Equal(3, chars.Count);
        Assert.Contains("'x'", chars);
        Assert.Contains("'\\n'", chars);
        Assert.Contains("'\\''", chars);
    }

    [Fact]
    public void Comments_Single_Block_And_Doc()
    {
        const string code = """
            // line comment
            int x = 1; /* block
            comment */ /// doc comment
            """;
        AssertRoundTrip(code);

        var tokens = Lex(code);
        Assert.Contains(tokens, t => t.Kind == TokenKind.Comment && t.Text == "// line comment");
        Assert.Contains(tokens, t => t.Kind == TokenKind.Comment && t.Text.StartsWith("/* block"));
        Assert.Contains(tokens, t => t.Kind == TokenKind.DocComment && t.Text == "/// doc comment");
    }

    [Fact]
    public void Preprocessor_Directives()
    {
        const string code = "#if DEBUG\nint x = 1;\n#endif\n#region Foo\n#endregion";
        AssertRoundTrip(code);

        var tokens = Lex(code);
        Assert.Contains(tokens, t => t.Kind == TokenKind.Preprocessor && t.Text == "#if DEBUG");
        Assert.Contains(tokens, t => t.Kind == TokenKind.Preprocessor && t.Text == "#endif");
        Assert.Contains(tokens, t => t.Kind == TokenKind.Preprocessor && t.Text == "#region Foo");
        Assert.Contains(tokens, t => t.Kind == TokenKind.Preprocessor && t.Text == "#endregion");
        // 行中 #（如字符串 #）不误判：确认行尾 # 前有换行的判定
        AssertRoundTrip("var s = \"a#b\";");
    }

    [Fact]
    public void Operators_And_Punctuation()
    {
        const string code = "=> == != <= >= && || ?? ?. += ->";
        AssertRoundTrip(code);

        var tokens = Lex(code);
        Assert.All(tokens.Where(t => t.Kind != TokenKind.Text), t => Assert.Equal(TokenKind.Operator, t.Kind));
    }

    [Fact]
    public void Typical_Method_Sample_RoundTrip()
    {
        const string code = """
            using System;

            namespace Irihi.Dogma.Demo;

            /// <summary>示例服务。</summary>
            public sealed class Greeter
            {
                private const string Prefix = "Hello";

                // 返回问候语
                public string Greet(string name, int times)
                {
                    var message = $"{Prefix}, {name}! x{times}";
                    Console.WriteLine(message); // 输出
                    return message;
                }
            }
            """;
        AssertRoundTrip(code);

        var tokens = Lex(code);
        Assert.Contains(tokens, t => t.Kind == TokenKind.DocComment);
        Assert.Contains(tokens, t => t.Kind == TokenKind.Keyword && t.Text == "class");
        Assert.Contains(tokens, t => t.Kind == TokenKind.String && t.Text.Contains("Hello"));
        Assert.Contains(tokens, t => t.Kind == TokenKind.Identifier && t.Text == "message");
        Assert.Contains(tokens, t => t.Kind == TokenKind.Comment && t.Text == "// 输出");
    }

    [Fact]
    public void Empty_Input_Produces_No_Tokens()
    {
        Assert.Empty(Lex(string.Empty));
    }
}
