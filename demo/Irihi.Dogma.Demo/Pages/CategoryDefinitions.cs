using Irihi.Dogma.Docs;

namespace Irihi.Dogma.Demo.Pages;

// 纯容器分类节点（无页面、不可点击）：
// 树形：Docs_Controls > Docs_Buttons / Docs_Input > 页面
// 通过显式 [DocCategory] 声明层级（IsClickable=false 的节点只做分组）。

[DocCategory("Docs_Controls", Order = 1, IsClickable = false)]
public sealed class DocsControlsDefinition;
