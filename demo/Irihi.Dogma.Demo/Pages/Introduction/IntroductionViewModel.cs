using Irihi.Dogma.Docs;

namespace Irihi.Dogma.Demo.Pages;

/// <summary>
/// 根级 Introduction 页面：刻意不实现 IPageMetadataProvider、不配置 PageMetadata，
/// 演示 Shell 对无元信息页面的处理（元信息卡片整体隐藏）。
/// </summary>
[DocCategory("Docs_Introduction", Order = 0)]
[DocPage("Docs_Introduction_Title", View = typeof(IntroductionView), Keywords = new[] { "introduction", "welcome" })]
public sealed partial class IntroductionViewModel;
