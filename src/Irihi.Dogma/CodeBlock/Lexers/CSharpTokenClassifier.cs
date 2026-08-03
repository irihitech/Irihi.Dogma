namespace Irihi.Dogma.Controls.Lexers;

/// <summary>
/// C# token 后处理分类器：在词法分析之后，通过轻量上下文规则把部分
/// <see cref="TokenKind.Identifier"/> 升级为 <see cref="TokenKind.Type"/>。
/// 纯词法无法精确区分类型与变量，此为启发式（覆盖 demo 常见场景）：
/// <list type="bullet">
/// <item>类型声明关键字（class/record/interface/struct/enum/delegate）后的标识符</item>
/// <item><c>new</c> 关键字后的标识符</item>
/// <item><c>typeof(...)</c> 括号内的第一个标识符</item>
/// <item><c>nameof(...)</c> 括号内的大写开头标识符（小写变量名不误判）</item>
/// </list>
/// 只修改 token 类别，不改变文本，round-trip 保持不变。
/// </summary>
public static class CSharpTokenClassifier
{
    private static readonly HashSet<string> TypeDeclarationKeywords = new(StringComparer.Ordinal)
    {
        "class", "record", "interface", "struct", "enum", "delegate",
    };

    /// <summary>对 token 流做类型名分类。</summary>
    public static IReadOnlyList<CodeToken> Classify(IReadOnlyList<CodeToken> tokens)
    {
        var result = new List<CodeToken>(tokens);

        for (var i = 0; i < result.Count; i++)
        {
            var token = result[i];
            if (token.Kind != TokenKind.Keyword)
            {
                continue;
            }

            if (TypeDeclarationKeywords.Contains(token.Text))
            {
                i = MarkNextType(result, i, skipVoidAfterDelegate: token.Text == "delegate");
            }
            else if (token.Text == "new")
            {
                i = MarkNextType(result, i);
            }
            else if (token.Text is "typeof" or "nameof")
            {
                i = MarkAfterTypeofOrNameof(result, i, token.Text);
            }
        }

        return result;
    }

    /// <summary>标记声明关键字后的类型名（跳过空白；delegate 时处理返回类型 + 委托名）。</summary>
    private static int MarkNextType(List<CodeToken> tokens, int i, bool skipVoidAfterDelegate = false)
    {
        var j = NextNonText(tokens, i + 1);
        if (j < 0)
        {
            return i;
        }

        if (!skipVoidAfterDelegate)
        {
            if (tokens[j].Kind == TokenKind.Identifier)
            {
                tokens[j] = new CodeToken(TokenKind.Type, tokens[j].Text);
                return j;
            }

            return i;
        }

        // delegate 语法：delegate [return-type] Name(...)
        // 返回类型是类型名（delegate Foo Bar()）；委托名也是类型名（delegate void Handler()）。
        if (tokens[j].Kind == TokenKind.Identifier)
        {
            tokens[j] = new CodeToken(TokenKind.Type, tokens[j].Text);
        }

        var m = NextNonText(tokens, j + 1);
        if (m >= 0 && tokens[m].Kind == TokenKind.Identifier)
        {
            tokens[m] = new CodeToken(TokenKind.Type, tokens[m].Text);
            return m;
        }

        return j;
    }

    private static int MarkAfterTypeofOrNameof(List<CodeToken> tokens, int i, string keyword)
    {
        var open = NextNonText(tokens, i + 1);
        if (open < 0 || tokens[open].Kind != TokenKind.Operator || tokens[open].Text != "(")
        {
            return i;
        }

        var name = NextNonText(tokens, open + 1);
        if (name < 0 || tokens[name].Kind != TokenKind.Identifier)
        {
            return i;
        }

        // typeof 无条件；nameof 仅当参数是大写开头（类型风格）才判为类型
        if (keyword == "typeof" || char.IsUpper(tokens[name].Text[0]))
        {
            tokens[name] = new CodeToken(TokenKind.Type, tokens[name].Text);
        }

        return name;
    }

    private static int NextNonText(List<CodeToken> tokens, int from)
    {
        for (var i = from; i < tokens.Count; i++)
        {
            if (tokens[i].Kind != TokenKind.Text)
            {
                return i;
            }
        }

        return -1;
    }
}
