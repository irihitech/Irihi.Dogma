using Irihi.Dogma.Controls.Lexers;

namespace Irihi.Dogma.Controls;

/// <summary>
/// 词法分析入口：按语言分派到对应的 lexer，产出 token 流。
/// </summary>
public static class CodeLexer
{
    /// <summary>
    /// 对源码进行词法分析。
    /// </summary>
    /// <param name="code">源码</param>
    /// <param name="language">语言</param>
    /// <returns>token 流（拼接后必须等于原始源码）</returns>
    public static IReadOnlyList<CodeToken> Tokenize(string code, CodeLanguage language) => language switch
    {
        CodeLanguage.Axaml => AxamlLexer.Tokenize(code),
        CodeLanguage.CSharp => CSharpTokenClassifier.Classify(CSharpLexer.Tokenize(code)),
        _ => [new CodeToken(TokenKind.Text, code)],
    };
}
