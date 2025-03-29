using System.Text;

namespace YqlossKeyViewerDotNet.Utils;

public static class PlaceholderUtil
{
    public static string Format(string text, Dictionary<string, object> placeholders)
    {
        var builder = new StringBuilder();

        int leftBracketIndex;
        while ((leftBracketIndex = text.IndexOf('<')) >= 0)
        {
            var rightBracketIndex = text.IndexOf('>', leftBracketIndex + 1);
            if (rightBracketIndex < 0) break;
            var placeholder = text[(leftBracketIndex + 1)..rightBracketIndex];
            builder.Append(text[..leftBracketIndex]);
            text = text[(rightBracketIndex + 1)..];

            var replacement = $"<{placeholder}>";
            if (placeholder.Contains(':'))
            {
                var split = placeholder.Split(':', 2);
                if (placeholders.TryGetValue(split[0], out var value))
                    replacement = ((IFormattable)value).ToString(split[1], null);
            }
            else if (placeholders.TryGetValue(placeholder, out var value))
            {
                replacement = value.ToString();
            }
            else if (placeholder == "{")
            {
                replacement = "<";
            }
            else if (placeholder == "}")
            {
                replacement = ">";
            }

            builder.Append(replacement);
        }

        builder.Append(text);
        return builder.ToString();
    }
}