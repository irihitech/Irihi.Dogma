namespace Irihi.Dogma.Controls.Lexers;

/// <summary>
/// AXAML 词法分析器（状态机实现，Phase 2 填充完整逻辑）。
/// </summary>
public static class AxamlLexer
{
    /// <summary>对 AXAML 源码进行词法分析。</summary>
    public static IReadOnlyList<CodeToken> Tokenize(string code) =>
        [new CodeToken(TokenKind.Text, code)];
}
