# Irihi.Dogma

[![NuGet](https://img.shields.io/nuget/v/Irihi.Dogma)](https://www.nuget.org/packages/Irihi.Dogma)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

A set of Avalonia UI controls designed for documentation and demo scenarios.

> **⚠️ Support & Scope**
>
> This repository exists to serve **IRIHI's own product demos** — it is not a general-purpose, publicly supported library. Before using it, please understand that:
>
> - **No support** — external support requests, issues, and feature suggestions are not processed.
> - **No compatibility guarantees** — APIs may change or break at any time, without notice or semantic-versioning discipline.
> - **Demo-first** — code targets our internal demo scenarios; use cases outside those scenarios are out of scope.
>
> Use at your own risk.

## Features

- **CodeBlock** — syntax-highlighted, selectable, copyable code display control with zero external dependencies
  - Supports AXAML and C# syntax highlighting
  - Built-in light/dark theme support
  - Line numbers and copy button
- **DocSite** — documentation site scaffolding with `DocPage` and `DocCategory` attributes, plus a source-generator that wires them up automatically

## Installation

```shell
dotnet add package Irihi.Dogma
```

## Usage

### CodeBlock

```xml
<dogma:CodeBlock Language="CSharp"
                 Code="{Binding MySourceCode}"
                 ShowLineNumbers="True"
                 ShowCopyButton="True" />
```

### DocSite

Annotate your demo view-models with `[DocPage]` / `[DocCategory]` attributes. The included source generator automatically registers them into `IDocRegistry` at compile time.

```csharp
[DocPage("My Page", Category = "Getting Started")]
public partial class MyPageViewModel : IViewModelProvider { ... }
```

## Building

```shell
dotnet build Irihi.Dogma.slnx
dotnet test Irihi.Dogma.slnx
```

## License

MIT © [Irihi Technology](https://github.com/irihitech)