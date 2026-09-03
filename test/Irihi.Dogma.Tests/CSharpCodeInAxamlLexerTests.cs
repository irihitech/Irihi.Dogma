using Irihi.Dogma.Controls;
using Irihi.Dogma.Controls.Lexers;
using Xunit;

namespace Irihi.Dogma.Tests;

public class CSharpCodeInAxamlLexerTests
{
    /// <summary>用户报告导致 TabControl 卡死的代码：含两个泛型类型参数的泛型调用。</summary>
    private const string TwoGenericParamsCode = """
        public async Task ShowStandardDrawerAsync()
        {
            await OverlayDrawer.ShowStandardAsync<DefaultDemoDialog,DefaultDemoDialogViewModel>(
                new DefaultDemoDialogViewModel(),
                null,
                CreateOptions());
        }
        """;

    /// <summary>词法分析必须在有限时间内终止（不允许死循环冻结 UI 线程）。</summary>
    private static IReadOnlyList<CodeToken> TokenizeWithTimeout(string code, CodeLanguage language)
    {
        IReadOnlyList<CodeToken>? result = null;
        var thread = new Thread(() => result = CodeLexer.Tokenize(code, language))
        {
            IsBackground = true, // 超时后线程泄漏也不阻塞进程退出
        };
        thread.Start();
        var finished = thread.Join(TimeSpan.FromSeconds(10));
        if (!finished)
        {
            Assert.Fail($"词法分析超过 10s 未结束（疑似死循环）：language={language}");
        }

        return result!;
    }

    [Fact]
    public void AxamlLexer_Terminates_On_CSharp_Code_With_Two_Generic_Params()
    {
        var tokens = TokenizeWithTimeout(TwoGenericParamsCode, CodeLanguage.Axaml);
        Assert.Equal(TwoGenericParamsCode, string.Concat(tokens.Select(t => t.Text)));
    }

    [Fact]
    public void AxamlLexer_Terminates_On_Common_NonXml_Lt_Patterns()
    {
        string[] samples =
        {
            "Dictionary<A,B> dict;",
            "var x = a < b && c > d;",
            "List<int> list = new();",
            "Func<int,(string,int)> f;",
            "<A=B>",
            "</ A",
            "a </ b! > c",
            "Show<T>()",
        };
        foreach (var sample in samples)
        {
            var tokens = TokenizeWithTimeout(sample, CodeLanguage.Axaml);
            Assert.Equal(sample, string.Concat(tokens.Select(t => t.Text)));
        }
    }

    [Fact]
    public void CSharpLexer_RoundTrip_On_Two_Generic_Params_Code()
    {
        var tokens = TokenizeWithTimeout(TwoGenericParamsCode, CodeLanguage.CSharp);
        Assert.Equal(TwoGenericParamsCode, string.Concat(tokens.Select(t => t.Text)));
    }
}
