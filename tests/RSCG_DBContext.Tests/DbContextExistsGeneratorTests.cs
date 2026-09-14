using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using RSCG_DBContext;

namespace RSCG_DBContext.Tests;

public sealed class DbContextExistsGeneratorTests
{
    private const string EfCoreStubs = """
namespace Microsoft.EntityFrameworkCore
{
    public class DbContext
    {
    }

    public class DbSet<T> : global::System.Linq.IQueryable<T>
    {
        public global::System.Type ElementType => typeof(T);
        public global::System.Linq.Expressions.Expression Expression => throw new global::System.NotImplementedException();
        public global::System.Linq.IQueryProvider Provider => throw new global::System.NotImplementedException();
        public global::System.Collections.Generic.IEnumerator<T> GetEnumerator() => throw new global::System.NotImplementedException();
        global::System.Collections.IEnumerator global::System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
""";

    [Fact]
    public void Generates_exists_methods_for_public_dbsets()
    {
        var source = """
namespace Demo;

using Microsoft.EntityFrameworkCore;
using RSCG_DBContext;

[GenerateDbContextExists]
public partial class SampleContext : DbContext
{
    public DbSet<Person> People { get; } = null!;
    public DbSet<Order> Orders { get; } = null!;
    internal DbSet<InternalOnly> Hidden { get; } = null!;
    public string Name { get; } = string.Empty;
}

public sealed class Person
{
}

public sealed class Order
{
}

public sealed class InternalOnly
{
}
""";

        var result = RunGenerator(source);
        var generatedSource = Assert.Single(
                result.RunResult.Results.Single().GeneratedSources,
                static generated => generated.HintName == "SampleContext.DbContextExists.g.cs")
            .SourceText
            .ToString();

        Assert.Contains("public bool Exists_Orders()", generatedSource, StringComparison.Ordinal);
        Assert.Contains("public bool Exists_People()", generatedSource, StringComparison.Ordinal);
        Assert.Contains("try", generatedSource, StringComparison.Ordinal);
        Assert.Contains("catch", generatedSource, StringComparison.Ordinal);
        Assert.Contains("Queryable.LongCount(this.People)", generatedSource, StringComparison.Ordinal);
        Assert.DoesNotContain("Exists_Hidden", generatedSource, StringComparison.Ordinal);
        Assert.DoesNotContain("Exists_Name", generatedSource, StringComparison.Ordinal);
        Assert.Empty(result.OutputCompilation.GetDiagnostics().Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
    }

    [Fact]
    public void Reports_diagnostic_when_dbcontext_is_not_partial()
    {
        var source = """
using Microsoft.EntityFrameworkCore;
using RSCG_DBContext;

[GenerateDbContextExists]
public class SampleContext : DbContext
{
    public DbSet<Person> People { get; } = null!;
}

public sealed class Person
{
}
""";

        var result = RunGenerator(source);
        var diagnostic = Assert.Single(result.RunResult.Results.Single().Diagnostics);

        Assert.Equal(DbContextExistsGenerator.MustBePartial.Id, diagnostic.Id);
        Assert.Contains("must be declared partial", diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    public void Reports_diagnostic_when_attribute_is_used_on_non_dbcontext()
    {
        var source = """
using RSCG_DBContext;

[GenerateDbContextExists]
public partial class NotAContext
{
}
""";

        var result = RunGenerator(source);
        var diagnostic = Assert.Single(result.RunResult.Results.Single().Diagnostics);

        Assert.Equal(DbContextExistsGenerator.MustInheritDbContext.Id, diagnostic.Id);
        Assert.Contains("is not a DbContext", diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    public void Render_uses_generation_model()
    {
        var source = DbContextExistsGenerator.Render(
            new DbContextExistsGenerator.GenerationModel(
                "Demo.Namespace",
                "SampleContext",
                ["People"]));

        Assert.Contains("namespace Demo.Namespace", source, StringComparison.Ordinal);
        Assert.Contains("partial class SampleContext", source, StringComparison.Ordinal);
        Assert.Contains("Exists_People", source, StringComparison.Ordinal);
    }

    private static GeneratorTestResult RunGenerator(string source)
    {
        var parseOptions = new CSharpParseOptions(LanguageVersion.Latest);
        var syntaxTrees = new[]
        {
            CSharpSyntaxTree.ParseText(EfCoreStubs, parseOptions),
            CSharpSyntaxTree.ParseText(source, parseOptions)
        };

        var compilation = CSharpCompilation.Create(
            assemblyName: "GeneratorTests",
            syntaxTrees: syntaxTrees,
            references: GetMetadataReferences(),
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new DbContextExistsGenerator());
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out _);

        return new GeneratorTestResult(driver.GetRunResult(), outputCompilation);
    }

    private static IReadOnlyList<MetadataReference> GetMetadataReferences()
    {
        var trustedAssemblies = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES");
        return trustedAssemblies!
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Select(static path => MetadataReference.CreateFromFile(path))
            .ToArray();
    }

    private sealed record GeneratorTestResult(GeneratorDriverRunResult RunResult, Compilation OutputCompilation);
}
