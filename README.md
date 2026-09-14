# RSCG_DBContext

A Roslyn incremental source generator that augments your Entity Framework Core `DbContext` with `Exists_*` helper methods, letting you check whether a table/`DbSet` (or the whole database) is reachable without writing that boilerplate by hand.

## What it generates

Given:

```csharp
using RSCG_DBContext;

[GenerateDbContextExists]
public partial class EmpContext : DbContext
{
    public DbSet<Employee> Employees { get; set; }
    public DbSet<Department> Departments { get; set; }
}
```

the generator emits a partial class with one `Exists_<DbSetName>()` method per `DbSet<T>` property, plus an `Exists_AllDBSets()` helper that checks them all:

```csharp
public partial class EmpContext
{
    public bool Exists_Employees() { /* returns true if the table can be queried */ }
    public bool Exists_Departments() { /* returns true if the table can be queried */ }
    public bool Exists_AllDBSets() { /* returns true only if every Exists_* above returns true */ }
    public IEnumerable<string> Problem_DBSets() { /* yields the DbSet property name for each DbSet that fails its Exists_* check */ }
}
```

## Requirements

- The class must inherit from `Microsoft.EntityFrameworkCore.DbContext`.
- The class must be declared `partial` (a diagnostic, `RSCGDB002`, is reported otherwise).
- Applying `[GenerateDbContextExists]` to a non-`DbContext` class reports diagnostic `RSCGDB001`.

## Installation

Install the NuGet package as an analyzer-only, build-time dependency:

```powershell
dotnet add package RSCG_DBContext
```

Because the package ships only as a Roslyn analyzer/source generator (no runtime assembly), it is automatically marked as a development dependency and won't add a runtime reference to your published output.

## Usage

1. Reference `Microsoft.EntityFrameworkCore` in your project as usual.
2. Add the `[GenerateDbContextExists]` attribute (from the `RSCG_DBContext` namespace) to your `partial` `DbContext` class.
3. Build the project — the generated `Exists_*` members become available on your context.
4. Use `Problem_DBSets()` to enumerate, by property name, only the `DbSet`s that are currently unreachable — handy for diagnosing which table(s) caused `Exists_AllDBSets()` to return `false`.

See [`ExampleProject`](ExampleProject) for a full sample and [`tests/RSCG_DBContext.Tests`](tests/RSCG_DBContext.Tests) for generator unit tests.

## Building and packing locally

```powershell
dotnet build RSCG_DBContext.slnx
dotnet pack src\RSCG_DBContext\RSCG_DBContext.csproj -c Release -o .\nupkg
```

The resulting `.nupkg` places `RSCG_DBContext.dll` under `analyzers/dotnet/cs` (loaded by the compiler as an analyzer) with no `lib/` assemblies, so consuming projects only get the source generator at compile time.