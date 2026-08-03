namespace Irihi.Dogma.Controls.Lexers;

/// <summary>
/// AXAML 词法分析器（手写状态机，零依赖）。
/// 产出 token 流满足 round-trip：所有 token 文本按顺序拼接 == 原始源码。
/// 覆盖：开始/结束标签、元素名、属性名、属性值（含引号）、自闭合、
/// 注释、CDATA、XML 声明/处理指令、DOCTYPE、元素间文本，以及属性值内
/// MarkupExtension（<c>{Binding ...}</c>）的分解（含嵌套扩展与 <c>{}</c> 转义）。
/// </summary>
public static class AxamlLexer
{
    /// <summary>对 AXAML 源码进行词法分析。</summary>
    public static IReadOnlyList<CodeToken> Tokenize(string code)
    {
        var tokens = new List<CodeToken>();
        var i = 0;
        var n = code.Length;

        while (i < n)
        {
            if (code[i] == '<')
            {
                i = ParseTag(code, i, tokens);
            }
            else
            {
                var start = i;
                while (i < n && code[i] != '<')
                {
                    i++;
                }

                tokens.Add(new CodeToken(TokenKind.XmlText, code[start..i]));
            }
        }

        return tokens;
    }

    private static int ParseTag(string code, int i, List<CodeToken> tokens)
    {
        var n = code.Length;

        // 注释
        if (StartsWith(code, i, "<!--"))
        {
            return ParseUntil(code, i, "-->", TokenKind.XmlComment, tokens);
        }

        // CDATA
        if (StartsWith(code, i, "<![CDATA["))
        {
            return ParseUntil(code, i, "]]>", TokenKind.XmlCData, tokens);
        }

        if (i + 1 >= n)
        {
            tokens.Add(new CodeToken(TokenKind.XmlPunctuation, "<"));
            return i + 1;
        }

        // XML 声明 / 处理指令
        if (code[i + 1] == '?')
        {
            return ParseUntil(code, i, "?>", TokenKind.XmlDeclaration, tokens);
        }

        // DOCTYPE（简化为一个声明类 token）
        if (code[i + 1] == '!')
        {
            return ParseUntil(code, i, ">", TokenKind.XmlDeclaration, tokens);
        }

        // 结束标签
        if (code[i + 1] == '/')
        {
            return ParseEndTag(code, i, tokens);
        }

        // 开始标签
        return ParseStartTag(code, i, tokens);
    }

    private static int ParseUntil(string code, int start, string terminator, TokenKind kind, List<CodeToken> tokens)
    {
        var index = code.IndexOf(terminator, start + 1, StringComparison.Ordinal);
        var end = index < 0 ? code.Length : index + terminator.Length;
        tokens.Add(new CodeToken(kind, code[start..end]));
        return end;
    }

    private static int ParseEndTag(string code, int i, List<CodeToken> tokens)
    {
        tokens.Add(new CodeToken(TokenKind.XmlPunctuation, "</"));
        i += 2;
        i = SkipWhitespace(code, i, tokens);
        i = ReadName(code, i, TokenKind.XmlElementName, tokens);
        i = SkipWhitespace(code, i, tokens);
        tokens.Add(new CodeToken(TokenKind.XmlPunctuation, ">"));
        return i + 1;
    }

    private static int ParseStartTag(string code, int i, List<CodeToken> tokens)
    {
        var n = code.Length;
        tokens.Add(new CodeToken(TokenKind.XmlPunctuation, "<"));
        i++;
        i = SkipWhitespace(code, i, tokens);
        i = ReadName(code, i, TokenKind.XmlElementName, tokens);

        while (i < n)
        {
            i = SkipWhitespace(code, i, tokens);
            if (i >= n)
            {
                break;
            }

            if (code[i] == '>')
            {
                tokens.Add(new CodeToken(TokenKind.XmlPunctuation, ">"));
                return i + 1;
            }

            if (code[i] == '/')
            {
                tokens.Add(new CodeToken(TokenKind.XmlPunctuation, "/"));
                i++;
                if (i < n && code[i] == '>')
                {
                    tokens.Add(new CodeToken(TokenKind.XmlPunctuation, ">"));
                    return i + 1;
                }

                continue;
            }

            i = ReadName(code, i, TokenKind.XmlAttributeName, tokens);
            i = SkipWhitespace(code, i, tokens);
            if (i < n && code[i] == '=')
            {
                tokens.Add(new CodeToken(TokenKind.XmlPunctuation, "="));
                i++;
                i = SkipWhitespace(code, i, tokens);
                if (i < n && (code[i] == '"' || code[i] == '\''))
                {
                    i = ParseAttributeValue(code, i, tokens);
                }
            }
        }

        return i;
    }

    /// <summary>解析属性值（含引号），并分解其中的 MarkupExtension。</summary>
    private static int ParseAttributeValue(string code, int i, List<CodeToken> tokens)
    {
        var quote = code[i];
        var n = code.Length;

        // 开引号
        tokens.Add(new CodeToken(TokenKind.XmlAttributeValue, quote.ToString()));
        i++;

        var buffer = new System.Text.StringBuilder();
        while (i < n && code[i] != quote)
        {
            // MarkupExtension：{ 开头且不是 {} 转义
            if (code[i] == '{' && i + 1 < n && code[i + 1] != '}')
            {
                FlushBuffer(buffer, tokens);
                i = ParseMarkupExtension(code, i, tokens);
                continue;
            }

            buffer.Append(code[i]);
            i++;
        }

        FlushBuffer(buffer, tokens);

        // 闭引号
        if (i < n)
        {
            tokens.Add(new CodeToken(TokenKind.XmlAttributeValue, quote.ToString()));
            i++;
        }

        return i;
    }

    /// <summary>解析 <c>{Name 参数, 参数}</c>，含嵌套扩展与 <c>{}</c> 转义。</summary>
    private static int ParseMarkupExtension(string code, int i, List<CodeToken> tokens)
    {
        var n = code.Length;
        tokens.Add(new CodeToken(TokenKind.MarkupExtensionBrace, "{"));
        i++;

        // 扩展类型名
        i = SkipWhitespace(code, i, tokens);
        i = ReadName(code, i, TokenKind.MarkupExtensionName, tokens);

        while (i < n)
        {
            if (code[i] == '{')
            {
                i = ParseMarkupExtension(code, i, tokens);
                continue;
            }

            if (code[i] == '}')
            {
                tokens.Add(new CodeToken(TokenKind.MarkupExtensionBrace, "}"));
                return i + 1;
            }

            // 参数段：直到下一个 { 或 }（含逗号、空白、=、值）
            var start = i;
            while (i < n && code[i] != '{' && code[i] != '}')
            {
                i++;
            }

            tokens.Add(new CodeToken(TokenKind.MarkupExtensionParameter, code[start..i]));
        }

        return i;
    }

    private static void FlushBuffer(System.Text.StringBuilder buffer, List<CodeToken> tokens)
    {
        if (buffer.Length > 0)
        {
            tokens.Add(new CodeToken(TokenKind.XmlAttributeValue, buffer.ToString()));
            buffer.Clear();
        }
    }

    private static int SkipWhitespace(string code, int i, List<CodeToken> tokens)
    {
        var n = code.Length;
        var start = i;
        while (i < n && char.IsWhiteSpace(code[i]))
        {
            i++;
        }

        if (i > start)
        {
            tokens.Add(new CodeToken(TokenKind.XmlText, code[start..i]));
        }

        return i;
    }

    /// <summary>读取 XML 名称（字母/数字/_/-/./:）。返回新索引；无名称时不产出 token。</summary>
    private static int ReadName(string code, int i, TokenKind kind, List<CodeToken> tokens)
    {
        var n = code.Length;
        var start = i;
        while (i < n && IsNameChar(code[i]))
        {
            i++;
        }

        if (i > start)
        {
            tokens.Add(new CodeToken(kind, code[start..i]));
        }

        return i;
    }

    private static bool IsNameChar(char c) =>
        char.IsLetterOrDigit(c) || c is '_' or '-' or '.' or ':';

    private static bool StartsWith(string code, int i, string value) =>
        i + value.Length <= code.Length && code.AsSpan(i, value.Length).SequenceEqual(value);
}
