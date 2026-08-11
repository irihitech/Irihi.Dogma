using Irihi.Dogma.Docs;

namespace Irihi.Dogma.Demo.Pages;

// 容器分类节点：树形 Docs_Controls > Docs_Buttons / Docs_Input > 页面。
// 分类的菜单标题来自共存 [DocPage] 的标题（Key 只是内部标识，不参与显示）；
// 容器分类同样标 [DocPage] 提供标题（View 可省略，不进 ViewLocator）。

[DocCategory("Docs_Controls", Order = 1, IsClickable = false)]
[DocPage("Docs_Controls")]
public sealed class DocsControlsDefinition;
