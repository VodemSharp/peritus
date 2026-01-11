# Custom templates for dotnet new

This project ships a custom `dotnet new` template that scaffolds the full Peritus Aspire modular solution.

## Template manifest
- `.template.config/template.json` defines the template metadata, outputs, exclusions, and generated symbols. Key fields:
  ```json
  "identity": "Peritus.Solution.Template",
  "shortName": "peritus",
  "description": "Creates the Peritus modular Aspire solution that contains the API, AppHost, ServiceDefaults, and module projects.",
  "primaryOutputs": [{ "path": "Peritus.slnx" }],
  "sourceName": "Peritus"
  ```
  The template excludes git/IDE/build artifacts and uses a generated `appHostSecret` GUID symbol for replacements.@/.template.config/template.json#1-68

## How to pack and install locally
1) Pack the template project:
   ```bash
   dotnet pack Peritus.Templates.csproj -c Release -o ./nupkg
   ```
2) Install the produced nupkg:
   ```bash
   dotnet new install ./nupkg/Peritus.Templates.1.0.0.nupkg
   ```
3) Verify:
   ```bash
   dotnet new --list | grep Peritus
   ```

## Using the template
- Create a new solution:
  ```bash
  dotnet new peritus -n MyCompany.Peritus
  ```
- The generated solution includes API (`src/api/Peritus.Api`), module(s), shared libraries (`src/common/*`), contracts, Aspire host (`src/host/Peritus.AppHost`), and service defaults.

## Post-actions
- The template includes a restore post-action so `dotnet restore` runs automatically after creation (can be skipped with `--skip-restore`).@/.template.config/template.json#54-66

## Tips when extending the template
- Add new files/projects under the solution root; ensure they’re not excluded by the manifest’s `exclude` list.@/.template.config/template.json#27-40
- Use additional generated symbols for secrets/IDs when new infra components are added.
- Keep `sourceName` consistent so identifiers rename correctly on `dotnet new` invocation.
