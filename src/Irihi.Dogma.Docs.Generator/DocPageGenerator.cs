using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Irihi.Dogma.Docs.Generator;

/// <summary>
/// 扫描 <c>[DocCategory]</c> / <c>[DocPage]</c>（共存模型），生成：
/// ① <c>GeneratedDocPages.Register</c>（AOT 安全的静态注册代码）
/// ② <c>GeneratedViewLocator</c>（VM→View 静态映射的 IDataTemplate）
/// 并做编译期诊断：分类环、Key 重复、页面缺 View、单独 DocPage。
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class DocPageGenerator : IIncrementalGenerator
{
    private const string CategoryAttribute = "Irihi.Dogma.Docs.DocCategoryAttribute";
    private const string PageAttribute = "Irihi.Dogma.Docs.DocPageAttribute";

    private static readonly DiagnosticDescriptor CycleError = new(
        "DOGDOC002", "Category cycle detected",
        "Category tree contains a cycle involving '{0}'",
        "Irihi.Dogma.Docs", DiagnosticSeverity.Error, isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor DuplicateKeyError = new(
        "DOGDOC003", "Duplicate category key",
        "Category key '{0}' is declared more than once",
        "Irihi.Dogma.Docs", DiagnosticSeverity.Error, isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor MissingViewError = new(
        "DOGDOC006", "DocPage requires a View",
        "DocPage '{0}' must specify View = typeof(...)",
        "Irihi.Dogma.Docs", DiagnosticSeverity.Error, isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor OrphanPageWarning = new(
        "DOGDOC007", "DocPage without DocCategory",
        "DocPage '{0}' is not co-attributed with [DocCategory] and is ignored",
        "Irihi.Dogma.Docs", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var categories = context.SyntaxProvider.ForAttributeWithMetadataName(
            CategoryAttribute,
            static (node, _) => node is ClassDeclarationSyntax,
            static (ctx, _) => CollectCategory(ctx))
            .Where(static c => c is not null);

        var pages = context.SyntaxProvider.ForAttributeWithMetadataName(
            PageAttribute,
            static (node, _) => node is ClassDeclarationSyntax,
            static (ctx, _) => CollectPage(ctx));

        var combined = categories.Collect().Combine(pages.Collect());

        context.RegisterSourceOutput(combined, static (spc, source) => Emit(source.Left, source.Right, spc));
    }

    private sealed record CategoryInfo(
        string Key,
        string? ParentKey,
        int Order,
        bool IsClickable,
        ImmutableArray<string> Tags,
        string VmTypeName,
        PageInfo? Page,
        Location Location);

    private sealed record PageInfo(
        string TitleKey,
        string? FallbackTitle,
        string? ViewTypeName,
        ImmutableArray<string> Keywords,
        string VmTypeName,
        Location Location);

    private static CategoryInfo? CollectCategory(GeneratorAttributeSyntaxContext ctx)
    {
        var symbol = (INamedTypeSymbol)ctx.TargetSymbol;
        var attr = ctx.Attributes[0];
        var key = attr.ConstructorArguments.FirstOrDefault().Value as string;
        if (key is null)
        {
            return null;
        }

        string? parent = null;
        var order = 0;
        var clickable = true;
        var tags = ImmutableArray<string>.Empty;
        foreach (var named in attr.NamedArguments)
        {
            switch (named.Key)
            {
                case "Parent":
                    parent = named.Value.Value as string;
                    break;
                case "Order":
                    if (named.Value.Value is int o)
                    {
                        order = o;
                    }

                    break;
                case "IsClickable":
                    if (named.Value.Value is bool b)
                    {
                        clickable = b;
                    }

                    break;
                case "Tags":
                    tags = ReadStrings(named.Value);
                    break;
            }
        }

        // 共存页面：同一类型上是否也有 [DocPage]
        PageInfo? page = null;
        var pageAttr = symbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == PageAttribute);
        if (pageAttr is not null)
        {
            page = CollectPageInfo(pageAttr, symbol);
        }

        return new CategoryInfo(
            key, parent, order, clickable, tags,
            symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            page, ctx.TargetNode.GetLocation());
    }

    private static PageInfo? CollectPage(GeneratorAttributeSyntaxContext ctx)
    {
        var symbol = (INamedTypeSymbol)ctx.TargetSymbol;
        return CollectPageInfo(ctx.Attributes[0], symbol);
    }

    private static PageInfo CollectPageInfo(AttributeData attr, INamedTypeSymbol symbol)
    {
        var titleKey = attr.ConstructorArguments.FirstOrDefault().Value as string ?? string.Empty;
        string? fallback = null;
        string? viewType = null;
        var keywords = ImmutableArray<string>.Empty;
        foreach (var named in attr.NamedArguments)
        {
            switch (named.Key)
            {
                case "Title":
                    fallback = named.Value.Value as string;
                    break;
                case "View":
                    viewType = named.Value.Value is INamedTypeSymbol view
                        ? view.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                        : null;
                    break;
                case "Keywords":
                    keywords = ReadStrings(named.Value);
                    break;
            }
        }

        return new PageInfo(
            titleKey, fallback, viewType, keywords,
            symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            symbol.Locations.FirstOrDefault() ?? Location.None);
    }

    private static ImmutableArray<string> ReadStrings(TypedConstant value)
    {
        if (value.Kind == TypedConstantKind.Array && value.Values is { Length: > 0 } values)
        {
            return values.Select(v => v.Value as string ?? string.Empty).ToImmutableArray();
        }

        return ImmutableArray<string>.Empty;
    }

    private static void Emit(
        ImmutableArray<CategoryInfo?> categories,
        ImmutableArray<PageInfo?> pages,
        SourceProductionContext spc)
    {
        var cats = categories.Where(c => c is not null).Select(c => c!).ToImmutableArray();

        // ---- 诊断 ----
        foreach (var group in cats.GroupBy(c => c.Key, StringComparer.Ordinal).Where(g => g.Count() > 1))
        {
            foreach (var dup in group.Skip(1))
            {
                spc.ReportDiagnostic(Diagnostic.Create(DuplicateKeyError, dup.Location, dup.Key));
            }
        }

        DetectCycles(cats, spc);

        foreach (var page in cats.Where(c => c.Page is not null).Select(c => c.Page!))
        {
            if (page.ViewTypeName is null)
            {
                spc.ReportDiagnostic(Diagnostic.Create(MissingViewError, page.Location, page.TitleKey));
            }
        }

        foreach (var page in pages.Where(p => p is not null).Select(p => p!))
        {
            // 已由共存收集的页面不计
            if (cats.All(c => c.Page?.VmTypeName != page.VmTypeName))
            {
                spc.ReportDiagnostic(Diagnostic.Create(OrphanPageWarning, page.Location, page.TitleKey));
            }
        }

        // ---- 生成 ----
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("#nullable enable");
        sb.AppendLine("namespace Irihi.Dogma.Docs;");
        sb.AppendLine();
        AppendRegister(sb, cats);
        sb.AppendLine();
        AppendViewLocator(sb, cats);
        spc.AddSource("GeneratedDocPages.g.cs", sb.ToString());
    }

    private static void AppendRegister(StringBuilder sb, ImmutableArray<CategoryInfo> cats)
    {
        sb.AppendLine("public static partial class GeneratedDocPages");
        sb.AppendLine("{");
        sb.AppendLine("    public static void Register(global::Irihi.Dogma.Docs.IDocRegistry registry)");
        sb.AppendLine("    {");
        foreach (var cat in cats)
        {
            sb.AppendLine("        registry.AddCategory(new global::Irihi.Dogma.Docs.DocCategoryMetadata");
            sb.AppendLine("        {");
            sb.Append("            Key = ").Append(Literal(cat.Key)).AppendLine(",");
            sb.Append("            ParentKey = ").Append(Literal(cat.ParentKey)).AppendLine(",");
            sb.Append("            Order = ").Append(cat.Order).AppendLine(",");
            sb.Append("            IsClickable = ").Append(cat.IsClickable ? "true" : "false").AppendLine(",");
            if (cat.Tags.Length > 0)
            {
                sb.Append("            Tags = ").Append(LiteralArray(cat.Tags)).AppendLine(",");
            }

            if (cat.Page is { } page)
            {
                sb.AppendLine("            Page = new global::Irihi.Dogma.Docs.DocPageMetadata");
                sb.AppendLine("            {");
                sb.Append("                TitleKey = ").Append(Literal(page.TitleKey)).AppendLine(",");
                sb.Append("                FallbackTitle = ").Append(Literal(page.FallbackTitle)).AppendLine(",");
                sb.Append("                ViewModelType = typeof(").Append(page.VmTypeName).AppendLine("),");
                sb.Append("                ViewType = typeof(").Append(page.ViewTypeName).AppendLine("),");
                if (page.Keywords.Length > 0)
                {
                    sb.Append("                Keywords = ").Append(LiteralArray(page.Keywords)).AppendLine(",");
                }

                sb.Append("                ViewModelFactory = () => new ").Append(page.VmTypeName).AppendLine("(),");
                sb.AppendLine("            },");
            }

            sb.AppendLine("        });");
        }

        sb.AppendLine("    }");
        sb.AppendLine("}");
    }

    private static void AppendViewLocator(StringBuilder sb, ImmutableArray<CategoryInfo> cats)
    {
        var pages = cats.Where(c => c.Page is not null).Select(c => (c.Page!, c.VmTypeName)).ToImmutableArray();
        sb.AppendLine("public sealed partial class GeneratedViewLocator : global::Avalonia.Controls.Templates.IDataTemplate");
        sb.AppendLine("{");
        sb.AppendLine("    public global::Avalonia.Controls.Control? Build(object? param) => param switch");
        sb.AppendLine("    {");
        foreach (var (page, vmType) in pages)
        {
            sb.Append("        ").Append(vmType).Append(" => new ").Append(page.ViewTypeName).AppendLine("(),");
        }

        sb.AppendLine("        _ => null,");
        sb.AppendLine("    };");
        sb.AppendLine();
        sb.Append("    public bool Match(object? data) => data is ");
        for (var i = 0; i < pages.Length; i++)
        {
            if (i > 0)
            {
                sb.Append(" or ");
            }

            sb.Append(pages[i].VmTypeName);
        }

        if (pages.Length == 0)
        {
            sb.Append("false");
        }

        sb.AppendLine(";");
        sb.AppendLine("}");
    }

    private static void DetectCycles(ImmutableArray<CategoryInfo> cats, SourceProductionContext spc)
    {
        var byKey = cats.ToDictionary(c => c.Key, StringComparer.Ordinal);
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var stack = new HashSet<string>(StringComparer.Ordinal);

        foreach (var cat in cats)
        {
            if (visited.Contains(cat.Key))
            {
                continue;
            }

            var current = cat.Key;
            while (current is not null && !visited.Contains(current))
            {
                if (!stack.Add(current))
                {
                    // 环：current 已在当前路径上
                    spc.ReportDiagnostic(Diagnostic.Create(CycleError, cat.Location, cat.Key));
                    return;
                }

                visited.Add(current);
                current = byKey.TryGetValue(current, out var meta) && meta.ParentKey is not null
                    ? meta.ParentKey
                    : null;
            }

            stack.Clear();
        }
    }

    private static string Literal(string? value) =>
        value is null ? "null" : SymbolDisplay.FormatLiteral(value, quote: true);

    private static string LiteralArray(ImmutableArray<string> values) =>
        "new[] { " + string.Join(", ", values.Select(v => SymbolDisplay.FormatLiteral(v, quote: true))) + " }";
}
