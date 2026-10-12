using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Irihi.Dogma.Controls;
using Irihi.Dogma.Controls.ViewModels;
using Irihi.Dogma.Demo;
using Irihi.Dogma.Demo.Pages;
using Irihi.Dogma.Docs;
using Xunit;

namespace Irihi.Dogma.HeadlessTests;

public class CodeBlockDemoTests
{
    private sealed class TestDocSite : DocSite;

    [AvaloniaFact]
    public void Generated_Demo_Page_Renders_All_Three_Languages_In_New_Group()
    {
        var site = new TestDocSite();
        GeneratedDocPages.Register(site);
        var group = Assert.Single(site.Roots, n => n.Metadata.Key == "Docs_DocumentationControls");
        Assert.False(group.Metadata.IsClickable);
        var page = Assert.Single(group.Children);
        Assert.Equal("Docs_CodeBlock", page.Metadata.Key);
        var metadata = Assert.IsType<DocPageMetadata>(page.Metadata.Page);
        var model = Assert.IsType<CodeBlockViewModel>(metadata.ViewModelFactory());
        var provider = Assert.IsAssignableFrom<IPageMetadataProvider>(model);
        Assert.NotNull(provider.PageMetadata.Title);
        Assert.NotNull(provider.PageMetadata.Description);
        Assert.Equal(2, provider.PageMetadata.Breadcrumbs?.Count);
        Assert.All(provider.PageMetadata.Breadcrumbs!, b => Assert.NotNull(b.Header));
        Assert.Equal(3, provider.PageMetadata.Tags.Count);
        Assert.All(provider.PageMetadata.Tags, t => Assert.NotNull(t.Text));
        var view = Assert.IsType<CodeBlockView>(new GeneratedViewLocator().Build(model));
        view.DataContext = model;
        var window = new Window { Content = view };
        window.Show();

        var blocks = view.GetVisualDescendants().OfType<CodeBlock>().ToList();
        Assert.Equal(new[] { CodeLanguage.Axaml, CodeLanguage.CSharp, CodeLanguage.Bash },
            blocks.Select(b => b.Language));
        Assert.Equal(new[] { model.AxamlCode, model.CSharpCode, model.BashCode },
            blocks.Select(b => b.Code));
        foreach (var block in blocks)
        {
            var text = Assert.Single(block.GetVisualDescendants().OfType<SelectableTextBlock>());
            Assert.Equal(block.Code, text.Inlines?.Text);
        }
        window.Close();
    }
}
