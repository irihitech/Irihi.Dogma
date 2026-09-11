using System.Collections.ObjectModel;
using Irihi.Dogma.Controls;
using Irihi.Dogma.Controls.ViewModels;
using Irihi.Dogma.Docs;
using Irihi.Lingua;

namespace Irihi.Dogma.Demo.Pages;

[DocCategory("Docs_Input", Parent = "Docs_Controls", Order = 2)]
[DocPage("Docs_Input_Title", View = typeof(InputView), Keywords = new[] { "textbox", "input" })]
public sealed partial class InputViewModel : IPageMetadataProvider
{
    private const string BasicUsageAnchorId = "input-basic-usage";
    private const string PasswordAnchorId = "input-password";

    public PageMetadataViewModel PageMetadata { get; set; } = new()
    {
        Title = LanguageManager.Instance.Docs_Input_Title,
        Description = LanguageManager.Instance.Docs_Input_Desc,
        Breadcrumbs =
        [
            new BreadcrumbItemData(LanguageManager.Instance.Docs_Controls),
            new BreadcrumbItemData(LanguageManager.Instance.Docs_Input_Title)
        ],
        Tags =
        [
            PageMetadataTags.MvvmSupport(),
            PageMetadataTags.InlineXaml(),
            new PageMetadataTagViewModel { Text = LinguaObservableString.FromLiteral("Input"), Classes = "Ghost" },
        ],
    };

    public DemoSectionViewModel BasicSection { get; }
    public DemoSectionViewModel PasswordSection { get; }

    public ObservableCollection<AnchorScrollViewerItemViewModel> AnchorItems { get; set; } =
    [
        new()
        {
            Header = LanguageManager.Instance.Page_Input_Section_Basic_Header,
            AnchorId = BasicUsageAnchorId
        },
        new()
        {
            Header = LanguageManager.Instance.Page_Input_Section_Password_Header,
            AnchorId = PasswordAnchorId
        },
    ];

    public InputViewModel()
    {
        BasicSection = new DemoSectionViewModel
        {
            Header = LanguageManager.Instance.Page_Input_Section_Basic_Header,
            Descriptions = { LanguageManager.Instance.Page_Input_Section_Basic_Description },
            SectionTag = DemoSectionTag.Function,
            AnchorId = BasicUsageAnchorId
        };
        BasicSection.CodeSnippets.Add(new DemoSectionCodeSnippetViewModel
        {
            CodeSnippetLanguage = CodeLanguage.Axaml,
            TabName = LanguageManager.Instance.DemoSection_Tab_Xaml,
            CodeSnippet = """
                          <StackPanel Spacing="8" MaxWidth="300">
                              <TextBox PlaceholderText="Enter your name"/>
                              <TextBox Text="Editable text"/>
                          </StackPanel>
                          """
        });

        PasswordSection = new DemoSectionViewModel
        {
            Header = LanguageManager.Instance.Page_Input_Section_Password_Header,
            Descriptions = { LanguageManager.Instance.Page_Input_Section_Password_Description },
            SectionTag = DemoSectionTag.Others,
            AnchorId = PasswordAnchorId
        };
        PasswordSection.CodeSnippets.Add(new DemoSectionCodeSnippetViewModel
        {
            CodeSnippetLanguage = CodeLanguage.Axaml,
            TabName = LanguageManager.Instance.DemoSection_Tab_Xaml,
            CodeSnippet = """
                          <TextBox PlaceholderText="Password" PasswordChar="●"/>
                          """
        });
    }
}
