# 计划：CodeBlock 代码高亮控件（零外部依赖）

> 状态：待批准 / 分支：`feature/code-highlight-displayer`
> 日期：2026-07-31

## 背景与目标

为 Avalonia 各类 demo 项目提供**代码块展示控件**，主要用于显示 **AXAML** 与 **C#** 源码，要求：

- 语法高亮（AXAML + C#）
- 文本可选择、可复制（拖选 + Ctrl+C + 右键菜单）
- **零外部依赖**，仅用 Avalonia 原生 API

## 核心方案（调研结论）

- **`SelectableTextBlock`**（Avalonia 11.0+ 内置，本仓库 12.1.1 自带）：继承 `TextBlock`，
  原生支持选择/复制（`SelectionStart/End`、`SelectedText`、`Copy()`、`SelectAll()`、`CopyingToClipboard`），
  且支持 `Inlines`（`Run` 可独立设置 `Foreground`/`FontWeight` 等）→ **高亮 + 选择复制同时获得，无需自绘或叠加**。
- 词法分析器**手写状态机**（AXAML 本质是 XML，但用 `System.Xml` 解析会丢失属性值引号、空白等原文，
  手写可保证 token 拼接 round-trip == 原始源码）。
- 渲染：token 流 → `Run` 序列 → `SelectableTextBlock.Inlines`。

参考资料：
- SelectableTextBlock 源码：<https://github.com/AvaloniaUI/Avalonia/blob/11.0.0/src/Avalonia.Controls/SelectableTextBlock.cs>
- 官方文档（"需要选择复制的文本用 SelectableTextBlock"）：<https://docs.avaloniaui.net/controls/data-display/text-display/textblock>
- Inlines/Run 文档：<https://docs.avaloniaui.net/controls/data-display/text-display/textblock#inlines>
- 剪贴板：`TopLevel.GetTopLevel(control)?.Clipboard` + `ClipboardExtensions.SetTextAsync(clipboard, text)`

## 已确认决策

| 项 | 决策 |
|---|---|
| 特性范围 | 行号 + 复制全部按钮 + 自定义配色（主题）✅；语言自动识别 ❌ |
| 代码来源 | 仅字符串属性（`Code`），demo 内嵌字符串常量 |
| 高亮精度 | 尽量完整词法（含插值字符串、MarkupExtension、预处理指令、CDATA 等） |
| 依赖 | 零外部依赖（仅 Avalonia + BCL） |

## 控件 API（草案）

```
CodeBlock : TemplatedControl
  string      Code              // 待展示源码
  CodeLanguage Language         // Axaml | CSharp
  CodeTheme   Theme             // Dark（默认）| Light
  bool        ShowLineNumbers   // 默认 true
  bool        ShowCopyButton    // 默认 true
```

模板结构：

```
Grid
 ├─ ScrollViewer (Auto/Auto)
 │   └─ Grid [Auto,*]
 │       ├─ TextBlock LineNumbers   // 1..N，同字体/字号/LineHeight 对齐
 │       └─ SelectableTextBlock CodeText
 └─ Button CopyButton（右上角覆盖）
```

统一等宽字体（Consolas / Cascadia Mono，跨平台 fallback）+ 固定 FontSize/LineHeight 保证行号对齐。

## 文件结构

```
src/Irihi.Dogma/
  CodeBlock/CodeBlock.cs            // 控件：属性 + 模板应用 + Inlines 重建
  CodeBlock/CodeBlock.axaml         // 模板（行号栏 + SelectableTextBlock + 复制按钮）
  CodeBlock/CodeLanguage.cs         // enum Axaml | CSharp
  CodeBlock/CodeTheme.cs            // enum Dark | Light
  CodeBlock/TokenKind.cs            // token 类型枚举
  CodeBlock/CodeToken.cs            // Kind + Text
  CodeBlock/CodePalette.cs          // TokenKind → Brush 映射（Dark/Light 两套）
  CodeBlock/CodeHighlightRenderer.cs// token 流 → InlineCollection（含 round-trip 断言）
  CodeBlock/Lexers/AxamlLexer.cs
  CodeBlock/Lexers/CSharpLexer.cs
test/Irihi.Dogma.Tests/             // tokenizer 单元测试
test/Irihi.Dogma.HeadlessTests/     // 控件行为测试
demo/Irihi.Dogma.Demo/              // MainWindow 展示 AXAML + C# 源码
```

## 分层任务

1. **控件骨架与渲染管线**
   - 定义 `CodeLanguage`/`CodeTheme`/`TokenKind`/`CodeToken`/`CodePalette`（Dark/Light，关键字加粗）
   - `CodeHighlightRenderer`：`List<CodeToken>` → `InlineCollection`，round-trip 断言（拼接 == 原码）
   - `CodeBlock` 控件 + `CodeBlock.axaml` 模板（ScrollViewer / 行号 / SelectableTextBlock / 复制按钮）
   - demo 接入：App.axaml `StyleInclude` + MainWindow 占位 CodeBlock
   - 验证：`Run` 内 `\n` 是否换行（若不换行则显式拆 `LineBreak`）；`dotnet build` 通过

2. **AXAML 词法分析器**
   - 手写状态机：文本、开始/结束标签、元素名、属性名、`=`、属性值（保留引号/实体原文）、自闭合 `/`、注释、CDATA、`<? ?>`、`<!DOCTYPE>`
   - MarkupExtension 分解：`{Binding Path=X, Converter={StaticResource Y}}` → `{`/扩展名/键值参数/嵌套扩展/`}`，处理 `{}` 转义
   - 单元测试：多类片段 round-trip + token 类型断言

3. **C# 词法分析器**
   - 状态机：关键字（含 contextual）、标识符、数字字面量（十六进制/二进制/浮点/后缀/`_` 分隔符）、字符 `'...'`
   - 字符串族：普通 `"..."`、verbatim `@"..."`、插值 `$"..."`/`$@"..."`（`{{`/`}}` 转义、嵌套 `{expr}` 递归）
   - 注释 `//`、`/* */`、`///`（单独类别）、预处理指令 `#if/#region`、运算符/标点分类
   - 单元测试：round-trip + 类型断言

4. **行号、复制按钮与主题整合**
   - 行号栏：按 `Code.Split('\n')` 生成 `1..N`，与代码同字体/字号/LineHeight 对齐，同 ScrollViewer 内同步滚动
   - 复制按钮：`TopLevel.GetTopLevel` + `ClipboardExtensions.SetTextAsync(Code)`，短暂"已复制"反馈
   - 主题：`Theme` 切换对应 `CodePalette` 重建 Run；Run 附带 `token-*` Classes 供 XAML 样式覆盖
   - Headless 测试：Inlines 数量/拼接文本、行号文本、复制按钮点击不抛异常

5. **demo 完善与全量验证**
   - MainWindow 展示真实 AXAML 源码 + C# 源码（含注释/字符串/插值），暗色主题
   - `dotnet build`（解决方案）+ `dotnet test`（两个测试项目）全绿
   - 视觉核对：高亮正确、拖选复制、Ctrl+C、右键复制、复制按钮、行号对齐

## 风险备注

- AXAML 要求合法 XML：demo 源码均合法；解析失败时降级为纯文本显示，不崩溃
- 等宽字体跨平台 fallback 需验证
- `Run` 内换行行为是 Phase 1 首个验证点
