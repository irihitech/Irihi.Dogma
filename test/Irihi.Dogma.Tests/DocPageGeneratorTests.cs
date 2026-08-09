using System.Collections.Immutable;
using System.Reflection;
using Irihi.Dogma.Docs.Generator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Irihi.Dogma.Tests;

public class DocPageGeneratorTests
{
    private const string AttributeSource = """
        namespace Irihi.Dogma.Docs
        {
            [System.AttributeUsage(System.AttributeTargets.Class)]
            public sealed class DocCategoryAttribute(string key) : System.Attribute
            {
                public string Key { get; } = key;
                public string? Parent { get; set; }
                public int Order { get; set; }
                public bool IsClickable { get; set; } = true;
                public string[]? Tags { get; set; }
            }
            [System.AttributeUsage(System.AttributeTargets.Class)]
            public sealed class DocPageAttribute(string titleKey) : System.Attribute
            {
                public string TitleKey { get; } = titleKey;
                public string? Title { get; set; }
                public System.Type? View { get; set; }
                public string[]? Keywords { get; set; }
            }
        }
        """;

    private static (string Generated, ImmutableArray<Diagnostic> Diagnostics) Run(string source)
    {
        var compilation = CSharpCompilation.Create(
            "GeneratorTest",
            new[] { CSharpSyntaxTree.ParseText(AttributeSource), CSharpSyntaxTree.ParseText(source) },
            TrustedReferences(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var driver = CSharpGeneratorDriver.Create(new DocPageGenerator()) as GeneratorDriver;
        driver = driver!.RunGeneratorsAndUpdateCompilation(compilation, out _, out _);
        var result = driver.GetRunResult().Results[0];
        if (result.Exception is { } generatorException)
        {
            throw generatorException;
        }

        var generated = result.GeneratedSources
            .FirstOrDefault(s => s.HintName == "GeneratedDocPages.g.cs").SourceText.ToString();
        return (generated, result.Diagnostics);
    }

    private static ImmutableArray<MetadataReference> TrustedReferences()
    {
        ImmutableArray<MetadataReference> refs = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Select(p => (MetadataReference)MetadataReference.CreateFromFile(p))
            .ToImmutableArray();
        return refs;
    }

    private const string SampleSource = """
        namespace Demo;

        public class ButtonViewModel { }
        public class ButtonView { }

        [Irihi.Dogma.Docs.DocCategory("Docs.Controls", Order = 1)]
        [Irihi.Dogma.Docs.DocPage("Demo.Button.Title",
            Title = "Button", View = typeof(ButtonView),
            Keywords = new[] { "click" })]
        public class ControlsViewModel { }
        """;

    [Fact]
    public void Generates_Register_With_Category_And_Page()
    {
        var (generated, diagnostics) = Run(SampleSource);
        Assert.Empty(diagnostics);

        Assert.Contains("GeneratedDocPages", generated);
        Assert.Contains("AddCategory", generated);
        Assert.Contains("Docs.Controls", generated);
        Assert.Contains("DocPageMetadata", generated);
        Assert.Contains("ControlsViewModel", generated);
        Assert.Contains("() => new global::Demo.ControlsViewModel()", generated);
    }

    [Fact]
    public void Generates_ViewLocator_With_Static_Switch()
    {
        var (generated, _) = Run(SampleSource);

        Assert.Contains("GeneratedViewLocator", generated);
        Assert.Contains("IDataTemplate", generated);
        Assert.Contains("global::Demo.ControlsViewModel => new global::Demo.ButtonView()", generated);
        Assert.Contains("data is global::Demo.ControlsViewModel", generated);
    }

    [Fact]
    public void Category_Without_Page_Has_No_ViewLocator_Entry()
    {
        var source = """
            namespace Demo;
            public class TextBoxViewModel { }
            public class TextBoxView { }

            [Irihi.Dogma.Docs.DocCategory("Docs.Group", Order = 1)]
            [Irihi.Dogma.Docs.DocPage("T", View = typeof(TextBoxView))]
            public class GroupViewModel { }

            [Irihi.Dogma.Docs.DocCategory("Docs.PureGroup", Order = 2, IsClickable = false)]
            public class PureGroupViewModel { }
            """;
        var (generated, _) = Run(source);

        // 纯分组节点：无 Page 属性，不进 ViewLocator
        Assert.Contains("Docs.PureGroup", generated);
        Assert.DoesNotContain("PureGroupViewModel =>", generated);
        Assert.Contains("global::Demo.GroupViewModel => new global::Demo.TextBoxView()", generated);
    }

    [Fact]
    public void Reports_Duplicate_Key_As_Dogdoc003()
    {
        var source = """
            namespace Demo;
            [Irihi.Dogma.Docs.DocCategory("Dup")]
            public class A { }
            [Irihi.Dogma.Docs.DocCategory("Dup")]
            public class B { }
            """;
        var (_, diagnostics) = Run(source);
        Assert.Contains(diagnostics, d => d.Id == "DOGDOC003");
    }

    [Fact]
    public void Reports_Cycle_As_Dogdoc002()
    {
        var source = """
            namespace Demo;
            [Irihi.Dogma.Docs.DocCategory("A", Parent = "B")]
            public class A { }
            [Irihi.Dogma.Docs.DocCategory("B", Parent = "A")]
            public class B { }
            """;
        var (_, diagnostics) = Run(source);
        Assert.Contains(diagnostics, d => d.Id == "DOGDOC002");
    }

    [Fact]
    public void Container_Category_With_Page_Without_View_Is_Allowed()
    {
        // View 可选：仅标题/容器页面（如 Docs_Controls 的 [DocPage]）不要求 View，
        // 也不进 ViewLocator（但 Register 正常生成 Page）
        var source = """
            namespace Demo;
            [Irihi.Dogma.Docs.DocCategory("C")]
            [Irihi.Dogma.Docs.DocPage("T")]
            public class C { }
            """;
        var (generated, diagnostics) = Run(source);
        Assert.Empty(diagnostics);
        Assert.Contains("DocPageMetadata", generated);
        Assert.DoesNotContain("C => new", generated);
    }

    [Fact]
    public void Reports_Orphan_DocPage_As_Dogdoc007()
    {
        var source = """
            namespace Demo;
            [Irihi.Dogma.Docs.DocPage("T", View = typeof(object))]
            public class Orphan { }
            """;
        var (_, diagnostics) = Run(source);
        Assert.Contains(diagnostics, d => d.Id == "DOGDOC007");
    }

    [Fact]
    public void No_Attributes_Generates_Empty_Register()
    {
        var (generated, diagnostics) = Run("namespace Demo { public class X { } }");
        Assert.Empty(diagnostics);
        Assert.Contains("GeneratedDocPages", generated);   // 类存在
        Assert.DoesNotContain("registry.AddCategory(new", generated);  // 无调用
        Assert.Contains("data is false", generated);  // ViewLocator 空 Match
    }
}
