namespace Irihi.Dogma.Controls.Lexers;

/// <summary>
/// C# 词法分析器（状态机实现，Phase 3 填充完整逻辑）。
/// </summary>
public static class CSharpLexer
{
    /// <summary>对 C# 源码进行词法分析。</summary>
    public static IReadOnlyList<CodeToken> Tokenize(string code) =>
        [new CodeToken(TokenKind.Text, code)];
}
