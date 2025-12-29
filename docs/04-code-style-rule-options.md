# .NET code style rule options

Per-project code style is enforced via `.editorconfig`, aligning with .NET code style rule options.

## Key enforced options (with severities)
- Qualification: no `this.` on fields/properties/methods/events (`false:suggestion`).@/.editorconfig#42-47
- `var` preferred for built-ins, apparent types, and elsewhere; predefined types for locals/member access (`:suggestion`).@/.editorconfig#48-54
- Modifier order enforced (`public, private, protected, internal, file, ...`).@/.editorconfig#39-41
- Async methods must end with `Async` (naming rule with `suggestion`).@/.editorconfig#72-79
- Readonly fields preferred; switch expressions, static local functions, null-propagation, inferred names enabled (mostly `:suggestion`).@/.editorconfig#83-105
- Expression-bodied members allowed for properties/indexers/accessors; discouraged (silent) for methods/ctors.@/.editorconfig#107-114
- Pattern matching preferred and inlined variable declarations enabled.@/.editorconfig#117-121

## How it’s applied
- IDE analyzers surface suggestions during development; consistent formatting in CI when analyzers run.
- Rules are repo-wide (root `.editorconfig` marked `root=true`).@/.editorconfig#1-3

## Extending
- Add more dotnet_style/csharp_style entries to `.editorconfig` following the same `option:value:severity` schema.
- Use `suggestion` severity to nudge; escalate to `warning`/`error` when you need enforcement.
