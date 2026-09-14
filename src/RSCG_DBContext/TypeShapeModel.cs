using System.Text;

namespace RSCG_DBContext;

public sealed partial class DbContextExistsGenerator
{
    internal sealed class TypeShapeModel
    {
        public TypeShapeModel(string name, string declarationKeyword, string? hintNameSegment = null)
        {
            Name = name;
            DeclarationKeyword = declarationKeyword;
            string safeHintNameSegment;
            if (string.IsNullOrWhiteSpace(hintNameSegment))
            {
                safeHintNameSegment = name;
            }
            else
            {
                safeHintNameSegment = hintNameSegment!;
            }

            HintNameSegment = SanitizeHintNameSegment(safeHintNameSegment);
        }

        public string Name { get; }

        public string DeclarationKeyword { get; }

        public string HintNameSegment { get; }

        private static string SanitizeHintNameSegment(string value)
        {
            var builder = new StringBuilder(value.Length);

            foreach (var character in value)
            {
                builder.Append(char.IsLetterOrDigit(character) || character is '.' or '_' or '-'
                    ? character
                    : '_');
            }

            return builder.ToString();
        }
    }
}
