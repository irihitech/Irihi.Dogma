using System.Text;

namespace Irihi.Dogma.Controls.Lexers;

/// <summary>
/// C# 词法分析器（手写状态机，零依赖）。
/// 产出 token 流满足 round-trip：所有 token 文本按顺序拼接 == 原始源码。
/// 覆盖：关键字（含 contextual）、标识符（含 verbatim）、数字字面量
/// （十六进制/二进制/浮点/后缀/分隔符）、字符串族（普通/verbatim/插值，
/// 插值表达式递归词法分析）、字符字面量、注释（含 ///）、预处理指令、运算符标点。
/// </summary>
public static class CSharpLexer
{
    private static readonly HashSet<string> Keywords = new(StringComparer.Ordinal)
    {
        // 保留关键字
        "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char",
        "checked", "class", "const", "continue", "decimal", "default", "delegate",
        "do", "double", "else", "enum", "event", "explicit", "extern", "false",
        "finally", "fixed", "float", "for", "foreach", "goto", "if", "implicit",
        "in", "int", "interface", "internal", "is", "lock", "long", "namespace",
        "new", "null", "object", "operator", "out", "override", "params", "private",
        "protected", "public", "readonly", "ref", "return", "sbyte", "sealed",
        "short", "sizeof", "stackalloc", "static", "string", "struct", "switch",
        "this", "throw", "true", "try", "typeof", "uint", "ulong", "unchecked",
        "unsafe", "ushort", "using", "virtual", "void", "volatile", "while",
        // contextual 关键字
        "add", "alias", "and", "ascending", "args", "async", "await", "by",
        "descending", "dynamic", "equals", "file", "from", "get", "global",
        "group", "init", "into", "join", "let", "managed", "nameof", "nint",
        "not", "notnull", "nuint", "on", "or", "orderby", "partial", "record",
        "remove", "required", "scoped", "select", "set", "unmanaged", "value",
        "var", "when", "where", "with", "yield",
    };

    private static readonly HashSet<char> NumberSuffixChars = new("fFdDmMuUlL");

    /// <summary>对 C# 源码进行词法分析。</summary>
    public static IReadOnlyList<CodeToken> Tokenize(string code)
    {
        var tokens = new List<CodeToken>();
        var i = 0;
        var n = code.Length;

        while (i < n)
        {
            var c = code[i];

            if (char.IsWhiteSpace(c))
            {
                var start = i;
                while (i < n && char.IsWhiteSpace(code[i]))
                {
                    i++;
                }

                tokens.Add(new CodeToken(TokenKind.Text, code[start..i]));
            }
            else if (c == '#')
            {
                i = ReadPreprocessor(code, i, tokens);
            }
            else if (c == '/' && i + 1 < n && code[i + 1] is '/' or '*')
            {
                i = ReadComment(code, i, tokens);
            }
            else if (c == '"' || (c == '@' && i + 1 < n && code[i + 1] == '"')
                     || (c == '$' && i + 1 < n && (code[i + 1] == '"' || code[i + 1] == '@')))
            {
                i = ReadString(code, i, tokens);
            }
            else if (c == '\'')
            {
                i = ReadChar(code, i, tokens);
            }
            else if (char.IsDigit(c) || (c == '.' && i + 1 < n && char.IsDigit(code[i + 1])))
            {
                i = ReadNumber(code, i, tokens);
            }
            else if (char.IsLetter(c) || c == '_' || (c == '@' && i + 1 < n && char.IsLetter(code[i + 1])))
            {
                i = ReadWord(code, i, tokens);
            }
            else
            {
                tokens.Add(new CodeToken(TokenKind.Operator, c.ToString()));
                i++;
            }
        }

        return tokens;
    }

    private static int ReadPreprocessor(string code, int i, List<CodeToken> tokens)
    {
        // C# 预处理指令：'#' 必须是行首（'#' 与最近 '\n' 之间只能有空白）
        var j = i;
        while (j > 0 && code[j - 1] != '\n' && char.IsWhiteSpace(code[j - 1]))
        {
            j--;
        }

        var atLineStart = j == 0 || code[j - 1] == '\n';
        if (!atLineStart)
        {
            // 不是行首的 #，按运算符处理
            tokens.Add(new CodeToken(TokenKind.Operator, "#"));
            return i + 1;
        }

        var start = i;
        while (i < code.Length && code[i] is not ('\n' or '\r'))
        {
            i++;
        }

        tokens.Add(new CodeToken(TokenKind.Preprocessor, code[start..i]));
        return i;
    }

    private static int ReadComment(string code, int i, List<CodeToken> tokens)
    {
        var n = code.Length;
        var start = i;

        if (code[i + 1] == '/')
        {
            // 行注释（/// 为文档注释）；\r 属于行终止符而非注释内容（CRLF 源码）
            var isDoc = i + 2 < n && code[i + 2] == '/';
            i += 2;
            if (isDoc)
            {
                i++; // 第三个 /
            }

            while (i < n && code[i] is not ('\n' or '\r'))
            {
                i++;
            }

            tokens.Add(new CodeToken(isDoc ? TokenKind.DocComment : TokenKind.Comment, code[start..i]));
            return i;
        }

        // 块注释
        i += 2;
        while (i < n && !(code[i] == '*' && i + 1 < n && code[i + 1] == '/'))
        {
            i++;
        }

        if (i < n)
        {
            i += 2;
        }

        tokens.Add(new CodeToken(TokenKind.Comment, code[start..i]));
        return i;
    }

    private static int ReadString(string code, int i, List<CodeToken> tokens)
    {
        var n = code.Length;
        var start = i;

        // 前缀：$ 和 @（任意顺序，最多两个字符）
        var isInterpolated = false;
        var isVerbatim = false;
        while (i < n && code[i] is '$' or '@')
        {
            if (code[i] == '$')
            {
                isInterpolated = true;
            }
            else
            {
                isVerbatim = true;
            }

            i++;
        }

        // 开引号
        var prefix = code[start..i];
        tokens.Add(new CodeToken(TokenKind.String, prefix + "\""));
        i++; // 跳过开引号

        var buffer = new StringBuilder();
        while (i < n)
        {
            var c = code[i];

            // verbatim 转义引号 ""
            if (isVerbatim && c == '"' && i + 1 < n && code[i + 1] == '"')
            {
                buffer.Append("\"\"");
                i += 2;
                continue;
            }

            // 插值花括号转义 {{ }}
            if (isInterpolated && c == '{' && i + 1 < n && code[i + 1] == '{')
            {
                buffer.Append("{{");
                i += 2;
                continue;
            }

            if (isInterpolated && c == '}' && i + 1 < n && code[i + 1] == '}')
            {
                buffer.Append("}}");
                i += 2;
                continue;
            }

            // 插值表达式开始
            if (isInterpolated && c == '{')
            {
                Flush(buffer, tokens);
                tokens.Add(new CodeToken(TokenKind.Operator, "{"));
                i++;
                var end = ReadInterpolationExpression(code, i, tokens);
                tokens.Add(new CodeToken(TokenKind.Operator, "}"));
                i = end + 1; // 跳过匹配的 '}'
                continue;
            }

            if (c == '"')
            {
                Flush(buffer, tokens);
                tokens.Add(new CodeToken(TokenKind.String, "\""));
                return i + 1;
            }

            // 非 verbatim 的转义序列
            if (c == '\\' && !isVerbatim && i + 1 < n)
            {
                buffer.Append(c);
                buffer.Append(code[i + 1]);
                i += 2;
                continue;
            }

            buffer.Append(c);
            i++;
        }

        Flush(buffer, tokens);
        return i;
    }

    /// <summary>
    /// 读取插值表达式（i 指向 '{' 之后），递归词法分析直到匹配的 '}'。
    /// 返回指向匹配 '}' 的索引。
    /// </summary>
    private static int ReadInterpolationExpression(string code, int i, List<CodeToken> tokens)
    {
        var n = code.Length;
        var depth = 1;
        var end = i;

        while (end < n)
        {
            var c = code[end];
            if (c == '{')
            {
                depth++;
                end++;
            }
            else if (c == '}')
            {
                depth--;
                if (depth == 0)
                {
                    break;
                }

                end++;
            }
            else if (c == '"')
            {
                end = SkipString(code, end);
            }
            else if (c == '\'')
            {
                end = SkipChar(code, end);
            }
            else
            {
                end++;
            }
        }

        // 递归词法分析表达式内容（不含花括号本身）
        if (end > i)
        {
            var exprTokens = Tokenize(code[i..end]);
            tokens.AddRange(exprTokens);
        }

        return end;
    }

    /// <summary>跳过字符串字面量（含 verbatim/插值/转义），返回字符串结束后的索引。</summary>
    private static int SkipString(string code, int i)
    {
        var n = code.Length;
        var isVerbatim = false;
        while (i < n && code[i] is '$' or '@')
        {
            if (code[i] == '@')
            {
                isVerbatim = true;
            }

            i++;
        }

        if (i < n && code[i] == '"')
        {
            i++;
        }

        while (i < n)
        {
            var c = code[i];
            if (c == '"')
            {
                if (isVerbatim && i + 1 < n && code[i + 1] == '"')
                {
                    i += 2;
                    continue;
                }

                return i + 1;
            }

            if (c == '\\' && !isVerbatim && i + 1 < n)
            {
                i += 2;
                continue;
            }

            i++;
        }

        return i;
    }

    /// <summary>跳过字符字面量，返回结束后的索引。</summary>
    private static int SkipChar(string code, int i)
    {
        var n = code.Length;
        if (i < n && code[i] == '\'')
        {
            i++;
        }

        while (i < n)
        {
            if (code[i] == '\\' && i + 1 < n)
            {
                i += 2;
                continue;
            }

            if (code[i] == '\'')
            {
                return i + 1;
            }

            i++;
        }

        return i;
    }

    private static int ReadChar(string code, int i, List<CodeToken> tokens)
    {
        var n = code.Length;
        var start = i;
        i++; // 开引号

        while (i < n)
        {
            if (code[i] == '\\' && i + 1 < n)
            {
                i += 2;
                continue;
            }

            if (code[i] == '\'')
            {
                i++;
                break;
            }

            i++;
        }

        tokens.Add(new CodeToken(TokenKind.Char, code[start..i]));
        return i;
    }

    private static int ReadNumber(string code, int i, List<CodeToken> tokens)
    {
        var n = code.Length;
        var start = i;

        // 十六进制 / 二进制
        if (code[i] == '0' && i + 1 < n && code[i + 1] is 'x' or 'X')
        {
            i += 2;
            while (i < n && (IsHexDigit(code[i]) || code[i] == '_'))
            {
                i++;
            }

            goto suffix;
        }

        if (code[i] == '0' && i + 1 < n && code[i + 1] is 'b' or 'B')
        {
            i += 2;
            while (i < n && (code[i] is '0' or '1' || code[i] == '_'))
            {
                i++;
            }

            goto suffix;
        }

        // 小数部分
        while (i < n && (char.IsDigit(code[i]) || code[i] == '_'))
        {
            i++;
        }

        // 小数点
        if (i + 1 < n && code[i] == '.' && char.IsDigit(code[i + 1]))
        {
            i++;
            while (i < n && (char.IsDigit(code[i]) || code[i] == '_'))
            {
                i++;
            }
        }

        // 指数
        if (i < n && code[i] is 'e' or 'E')
        {
            var j = i + 1;
            if (j < n && code[j] is '+' or '-')
            {
                j++;
            }

            if (j < n && char.IsDigit(code[j]))
            {
                i = j;
                while (i < n && (char.IsDigit(code[i]) || code[i] == '_'))
                {
                    i++;
                }
            }
        }

        suffix:
        while (i < n && NumberSuffixChars.Contains(code[i]))
        {
            i++;
        }

        tokens.Add(new CodeToken(TokenKind.Number, code[start..i]));
        return i;
    }

    private static int ReadWord(string code, int i, List<CodeToken> tokens)
    {
        var n = code.Length;
        var start = i;

        // verbatim 标识符 @name：@ 归入标识符
        if (code[i] == '@')
        {
            i++;
        }

        while (i < n && (char.IsLetterOrDigit(code[i]) || code[i] == '_'))
        {
            i++;
        }

        var text = code[start..i];
        var kind = Keywords.Contains(text) ? TokenKind.Keyword : TokenKind.Identifier;
        tokens.Add(new CodeToken(kind, text));
        return i;
    }

    private static void Flush(StringBuilder buffer, List<CodeToken> tokens)
    {
        if (buffer.Length > 0)
        {
            tokens.Add(new CodeToken(TokenKind.String, buffer.ToString()));
            buffer.Clear();
        }
    }

    private static bool IsHexDigit(char c) =>
        char.IsDigit(c) || c is >= 'a' and <= 'f' or >= 'A' and <= 'F';
}
