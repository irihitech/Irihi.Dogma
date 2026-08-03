namespace Irihi.Dogma.Controls;

/// <summary>
/// 一个词法 token：类别 + 原始文本。
/// 所有 token 的文本按顺序拼接必须等于原始源码（round-trip 保证）。
/// </summary>
/// <param name="Kind">token 类别</param>
/// <param name="Text">原始文本（含空白、引号、标点等）</param>
public readonly record struct CodeToken(TokenKind Kind, string Text);
