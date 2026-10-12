namespace Irihi.Dogma.Controls.Lexers;

/// <summary>
/// Bash 词法分析器，保留所有源码文本并使用通用 token 配色。
/// </summary>
public static class BashLexer
{
    private static readonly HashSet<string> Keywords = new(StringComparer.Ordinal)
    {
        "if", "then", "else", "elif", "fi", "case", "esac", "for", "select",
        "while", "until", "do", "done", "in", "function", "time", "coproc", "!", "{", "}"
    };

    /// <summary>对 Bash 源码进行词法分析。</summary>
    public static IReadOnlyList<CodeToken> Tokenize(string code)
    {
        var tokens = new List<CodeToken>();
        var i = 0;
        var wordStart = true;
        while (i < code.Length)
        {
            var start = i;
            var c = code[i++];
            var kind = TokenKind.Text;
            if (char.IsWhiteSpace(c))
            {
                while (i < code.Length && char.IsWhiteSpace(code[i]))
                    i++;
                wordStart = true;
            }
            else if (c == '#' && wordStart)
            {
                while (i < code.Length && code[i] is not ('\r' or '\n'))
                    i++;
                kind = TokenKind.Comment;
            }
            else if (c is '\'' or '"' or '`')
            {
                i = ReadQuoted(code, i, c);
                kind = TokenKind.String;
                wordStart = false;
            }
            else if (c == '\\')
            {
                if (i < code.Length)
                {
                    // 续行不改变当前 shell 单词的边界。
                    if (code[i++] != '\n')
                        wordStart = false;
                }
            }
            else if (c == '$')
            {
                if (i < code.Length && code[i] == '{')
                {
                    i++;
                    while (i < code.Length && code[i] != '}')
                        i++;
                    if (i < code.Length)
                        i++;
                }
                else if (i < code.Length && (char.IsLetter(code[i]) || code[i] == '_'))
                {
                    while (i < code.Length && (char.IsLetterOrDigit(code[i]) || code[i] == '_'))
                        i++;
                }
                else if (i < code.Length && (char.IsDigit(code[i]) || "@*#?$!-".Contains(code[i])))
                {
                    i++;
                }
                kind = i > start + 1 ? TokenKind.Identifier : TokenKind.Operator;
                wordStart = false;
            }
            else if (IsOperator(c))
            {
                kind = TokenKind.Operator;
                wordStart = c != '=';
            }
            else
            {
                while (i < code.Length && !char.IsWhiteSpace(code[i]) && !IsOperator(code[i])
                       && code[i] is not ('\'' or '"' or '`' or '$' or '\\'))
                    i++;
                var word = code[start..i];
                kind = wordStart && Keywords.Contains(word) ? TokenKind.Keyword
                    : word.All(char.IsDigit) ? TokenKind.Number : TokenKind.Identifier;
                wordStart = false;
            }
            tokens.Add(new CodeToken(kind, code[start..i]));
        }
        return tokens;
    }

    private static bool IsOperator(char c) => c is ';' or '|' or '&' or '(' or ')' or '<' or '>' or '=';

    private static int ReadQuoted(string code, int i, char quote)
    {
        var contexts = new Stack<char>();
        contexts.Push(quote);
        while (i < code.Length && contexts.Count > 0)
        {
            var context = contexts.Peek();
            var c = code[i++];
            if (c == '\\' && context != '\'' && i < code.Length
                && (context is '(' or '`' || code[i] is '$' or '`' or '"' or '\\' or '\n'))
            {
                i++;
            }
            else if (context == '\'' || c == context && context != '(')
            {
                if (c == context)
                    contexts.Pop();
            }
            else if (c == '$' && i < code.Length && code[i] == '(')
            {
                i++;
                contexts.Push('(');
            }
            else if (context == '(')
            {
                if (c == ')')
                    contexts.Pop();
                else if (c is '(' or '\'' or '"' or '`')
                    contexts.Push(c);
            }
        }
        return i;
    }
}
