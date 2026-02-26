using System;
using System.Text;

namespace Reloader.UI
{
    internal static class ItemDisplayNameFormatter
    {
        public static string Format(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                return string.Empty;
            }

            var tokens = itemId.Split(new[] { '_', '-', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length == 0)
            {
                return string.Empty;
            }

            var builder = new StringBuilder(itemId.Length);
            for (var i = 0; i < tokens.Length; i++)
            {
                if (i > 0)
                {
                    builder.Append(' ');
                }

                var token = tokens[i];
                if (token.Length == 0)
                {
                    continue;
                }

                var first = token[0];
                if (char.IsLetter(first))
                {
                    builder.Append(char.ToUpperInvariant(first));
                    if (token.Length > 1)
                    {
                        builder.Append(token.AsSpan(1).ToString().ToLowerInvariant());
                    }
                }
                else
                {
                    builder.Append(token);
                }
            }

            return builder.ToString();
        }
    }
}
