using Avalonia.Controls.Documents;
using Avalonia.Media;

namespace Irihi.Dogma.Controls;

/// <summary>
/// 将词法 token 流渲染为 <see cref="InlineCollection"/>（Run 序列）。
/// 每个 token 映射为一个 <see cref="Run"/>，文本原样保留，因此
/// 拼接结果必然等于原始源码（round-trip）。
/// </summary>
public static class CodeHighlightRenderer
{
    /// <summary>
    /// 渲染 token 流为可放入 <see cref="TextBlock.Inlines"/> 的 Run 序列。
    /// </summary>
    /// <param name="tokens">词法 token 流（来自 <see cref="CodeLexer"/>）</param>
    /// <param name="palette">配色调色板</param>
    public static InlineCollection Render(IReadOnlyList<CodeToken> tokens, CodePalette palette)
    {
        var inlines = new InlineCollection();
        foreach (var token in tokens)
        {
            inlines.Add(CreateRun(token, palette));
        }

        return inlines;
    }

    /// <summary>为单个 token 创建 Run（附 token-* 样式类，供 XAML 覆盖配色）。</summary>
    public static Run CreateRun(CodeToken token, CodePalette palette)
    {
        var run = new Run { Text = token.Text };
        run.Foreground = palette.For(token.Kind);
        if (token.Kind == TokenKind.Keyword)
        {
            run.FontWeight = FontWeight.Bold;
        }

        // 例如 token-keyword / token-string / token-markupextensionbrace，
        // 用户可在 XAML 中通过样式选择器 .token-keyword { Foreground: ... } 覆盖。
        run.Classes.Add("token-" + token.Kind.ToString().ToLowerInvariant());

        return run;
    }
}
