using System.Collections.ObjectModel;
using Irihi.Dogma.Controls;
using Irihi.Dogma.Controls.ViewModels;
using Irihi.Dogma.Docs;

namespace Irihi.Dogma.Demo.Pages;

[DocCategory("Docs_Buttons", Parent = "Docs_Controls", Order = 1)]
[DocPage("Docs_Button_Title", View = typeof(ButtonView), Keywords = new[] { "click", "button" })]
public sealed partial class ButtonViewModel : IPageMetadataProvider
{
    private const string BasicUsageAnchorId = "button-basic-usage";
    private const string DisabledStateAnchorId = "button-disabled-state";

    public PageMetadataViewModel PageMetadata { get; set; } = new()
    {
        Title = LanguageManager.Instance.Docs_Button_Title,
        Description = LanguageManager.Instance.Docs_Button_Desc,
        Breadcrumbs =
        [
            new BreadcrumbItemData(LanguageManager.Instance.Docs_Controls),
            new BreadcrumbItemData(LanguageManager.Instance.Docs_Button_Title)
        ],
        Tags = ["Button", "Input"],
        MvvmSupport = true,
        InlineXamlSupport = true,
    };

    public DemoSectionViewModel BasicSection { get; }
    public DemoSectionViewModel DisabledSection { get; }

    public ObservableCollection<AnchorScrollViewerItemViewModel> AnchorItems { get; set; } =
    [
        new()
        {
            Header = LanguageManager.Instance.Page_Button_Section_Basic_Header,
            AnchorId = BasicUsageAnchorId
        },
        new()
        {
            Header = LanguageManager.Instance.Page_Button_Section_Disabled_Header,
            AnchorId = DisabledStateAnchorId
        },
    ];

    public ButtonViewModel()
    {
        BasicSection = new DemoSectionViewModel
        {
            Header = LanguageManager.Instance.Page_Button_Section_Basic_Header,
            Descriptions = { LanguageManager.Instance.Page_Button_Section_Basic_Description },
            SectionTag = DemoSectionTag.Function,
            AnchorId = BasicUsageAnchorId
        };
        BasicSection.CodeSnippets.Add(new DemoSectionCodeSnippetViewModel
        {
            CodeSnippetLanguage = CodeLanguage.Axaml,
            TabName = LanguageManager.Instance.DemoSection_Tab_Xaml,
            CodeSnippet = """
                          <StackPanel Spacing="8">
                              <Button Content="Click me"/>
                              <Button Classes="Primary" Content="Primary"/>
                              <Button Classes="Secondary" Content="Secondary"/>
                          </StackPanel>
                          """
        });

        DisabledSection = new DemoSectionViewModel
        {
            Header = LanguageManager.Instance.Page_Button_Section_Disabled_Header,
            Descriptions = { LanguageManager.Instance.Page_Button_Section_Disabled_Description },
            SectionTag = DemoSectionTag.Style,
            AnchorId = DisabledStateAnchorId
        };
        DisabledSection.CodeSnippets.Add(new DemoSectionCodeSnippetViewModel
        {
            CodeSnippetLanguage = CodeLanguage.Axaml,
            TabName = LanguageManager.Instance.DemoSection_Tab_Xaml,
            CodeSnippet = """
                          <Button Content="Disabled" IsEnabled="False"/>
                          """
        });
    }
}
