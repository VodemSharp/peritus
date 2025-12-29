# Central Package Management

The solution uses Central Package Management (CPM) via `Directory.Packages.props` at the repo root.

## Configuration
- CPM enabled and warnings treated as errors:
  ```xml
  <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  ```
  @/Directory.Packages.props#2-5
- Package versions are declared once for all projects, e.g. EF Core, JwtBearer, Serilog, Scalar, Refit, OpenTelemetry, Aspire hosting/test packages, xUnit:
  ```xml
  <PackageVersion Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="10.0.0"/>
  <PackageVersion Include="Serilog.AspNetCore" Version="9.0.0"/>
  <PackageVersion Include="Scalar.AspNetCore" Version="2.11.0"/>
  <PackageVersion Include="Refit" Version="9.0.2"/>
  <PackageVersion Include="OpenTelemetry.Extensions.Hosting" Version="1.13.1"/>
  <PackageVersion Include="Aspire.Hosting.AppHost" Version="13.1.0"/>
  ```
  @/Directory.Packages.props#7-49

## How projects consume
- Individual csproj files omit explicit versions; SDK resolves from CPM.
- Updates happen centrally by editing `Directory.Packages.props`, keeping the fleet consistent.

## Tips
- When adding new packages, add `<PackageVersion Include="..." Version="..."/>` here first.
- Use `TreatWarningsAsErrors` to catch analyzer/package issues early across all projects.
