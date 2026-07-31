using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Media;

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

    /// <summary>配色方案（注意：与 Avalonia 内置 Theme/ThemeVariant 无关联）。</summary>
    public static readonly StyledProperty<CodeTheme> ColorSchemeProperty =
        AvaloniaProperty.Register<CodeBlock, CodeTheme>(nameof(ColorScheme), CodeTheme.Dark);

    /// <summary>是否显示行号。</summary>
    public static readonly StyledProperty<bool> ShowLineNumbersProperty =
        AvaloniaProperty.Register<CodeBlock, bool>(nameof(ShowLineNumbers), true);

    /// <summary>是否显示"复制全部"按钮。</summary>
    public static readonly StyledProperty<bool> ShowCopyButtonProperty =
        AvaloniaProperty.Register<CodeBlock, bool>(nameof(ShowCopyButton), true);

    private Border? _container;
    private SelectableTextBlock? _codeText;
    private TextBlock? _lineNumbers;
    private Button? _copyButton;
    private bool _copyBusy;

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

    public CodeTheme ColorScheme
    {
        get => GetValue(ColorSchemeProperty);
        set => SetValue(ColorSchemeProperty, value);
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

        if (change.Property == ColorSchemeProperty)
        {
            ApplyPalette();
            RenderCode();
        }
        else if (change.Property == CodeProperty || change.Property == LanguageProperty)
        {
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

    private void ApplyPalette()
    {
        var palette = CodePalette.For(ColorScheme);

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
        _codeText.Inlines = CodeHighlightRenderer.Render(tokens, CodePalette.For(ColorScheme));
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
        if (_copyBusy || _copyButton is null)
        {
            return;
        }

        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard is null)
        {
            return;
        }

        _copyBusy = true;
        try
        {
            await clipboard.SetTextAsync(Code ?? string.Empty);
            _copyButton.Content = "Copied ✓";
            await Task.Delay(1500);
            _copyButton.Content = "Copy";
        }
        finally
        {
            _copyBusy = false;
        }
    }
}
