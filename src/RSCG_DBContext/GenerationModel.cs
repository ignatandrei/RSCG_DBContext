using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace RSCG_DBContext;

public sealed partial class DbContextExistsGenerator
{
    internal sealed class GenerationModel
    {
        public GenerationModel(
            string? namespaceName,
            TypeShapeModel targetType,
            ImmutableArray<TypeShapeModel> containingTypes,
            ImmutableArray<string> dbSetPropertyNames)
        {
            NamespaceName = namespaceName;
            TargetType = targetType;
            ContainingTypes = containingTypes;
            DbSetPropertyNames = dbSetPropertyNames;
        }

        public string? NamespaceName { get; }

        public TypeShapeModel TargetType { get; }

        public ImmutableArray<TypeShapeModel> ContainingTypes { get; }

        public ImmutableArray<string> DbSetPropertyNames { get; }

        public string SourceHintName =>
            ContainingTypes.Length == 0
                ? TargetType.HintNameSegment
                : $"{string.Join(".", ContainingTypes.Select(static type => type.HintNameSegment))}.{TargetType.HintNameSegment}";
    }
}
