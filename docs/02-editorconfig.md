# EditorConfig & code style

The solution enforces consistent formatting and naming via `.editorconfig` at the repo root.

## Defaults
- 4-space indentation, trim trailing whitespace, final newline.@/.editorconfig#4-12
- UTF-8 charset for project files.@/.editorconfig#13-16

## C# formatting highlights
- Braces on new lines for all blocks; wrap object/collection initializers (`chop_always`).@/.editorconfig#18-29
- Indent block contents, switch labels, case contents; braces not indented.@/.editorconfig#31-37
- Prefer `var` everywhere and predefined types for locals/members; avoid `this.` qualification.@/.editorconfig#42-54
- Naming:
  - Constants: PascalCase.@/.editorconfig#55-62
  - Private/internal fields: `_camelCase`.@/.editorconfig#63-70
  - Async methods must end with `Async`.@/.editorconfig#72-79
- Using directives outside namespace; sort System first.@/.editorconfig#82-85
- Prefer braces, switch expressions, static local functions, null propagation, conditional expressions, collection/object initializers, inferred names, readonly fields.@/.editorconfig#83-105
- Expression-bodied: allowed for operators/properties/indexers/accessors/lambdas/local functions; not for methods/ctors (silent hints).@/.editorconfig#107-115
- Pattern matching preferred, inlined declarations enabled.@/.editorconfig#117-121
- Spacing rules set for casts, commas, keywords, operators, etc.@/.editorconfig#131-154

## Other file types
- XML/props/targets/resx/JSON/YAML have 2-space indent defaults; resx keeps trailing whitespace and no final newline.@/.editorconfig#155-180
- Shell scripts enforce LF; bat/cmd enforce CRLF.@/.editorconfig#181-186

## How it’s used
- IDEs/build analyzers honor these rules, giving suggestions/hints (severity set mostly to `suggestion`).
- Works with central package management and solution analyzers to keep consistent style across projects.
