using Avalonia.Media;

namespace Irihi.Dogma.Controls;

/// <summary>
/// 代码高亮调色板：UI 基础色 + 各 token 类别配色，作为一个整体打包为资源使用
/// （资源键 <see cref="CodeBlock.CodeBlockPaletteKey"/>，可按 ThemeVariant 分别提供）。
/// 未设置的 token 色回退到 <see cref="Foreground"/>。
/// </summary>
public class CodePalette
{
    /// <summary>代码块背景色</summary>
    public IBrush Background { get; set; } = Brushes.Transparent;

    /// <summary>默认文本前景色</summary>
    public IBrush Foreground { get; set; } = Brushes.Black;

    /// <summary>行号前景色</summary>
    public IBrush LineNumber { get; set; } = Brushes.Gray;

    /// <summary>文本选区背景色</summary>
    public IBrush Selection { get; set; } = Brushes.LightBlue;

    // ---- token 配色（null 时回退 Foreground）----

    public IBrush? Keyword { get; set; }
    public IBrush? Type { get; set; }
    public IBrush? Number { get; set; }
    public IBrush? String { get; set; }
    public IBrush? Char { get; set; }
    public IBrush? Comment { get; set; }
    public IBrush? DocComment { get; set; }
    public IBrush? Preprocessor { get; set; }
    public IBrush? Operator { get; set; }
    public IBrush? XmlPunctuation { get; set; }
    public IBrush? XmlElementName { get; set; }
    public IBrush? XmlAttributeName { get; set; }
    public IBrush? XmlAttributeValue { get; set; }
    public IBrush? XmlComment { get; set; }
    public IBrush? XmlCData { get; set; }
    public IBrush? XmlDeclaration { get; set; }
    public IBrush? MarkupExtensionBrace { get; set; }
    public IBrush? MarkupExtensionName { get; set; }
    public IBrush? MarkupExtensionParameter { get; set; }

    /// <summary>取某 token 类别对应的画刷；未配置时回退到 <see cref="Foreground"/>。</summary>
    public IBrush For(TokenKind kind) => kind switch
    {
        TokenKind.Keyword => Keyword,
        TokenKind.Type => Type,
        TokenKind.Number => Number,
        TokenKind.String => String,
        TokenKind.Char => Char,
        TokenKind.Comment => Comment,
        TokenKind.DocComment => DocComment,
        TokenKind.Preprocessor => Preprocessor,
        TokenKind.Operator => Operator,
        TokenKind.XmlPunctuation => XmlPunctuation,
        TokenKind.XmlElementName => XmlElementName,
        TokenKind.XmlAttributeName => XmlAttributeName,
        TokenKind.XmlAttributeValue => XmlAttributeValue,
        TokenKind.XmlComment => XmlComment,
        TokenKind.XmlCData => XmlCData,
        TokenKind.XmlDeclaration => XmlDeclaration,
        TokenKind.MarkupExtensionBrace => MarkupExtensionBrace,
        TokenKind.MarkupExtensionName => MarkupExtensionName,
        TokenKind.MarkupExtensionParameter => MarkupExtensionParameter,
        _ => null,
    } ?? Foreground;

    /// <summary>暗色主题（默认）</summary>
    public static CodePalette Dark { get; } = CreateDark();

    /// <summary>亮色主题</summary>
    public static CodePalette Light { get; } = CreateLight();

    private static CodePalette CreateDark() => new()
    {
        Background = Brush.Parse("#1E1E1E"),
        Foreground = Brush.Parse("#D4D4D4"),
        LineNumber = Brush.Parse("#858585"),
        Selection = Brush.Parse("#264F78"),
        Keyword = Brush.Parse("#569CD6"),
        Type = Brush.Parse("#4EC9B0"),
        Number = Brush.Parse("#B5CEA8"),
        String = Brush.Parse("#CE9178"),
        Char = Brush.Parse("#CE9178"),
        Comment = Brush.Parse("#6A9955"),
        DocComment = Brush.Parse("#608B4E"),
        Preprocessor = Brush.Parse("#C586C0"),
        Operator = Brush.Parse("#D4D4D4"),
        XmlPunctuation = Brush.Parse("#808080"),
        XmlElementName = Brush.Parse("#569CD6"),
        XmlAttributeName = Brush.Parse("#9CDCFE"),
        XmlAttributeValue = Brush.Parse("#CE9178"),
        XmlComment = Brush.Parse("#6A9955"),
        XmlCData = Brush.Parse("#CE9178"),
        XmlDeclaration = Brush.Parse("#C586C0"),
        MarkupExtensionBrace = Brush.Parse("#C586C0"),
        MarkupExtensionName = Brush.Parse("#C586C0"),
        MarkupExtensionParameter = Brush.Parse("#9CDCFE"),
    };

    private static CodePalette CreateLight() => new()
    {
        Background = Brush.Parse("#FFFFFF"),
        Foreground = Brush.Parse("#000000"),
        LineNumber = Brush.Parse("#237893"),
        Selection = Brush.Parse("#ADD6FF"),
        Keyword = Brush.Parse("#0000FF"),
        Type = Brush.Parse("#267F99"),
        Number = Brush.Parse("#098658"),
        String = Brush.Parse("#A31515"),
        Char = Brush.Parse("#A31515"),
        Comment = Brush.Parse("#008000"),
        DocComment = Brush.Parse("#008000"),
        Preprocessor = Brush.Parse("#AF00DB"),
        Operator = Brush.Parse("#000000"),
        XmlPunctuation = Brush.Parse("#800000"),
        XmlElementName = Brush.Parse("#800000"),
        XmlAttributeName = Brush.Parse("#FF0000"),
        XmlAttributeValue = Brush.Parse("#0000FF"),
        XmlComment = Brush.Parse("#008000"),
        XmlCData = Brush.Parse("#A31515"),
        XmlDeclaration = Brush.Parse("#AF00DB"),
        MarkupExtensionBrace = Brush.Parse("#AF00DB"),
        MarkupExtensionName = Brush.Parse("#AF00DB"),
        MarkupExtensionParameter = Brush.Parse("#FF0000"),
    };
}
