using Irihi.Lingua;

namespace Irihi.Dogma.Docs;

/// <summary>
/// 文档站注册表与核心逻辑：收集分类/页面元数据，构建分类树，提供搜索。
/// </summary>
public sealed class DocSite : IDocRegistry
{
    /// <summary>全局实例（宿主持有并注入 LinguaManager / ViewModelProvider）。</summary>
    public static DocSite Instance { get; } = new();

    private readonly List<DocCategoryMetadata> _categories = [];
    private IReadOnlyList<DocCategoryNode>? _roots;

    /// <summary>文本来源（Lingua）；null 时标题回退到 fallback/键字面量。</summary>
    public ILinguaManager? LinguaManager { get; set; }

    /// <summary>VM 获取语义（默认每次新建；宿主可注入单例缓存/DI 实现）。</summary>
    public IViewModelProvider ViewModelProvider { get; set; } = new DefaultViewModelProvider();

    /// <summary>注册数据变化（AddCategory）后触发，宿主据此重建 UI。</summary>
    public event Action? TreeChanged;

    /// <inheritdoc />
    public void AddCategory(DocCategoryMetadata category)
    {
        _categories.Add(category);
        _roots = null;
        TreeChanged?.Invoke();
    }

    /// <summary>顶层分类节点（排序后）。</summary>
    public IReadOnlyList<DocCategoryNode> Roots => _roots ??= BuildTree();

    /// <summary>树遍历收集的所有页面。</summary>
    public IEnumerable<DocPageNode> AllPages
    {
        get
        {
            foreach (var root in Roots)
            {
                foreach (var page in CollectPages(root))
                {
                    yield return page;
                }
            }
        }
    }

    /// <summary>
    /// 按查询搜索页面：匹配标题 fallback/键、关键字、所属分类键；标题命中优先。
    /// </summary>
    public IEnumerable<DocPageNode> Search(string query)
    {
        var q = query.Trim();
        if (q.Length == 0)
        {
            return [];
        }

        var parts = q.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return AllPages
            .Select(p => (Page: p, Score: Score(p, parts)))
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Page.Metadata.TitleKey, StringComparer.Ordinal)
            .Select(x => x.Page)
            .ToList();
    }

    private static int Score(DocPageNode page, string[] parts)
    {
        var m = page.Metadata;
        var title = string.Join(" ", new[] { m.FallbackTitle, m.TitleKey });
        var keywords = string.Join(" ", m.Keywords);
        var category = page.Category.Metadata.Key;
        var score = 0;
        foreach (var part in parts)
        {
            if (title.Contains(part, StringComparison.OrdinalIgnoreCase))
            {
                score += 2;
            }

            if (keywords.Contains(part, StringComparison.OrdinalIgnoreCase))
            {
                score += 1;
            }

            if (category.Contains(part, StringComparison.OrdinalIgnoreCase))
            {
                score += 1;
            }
        }

        return score;
    }

    private IReadOnlyList<DocCategoryNode> BuildTree()
    {
        // 1. 显式分类索引（注册顺序稳定）
        var byKey = new Dictionary<string, DocCategoryMetadata>(StringComparer.Ordinal);
        var registrationOrder = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var i = 0; i < _categories.Count; i++)
        {
            byKey[_categories[i].Key] = _categories[i];
            registrationOrder[_categories[i].Key] = i;
        }

        // 2. 隐式创建被引用但未声明的父节点（容器：IsClickable=false、无页面）
        var nextOrder = _categories.Count;
        var addedImplicit = new List<string>();
        foreach (var meta in _categories)
        {
            var parentKey = meta.ParentKey;
            while (parentKey is not null && !byKey.ContainsKey(parentKey))
            {
                byKey[parentKey] = new DocCategoryMetadata
                {
                    Key = parentKey,
                    IsExplicit = false,
                    IsClickable = false,
                };
                registrationOrder[parentKey] = nextOrder++;
                addedImplicit.Add(parentKey);
                parentKey = null; // 隐式节点无父链
            }
        }

        // 3. 建分类节点（页面稍后挂载，因需引用分类节点自身）
        var nodes = new Dictionary<string, DocCategoryNode>(StringComparer.Ordinal);
        foreach (var meta in byKey.Values)
        {
            nodes[meta.Key] = new DocCategoryNode
            {
                Metadata = meta,
                Title = ResolveTitle(meta.Key, meta.Key),
            };
        }

        // 3b. 共存页面挂载到其分类节点
        foreach (var node in nodes.Values)
        {
            if (node.Metadata.Page is { } pageMeta)
            {
                node.Page = new DocPageNode
                {
                    Metadata = pageMeta,
                    Title = ResolveTitle(pageMeta.TitleKey, pageMeta.FallbackTitle ?? pageMeta.TitleKey),
                    Category = node,
                };
            }
        }

        // 4. 连接父子（byKey 已保证 parent 存在；SG 已拦截成环）
        foreach (var node in nodes.Values)
        {
            var parentKey = node.Metadata.ParentKey;
            if (parentKey is not null && nodes.TryGetValue(parentKey, out var parent) && !ReferenceEquals(parent, node))
            {
                node.Parent = parent;
            }
        }

        // 5. 填充 Children（同级按 Order + 注册顺序稳定排序）
        foreach (var node in nodes.Values)
        {
            node.Children = nodes.Values
                .Where(n => n.Metadata.ParentKey == node.Metadata.Key)
                .OrderBy(n => n.Metadata.Order)
                .ThenBy(n => registrationOrder.GetValueOrDefault(n.Metadata.Key))
                .ToList();
        }

        // 6. 顶层 = 无父节点
        return nodes.Values.Where(n => n.Parent is null).ToList();
    }

    private IObservable<string?> ResolveTitle(string key, string fallback)
    {
        var observable = LinguaManager?.GetObservable(key);
        return observable ?? new ObservableValue<string?>(fallback);
    }

    private static IEnumerable<DocPageNode> CollectPages(DocCategoryNode node)
    {
        if (node.Page is { } page)
        {
            yield return page;
        }

        foreach (var child in node.Children)
        {
            foreach (var p in CollectPages(child))
            {
                yield return p;
            }
        }
    }
}
