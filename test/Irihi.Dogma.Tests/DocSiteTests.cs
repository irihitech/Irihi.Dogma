using Irihi.Dogma.Docs;
using Xunit;

namespace Irihi.Dogma.Tests;

public class DocSiteTests
{
    private static DocPageMetadata Page(
        string titleKey,
        string? fallback = null,
        string[]? keywords = null,
        Func<object>? factory = null)
        => new()
        {
            TitleKey = titleKey,
            FallbackTitle = fallback,
            ViewModelType = typeof(object),
            ViewType = typeof(object),
            Keywords = keywords ?? [],
            ViewModelFactory = factory ?? (() => new object()),
        };

    private static DocCategoryMetadata Cat(
        string key,
        string? parent = null,
        int order = 0,
        bool clickable = true,
        string[]? tags = null,
        DocPageMetadata? page = null)
        => new()
        {
            Key = key,
            ParentKey = parent,
            Order = order,
            IsClickable = clickable,
            Tags = tags ?? [],
            Page = page,
        };

    private static DocSite CreateSite(params DocCategoryMetadata[] categories)
    {
        var site = new DocSite();
        foreach (var c in categories)
        {
            site.AddCategory(c);
        }

        return site;
    }

    // ---- 树构建 ----

    [Fact]
    public void Builds_Two_Level_Tree_With_Parent_Child_Links()
    {
        var site = CreateSite(
            Cat("Root", order: 1),
            Cat("Child", parent: "Root", order: 2, page: Page("Child.Title", "Child")));

        var root = Assert.Single(site.Roots);
        Assert.Equal("Root", root.Metadata.Key);
        Assert.Null(root.Parent);

        var child = Assert.Single(root.Children);
        Assert.Equal("Child", child.Metadata.Key);
        Assert.Same(root, child.Parent);
    }

    [Fact]
    public void Sibling_Order_By_Order_Then_Registration()
    {
        var site = CreateSite(
            Cat("B", parent: "Root", order: 2),
            Cat("A", parent: "Root", order: 1),
            Cat("Root", order: 0));

        var root = Assert.Single(site.Roots);
        Assert.Equal(new[] { "A", "B" }, root.Children.Select(c => c.Metadata.Key));
    }

    [Fact]
    public void Implicit_Node_Created_For_UnDeclared_Parent()
    {
        var site = CreateSite(
            Cat("Child", parent: "UnDeclared", page: Page("T", "t")));

        var root = Assert.Single(site.Roots);
        Assert.Equal("UnDeclared", root.Metadata.Key);
        Assert.False(root.Metadata.IsExplicit);
        Assert.False(root.IsClickable);
        Assert.Null(root.Page);

        var child = Assert.Single(root.Children);
        Assert.Equal("Child", child.Metadata.Key);
    }

    // ---- 共存绑定 ----

    [Fact]
    public void CoAttributed_Page_Is_Mounted_On_Category()
    {
        var page = Page("Child.Title", "Child");
        var site = CreateSite(Cat("Child", page: page));

        var node = Assert.Single(site.Roots);
        Assert.NotNull(node.Page);
        Assert.Same(node, node.Page!.Category);
        Assert.Same(page, node.Page.Metadata);
        Assert.Equal("Child.Title", Assert.Single(site.AllPages).Metadata.TitleKey);
    }

    // ---- IsClickable ----

    [Fact]
    public void IsClickable_Defaults_To_True()
    {
        var site = CreateSite(Cat("A"), Cat("B", clickable: false));
        var nodes = site.Roots;
        Assert.True(nodes[0].IsClickable);
        Assert.False(nodes[1].IsClickable);
    }

    // ---- 标题 fallback ----

    [Fact]
    public void Title_Falls_Back_Without_LinguaManager()
    {
        var site = CreateSite(
            Cat("Root"),
            Cat("Child", parent: "Root", page: Page("Child.Title", "Child Display")));

        var child = site.Roots[0].Children[0];
        // 分类标题 = 共存页面的标题（fallback）
        Assert.Equal("Child Display", GetValue(child.Title));
        Assert.Equal("Child Display", GetValue(child.Page!.Title));
    }

    [Fact]
    public void Category_Without_Page_Title_Falls_Back_To_Key()
    {
        var site = CreateSite(Cat("Root"), Cat("Pure", parent: "Root"));

        var pure = site.Roots[0].Children[0];
        // 无页面的容器节点：标题 fallback 到 Key 字面量
        Assert.Equal("Pure", GetValue(pure.Title));
        Assert.Null(pure.Page);
    }

    // ---- 搜索 ----

    [Fact]
    public void Search_Matches_Title_Keywords_And_Category()
    {
        var site = CreateSite(
            Cat("Controls", parent: null, page: Page("Controls.Title", "Controls", new[] { "ui", "widget" })),
            Cat("Buttons", parent: "Controls", page: Page("Buttons.Title", "Buttons")));

        Assert.Single(site.Search("ui"));          // 关键字命中
        Assert.Single(site.Search("Buttons"));     // 标题命中
        Assert.Single(site.Search("Controls"));    // 分类命中（Controls 页面）
        Assert.Equal(1, site.Search("controls").Count());  // 仅 Controls 页面的分类键命中
        Assert.Empty(site.Search("nothing"));
        Assert.Empty(site.Search("  "));           // 空白查询
    }

    [Fact]
    public void Search_Ranks_Title_Hit_Above_Keyword_Hit()
    {
        var site = CreateSite(
            Cat("A", page: Page("Alpha.Title", "Alpha", new[] { "click" })),
            Cat("B", page: Page("Beta.Title", "Clickable")));

        var results = site.Search("click").ToList();
        Assert.Equal(2, results.Count);
        Assert.Equal("Beta.Title", results[0].Metadata.TitleKey);   // 标题命中优先
        Assert.Equal("Alpha.Title", results[1].Metadata.TitleKey);  // 关键字命中
    }

    // ---- provider ----

    [Fact]
    public void Default_Provider_Creates_New_Instance_Each_Time()
    {
        var site = CreateSite(Cat("A", page: Page("A.Title", "A")));
        var page = Assert.Single(site.AllPages);
        Assert.NotSame(site.ViewModelProvider.GetViewModel(page), site.ViewModelProvider.GetViewModel(page));
    }

    [Fact]
    public void Custom_Provider_Can_Cache_Singleton()
    {
        var site = CreateSite(Cat("A", page: Page("A.Title", "A")));
        site.ViewModelProvider = new SingletonViewModelProvider();

        var page = Assert.Single(site.AllPages);
        Assert.Same(site.ViewModelProvider.GetViewModel(page), site.ViewModelProvider.GetViewModel(page));
    }

    // ---- 事件 ----

    [Fact]
    public void TreeChanged_Raised_On_AddCategory()
    {
        var site = new DocSite();
        var raised = 0;
        site.TreeChanged += () => raised++;
        site.AddCategory(Cat("A"));
        Assert.Equal(1, raised);
    }

    [Fact]
    public void Subclass_Can_Override_Scoring_And_CollectPages()
    {
        // 验证 protected/virtual 扩展点：子类可 override 搜索评分与页面收集
        var site = new CustomDocSite();
        site.AddCategory(Cat("A", page: Page("A.Title", "Alpha")));
        site.AddCategory(Cat("B", page: Page("B.Title", "Beta")));

        // 自定义评分：全部恒定 1 → 搜索返回按标题排序而非加权
        var results = site.Search("alpha beta").ToList();
        Assert.Equal(2, results.Count);
        Assert.Equal("A.Title", results[0].Metadata.TitleKey);

        // 自定义收集：跳过含 "skip" 关键字的页面
        site.AddCategory(Cat("C", page: Page("C.Title", "Gamma", new[] { "skip" })));
        Assert.DoesNotContain(site.AllPages, p => p.Metadata.TitleKey == "C.Title");
    }

    private sealed class CustomDocSite : DocSite
    {
        protected override int Score(DocPageNode page, string[] parts) => 1;

        protected override IEnumerable<DocPageNode> CollectPages(DocCategoryNode node)
        {
            foreach (var page in base.CollectPages(node))
            {
                if (!page.Metadata.Keywords.Contains("skip"))
                {
                    yield return page;
                }
            }
        }
    }

    private static T GetValue<T>(IObservable<T> observable)
    {
        T? result = default;
        observable.Subscribe(new SimpleObserver<T>(v => result = v));
        return result!;
    }

    private sealed class SimpleObserver<T>(Action<T> onNext) : IObserver<T>
    {
        public void OnCompleted() { }
        public void OnError(Exception error) { }
        public void OnNext(T value) => onNext(value);
    }

    private sealed class SingletonViewModelProvider : IViewModelProvider
    {
        private readonly Dictionary<Type, object> _cache = [];

        public object GetViewModel(DocPageNode page)
        {
            var type = page.Metadata.ViewModelType;
            return _cache.TryGetValue(type, out var vm)
                ? vm
                : _cache[type] = page.Metadata.ViewModelFactory();
        }
    }
}
