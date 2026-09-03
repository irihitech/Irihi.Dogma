using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Irihi.Dogma.Controls;
using Xunit;

namespace Irihi.Dogma.HeadlessTests;

public class TabControlReproTests
{
    /// <summary>用户报告无法切换的代码示例：含两个泛型类型参数。</summary>
    private const string TwoGenericParamsCode = """
        public async Task ShowStandardDrawerAsync()
        {
            await OverlayDrawer.ShowStandardAsync<DefaultDemoDialog,DefaultDemoDialogViewModel>(
                new DefaultDemoDialogViewModel(),
                null,
                CreateOptions());
        }
        """;

    private static TabControl CreateTabControl(string code)
    {
        var tab = new TabControl();
        tab.Items.Add(new TabItem { Header = "Simple", Content = new CodeBlock { Code = "simple code", Language = CodeLanguage.CSharp } });
        tab.Items.Add(new TabItem { Header = "Generic", Content = new CodeBlock { Code = code, Language = CodeLanguage.CSharp } });
        return tab;
    }

    [AvaloniaFact]
    public void TabControl_Can_Switch_To_TwoGenericParams_Tab()
    {
        var tab = CreateTabControl(TwoGenericParamsCode);
        var window = new Window { Width = 800, Height = 600, Content = tab };
        window.Show();

        tab.SelectedIndex = 0;
        Assert.Equal(0, tab.SelectedIndex);

        // 切到含两泛型参数代码的 Tab
        tab.SelectedIndex = 1;
        Assert.Equal(1, tab.SelectedIndex);

        // 再切回来
        tab.SelectedIndex = 0;
        Assert.Equal(0, tab.SelectedIndex);
    }

    [AvaloniaFact]
    public void TabControl_Can_Switch_Back_And_Forth_Repeatedly()
    {
        var tab = CreateTabControl(TwoGenericParamsCode);
        var window = new Window { Width = 800, Height = 600, Content = tab };
        window.Show();

        for (var i = 0; i < 5; i++)
        {
            tab.SelectedIndex = 1;
            Assert.Equal(1, tab.SelectedIndex);
            tab.SelectedIndex = 0;
            Assert.Equal(0, tab.SelectedIndex);
        }
    }

    /// <summary>
    /// Code 先于 Language 赋值（XAML 属性按书写顺序应用）：
    /// 中间态会用默认 Axaml 渲染 C# 代码，修复前此处死循环。
    /// </summary>
    [AvaloniaFact]
    public void Code_Set_Before_Language_Transient_Axaml_State_Terminates()
    {
        var block = new CodeBlock(); // Language 保持默认 Axaml
        var window = new Window { Width = 800, Height = 600, Content = block };
        window.Show();

        block.Code = TwoGenericParamsCode;
        Assert.Equal(CodeLanguage.Axaml, block.Language);

        block.Language = CodeLanguage.CSharp;

        var text = block.GetVisualDescendants().OfType<SelectableTextBlock>()
            .First(t => t.Name == "PART_CodeText");
        Assert.Equal(TwoGenericParamsCode, text.Inlines?.Text);
    }
}
