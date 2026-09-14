using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace RSCG_DBContext;

public sealed partial class DbContextExistsGenerator
{
    private sealed class GenerationResult
    {
        public GenerationResult(GenerationModel? model, ImmutableArray<Diagnostic> diagnostics)
        {
            Model = model;
            Diagnostics = diagnostics;
        }

        public GenerationModel? Model { get; }

        public ImmutableArray<Diagnostic> Diagnostics { get; }
    }
}
