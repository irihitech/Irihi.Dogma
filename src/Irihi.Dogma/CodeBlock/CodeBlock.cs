using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Styling;

namespace Irihi.Dogma.Controls;

/// <summary>
/// 代码块展示控件：语法高亮 + 文本选择复制（基于 <see cref="SelectableTextBlock"/>），
/// 可选行号与"复制全部"按钮，支持暗/亮主题。
/// </summary>
public class CodeBlock : TemplatedControl
{
    /// <summary>要展示的源码文本。</summary>
    public static readonly StyledProperty<string> CodeProperty =
        AvaloniaProperty.Register<CodeBlock, string>(nameof(Code), string.Empty);

    /// <summary>源码语言（决定词法分析方式）。</summary>
    public static readonly StyledProperty<CodeLanguage> LanguageProperty =
        AvaloniaProperty.Register<CodeBlock, CodeLanguage>(nameof(Language), CodeLanguage.Axaml);

    /// <summary>是否显示行号。</summary>
    public static readonly StyledProperty<bool> ShowLineNumbersProperty =
        AvaloniaProperty.Register<CodeBlock, bool>(nameof(ShowLineNumbers), true);

    /// <summary>是否显示"复制全部"按钮。</summary>
    public static readonly StyledProperty<bool> ShowCopyButtonProperty =
        AvaloniaProperty.Register<CodeBlock, bool>(nameof(ShowCopyButton), true);

    /// <summary>"复制全部"按钮的显示文本，默认为 "Copy"。</summary>
    public static readonly StyledProperty<string> CopyButtonTextProperty =
        AvaloniaProperty.Register<CodeBlock, string>(nameof(CopyButtonText), "Copy");

    /// <summary>
    /// 调色板：为 null 时按 <see cref="StyledElement.ActualThemeVariant"/> 自动选择内建
    /// <see cref="CodePalette.Dark"/> 或 <see cref="CodePalette.Light"/>；
    /// 显式赋值后使用自定义 palette（不再随主题切换）。
    /// </summary>
    public static readonly StyledProperty<CodePalette?> PaletteProperty =
        AvaloniaProperty.Register<CodeBlock, CodePalette?>(nameof(Palette));

    private Border? _container;
    private SelectableTextBlock? _codeText;
    private TextBlock? _lineNumbers;
    private Button? _copyButton;

    public string Code
    {
        get => GetValue(CodeProperty);
        set => SetValue(CodeProperty, value);
    }

    public CodeLanguage Language
    {
        get => GetValue(LanguageProperty);
        set => SetValue(LanguageProperty, value);
    }

    public CodePalette? Palette
    {
        get => GetValue(PaletteProperty);
        set => SetValue(PaletteProperty, value);
    }

    public bool ShowLineNumbers
    {
        get => GetValue(ShowLineNumbersProperty);
        set => SetValue(ShowLineNumbersProperty, value);
    }

    public bool ShowCopyButton
    {
        get => GetValue(ShowCopyButtonProperty);
        set => SetValue(ShowCopyButtonProperty, value);
    }

    public string CopyButtonText
    {
        get => GetValue(CopyButtonTextProperty);
        set => SetValue(CopyButtonTextProperty, value);
    }

    public CodeBlock()
    {
        // 跟随 Avalonia 原生主题变体（窗口/应用 RequestedThemeVariant 或系统主题）
        ActualThemeVariantChanged += (_, _) =>
        {
            ApplyPalette();
            RenderCode();
        };
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        _container = e.NameScope.Find<Border>("PART_Container");
        _codeText = e.NameScope.Find<SelectableTextBlock>("PART_CodeText");
        _lineNumbers = e.NameScope.Find<TextBlock>("PART_LineNumbers");
        _copyButton = e.NameScope.Find<Button>("PART_CopyButton");

        if (_copyButton is not null)
        {
            _copyButton.Click += OnCopyClick;
        }

        ApplyPalette();
        RenderCode();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == CodeProperty || change.Property == LanguageProperty || change.Property == PaletteProperty)
        {
            ApplyPalette();
            RenderCode();
        }
        else if (change.Property == ShowLineNumbersProperty)
        {
            if (_lineNumbers is not null)
            {
                _lineNumbers.IsVisible = ShowLineNumbers;
            }
        }
        else if (change.Property == ShowCopyButtonProperty)
        {
            if (_copyButton is not null)
            {
                _copyButton.IsVisible = ShowCopyButton;
            }
        }
    }

    /// <summary>解析当前调色板：显式 <see cref="Palette"/> 优先，否则按主题变体取内建默认。</summary>
    private CodePalette ResolvePalette() =>
        Palette ?? (ActualThemeVariant == ThemeVariant.Light ? CodePalette.Light : CodePalette.Dark);

    private void ApplyPalette()
    {
        var palette = ResolvePalette();

        if (_container is not null)
        {
            _container.Background = palette.Background;
        }

        if (_codeText is not null)
        {
            _codeText.Foreground = palette.Foreground;
            _codeText.SelectionBrush = palette.Selection;
        }

        if (_lineNumbers is not null)
        {
            _lineNumbers.Foreground = palette.LineNumber;
        }
    }

    private void RenderCode()
    {
        if (_codeText is null)
        {
            return;
        }

        var code = Code ?? string.Empty;
        var tokens = CodeLexer.Tokenize(code, Language);
        _codeText.Inlines = CodeHighlightRenderer.Render(tokens, ResolvePalette());
        UpdateLineNumbers(code);
    }

    private void UpdateLineNumbers(string code)
    {
        if (_lineNumbers is null)
        {
            return;
        }

        var normalized = code.Replace("\r\n", "\n").Replace('\r', '\n');
        var lineCount = normalized.Split('\n').Length;
        _lineNumbers.Text = string.Join("\n", Enumerable.Range(1, lineCount));
        _lineNumbers.IsVisible = ShowLineNumbers;
    }

    private async void OnCopyClick(object? sender, RoutedEventArgs e)
    {
        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard is null)
        {
            return;
        }

        await clipboard.SetTextAsync(Code ?? string.Empty);
    }
}
