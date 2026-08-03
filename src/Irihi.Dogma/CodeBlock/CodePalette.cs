using Avalonia.Media;

namespace Irihi.Dogma.Controls;

/// <summary>
/// 代码高亮调色板：UI 基础色 + 各 token 类别配色。
/// 通过 <see cref="For"/> 按类别取色，未配置的类别回退到 <see cref="Foreground"/>。
/// </summary>
public sealed class CodePalette
{
    private readonly IReadOnlyDictionary<TokenKind, IBrush> _tokenBrushes;

    private CodePalette(
        IBrush background,
        IBrush foreground,
        IBrush lineNumber,
        IBrush selection,
        IReadOnlyDictionary<TokenKind, IBrush> tokenBrushes)
    {
        Background = background;
        Foreground = foreground;
        LineNumber = lineNumber;
        Selection = selection;
        _tokenBrushes = tokenBrushes;
    }

    /// <summary>代码块背景色</summary>
    public IBrush Background { get; }

    /// <summary>默认文本前景色</summary>
    public IBrush Foreground { get; }

    /// <summary>行号前景色</summary>
    public IBrush LineNumber { get; }

    /// <summary>文本选区背景色</summary>
    public IBrush Selection { get; }

    /// <summary>
    /// 取某 token 类别对应的画刷；未配置时回退到 <see cref="Foreground"/>。
    /// </summary>
    public IBrush For(TokenKind kind) =>
        _tokenBrushes.TryGetValue(kind, out var brush) ? brush : Foreground;

    /// <summary>暗色主题（默认）</summary>
    public static CodePalette Dark { get; } = CreateDark();

    /// <summary>亮色主题</summary>
    public static CodePalette Light { get; } = CreateLight();

    /// <summary>按 <see cref="CodeTheme"/> 取调色板。</summary>
    public static CodePalette For(CodeTheme theme) => theme == CodeTheme.Light ? Light : Dark;

    private static CodePalette CreateDark() => new(
        background: Brush.Parse("#1E1E1E"),
        foreground: Brush.Parse("#D4D4D4"),
        lineNumber: Brush.Parse("#858585"),
        selection: Brush.Parse("#264F78"),
        tokenBrushes: new Dictionary<TokenKind, IBrush>
        {
            [TokenKind.Keyword] = Brush.Parse("#569CD6"),
            [TokenKind.Type] = Brush.Parse("#4EC9B0"),
            [TokenKind.Number] = Brush.Parse("#B5CEA8"),
            [TokenKind.String] = Brush.Parse("#CE9178"),
            [TokenKind.Char] = Brush.Parse("#CE9178"),
            [TokenKind.Comment] = Brush.Parse("#6A9955"),
            [TokenKind.DocComment] = Brush.Parse("#608B4E"),
            [TokenKind.Preprocessor] = Brush.Parse("#C586C0"),
            [TokenKind.Operator] = Brush.Parse("#D4D4D4"),
            [TokenKind.XmlPunctuation] = Brush.Parse("#808080"),
            [TokenKind.XmlElementName] = Brush.Parse("#569CD6"),
            [TokenKind.XmlAttributeName] = Brush.Parse("#9CDCFE"),
            [TokenKind.XmlAttributeValue] = Brush.Parse("#CE9178"),
            [TokenKind.XmlComment] = Brush.Parse("#6A9955"),
            [TokenKind.XmlCData] = Brush.Parse("#CE9178"),
            [TokenKind.XmlDeclaration] = Brush.Parse("#C586C0"),
            [TokenKind.MarkupExtensionBrace] = Brush.Parse("#C586C0"),
            [TokenKind.MarkupExtensionName] = Brush.Parse("#C586C0"),
            [TokenKind.MarkupExtensionParameter] = Brush.Parse("#9CDCFE"),
        });

    private static CodePalette CreateLight() => new(
        background: Brush.Parse("#FFFFFF"),
        foreground: Brush.Parse("#000000"),
        lineNumber: Brush.Parse("#237893"),
        selection: Brush.Parse("#ADD6FF"),
        tokenBrushes: new Dictionary<TokenKind, IBrush>
        {
            [TokenKind.Keyword] = Brush.Parse("#0000FF"),
            [TokenKind.Type] = Brush.Parse("#267F99"),
            [TokenKind.Number] = Brush.Parse("#098658"),
            [TokenKind.String] = Brush.Parse("#A31515"),
            [TokenKind.Char] = Brush.Parse("#A31515"),
            [TokenKind.Comment] = Brush.Parse("#008000"),
            [TokenKind.DocComment] = Brush.Parse("#008000"),
            [TokenKind.Preprocessor] = Brush.Parse("#AF00DB"),
            [TokenKind.Operator] = Brush.Parse("#000000"),
            [TokenKind.XmlPunctuation] = Brush.Parse("#800000"),
            [TokenKind.XmlElementName] = Brush.Parse("#800000"),
            [TokenKind.XmlAttributeName] = Brush.Parse("#FF0000"),
            [TokenKind.XmlAttributeValue] = Brush.Parse("#0000FF"),
            [TokenKind.XmlComment] = Brush.Parse("#008000"),
            [TokenKind.XmlCData] = Brush.Parse("#A31515"),
            [TokenKind.XmlDeclaration] = Brush.Parse("#AF00DB"),
            [TokenKind.MarkupExtensionBrace] = Brush.Parse("#AF00DB"),
            [TokenKind.MarkupExtensionName] = Brush.Parse("#AF00DB"),
            [TokenKind.MarkupExtensionParameter] = Brush.Parse("#FF0000"),
        });
}
