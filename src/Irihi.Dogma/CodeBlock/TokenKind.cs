namespace Irihi.Dogma.Controls;

/// <summary>
/// 词法 token 的类别，用于决定高亮配色。
/// </summary>
public enum TokenKind
{
    /// <summary>普通文本 / 空白 / 未分类内容</summary>
    Text,

    /// <summary>标识符</summary>
    Identifier,

    /// <summary>语言关键字（加粗）</summary>
    Keyword,

    /// <summary>数字字面量</summary>
    Number,

    /// <summary>字符串字面量</summary>
    String,

    /// <summary>字符字面量（C#）</summary>
    Char,

    /// <summary>注释</summary>
    Comment,

    /// <summary>XML 文档注释（C# ///）</summary>
    DocComment,

    /// <summary>预处理指令（C# #if/#region 等）</summary>
    Preprocessor,

    /// <summary>运算符 / 标点</summary>
    Operator,

    /// <summary>XML 标点（&lt; &gt; / = ! ? 等）</summary>
    XmlPunctuation,

    /// <summary>XML / AXAML 元素名</summary>
    XmlElementName,

    /// <summary>XML / AXAML 属性名</summary>
    XmlAttributeName,

    /// <summary>XML / AXAML 属性值（含引号）</summary>
    XmlAttributeValue,

    /// <summary>XML 注释（&lt;!-- --&gt;）</summary>
    XmlComment,

    /// <summary>CDATA 节</summary>
    XmlCData,

    /// <summary>XML 声明 / 处理指令（&lt;?xml ... ?&gt; 等）</summary>
    XmlDeclaration,

    /// <summary>XML 元素间文本内容</summary>
    XmlText,

    /// <summary>MarkupExtension 花括号（{ }）</summary>
    MarkupExtensionBrace,

    /// <summary>MarkupExtension 类型名（如 Binding、StaticResource）</summary>
    MarkupExtensionName,

    /// <summary>MarkupExtension 参数 / 键值</summary>
    MarkupExtensionParameter,
}
