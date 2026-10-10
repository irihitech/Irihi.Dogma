using Irihi.Dogma.Controls;
using Irihi.Dogma.Controls.Lexers;
using Xunit;

namespace Irihi.Dogma.Tests;

public class BashLexerTests
{
    [Theory]
    [InlineData("")]
    [InlineData("#!/bin/bash\r\n# comment\r\n")]
    [InlineData("for file in *.txt; do echo \"$file\" | cat; done\n")]
    [InlineData("name=value; echo ${name:-default} $HOME $1 $? $$")]
    [InlineData("echo 'unterminated")]
    [InlineData("echo \"unterminated\\")]
    [InlineData("echo ${unterminated")]
    [InlineData("echo hello\\ world foo#bar \\#literal \\\nnext")]
    [InlineData("echo $(pwd) `date` && exit 0")]
    public void Tokens_Preserve_Source_And_Dispatch_To_Bash(string code)
    {
        var tokens = BashLexer.Tokenize(code);
        Assert.Equal(code, string.Concat(tokens.Select(t => t.Text)));
        Assert.All(tokens, t => Assert.NotEmpty(t.Text));
        Assert.Equal(tokens, CodeLexer.Tokenize(code, CodeLanguage.Bash));
    }

    [Fact]
    public void Common_Shell_Syntax_Uses_Existing_Token_Kinds()
    {
        const string code = "if [ $count -gt 0 ]; then echo 'hello' \"world\" > output; fi # comment";
        var tokens = BashLexer.Tokenize(code);
        Assert.Contains(new CodeToken(TokenKind.Keyword, "if"), tokens);
        Assert.Contains(new CodeToken(TokenKind.Keyword, "then"), tokens);
        Assert.Contains(new CodeToken(TokenKind.Keyword, "fi"), tokens);
        Assert.Contains(new CodeToken(TokenKind.Identifier, "$count"), tokens);
        Assert.Contains(new CodeToken(TokenKind.Number, "0"), tokens);
        Assert.Contains(new CodeToken(TokenKind.String, "'hello'"), tokens);
        Assert.Contains(new CodeToken(TokenKind.String, "\"world\""), tokens);
        Assert.Contains(new CodeToken(TokenKind.Operator, ">"), tokens);
        Assert.Contains(new CodeToken(TokenKind.Comment, "# comment"), tokens);
    }

    [Theory]
    [InlineData("echo foo#bar")]
    [InlineData("echo \\#literal")]
    [InlineData("echo foo\\ #bar")]
    [InlineData("name=#value")]
    [InlineData("echo '#' \"#\"")]
    public void Hash_Inside_A_Word_Or_String_Is_Not_A_Comment(string code)
    {
        Assert.DoesNotContain(BashLexer.Tokenize(code), t => t.Kind == TokenKind.Comment);
    }

    [Fact]
    public void Single_Quotes_Do_Not_Interpret_Backslashes()
    {
        var tokens = BashLexer.Tokenize("echo 'path\\' # comment");
        Assert.Contains(new CodeToken(TokenKind.String, "'path\\'"), tokens);
        Assert.Contains(new CodeToken(TokenKind.Comment, "# comment"), tokens);
    }

    [Theory]
    [InlineData("\"$(printf \"%s\" \"it's fine\")\"")]
    [InlineData("\"$(echo \"$(pwd)\")\"")]
    public void Command_Substitution_Quotes_Do_Not_End_The_Outer_String(string value)
    {
        var code = $"echo {value}\n# comment";
        var tokens = BashLexer.Tokenize(code);
        Assert.Equal(code, string.Concat(tokens.Select(t => t.Text)));
        Assert.Contains(new CodeToken(TokenKind.String, value), tokens);
        Assert.Contains(new CodeToken(TokenKind.Comment, "# comment"), tokens);
    }
}
