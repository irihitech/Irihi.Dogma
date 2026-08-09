using Irihi.Dogma.Docs;

namespace Irihi.Dogma.Demo;

/// <summary>
/// 本项目的 DocSite 实例（示范"每个项目使用自己的 DocSite 子类"）：
/// 承载本项目的页面注册与状态，不依赖全局 <see cref="DocSite.Instance"/>，
/// 库特定扩展可在此子类上添加。
/// </summary>
public sealed class DemoDocSite : DocSite
{
}
