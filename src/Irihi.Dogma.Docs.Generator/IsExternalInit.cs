// netstandard2.0 缺少 IsExternalInit，record/init 需要此类型。
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}
