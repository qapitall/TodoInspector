using System;
using System.Text.RegularExpressions;

namespace TodoInspector
{
    internal static class TodoParser
    {
        private static readonly Regex TodoRegex = new Regex(@"\G//\s*(?:todo|to-do)(?:-([\w]+))?(?:-([\w]+))?\s+(.*)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase
        );

        internal static bool TryParse(string line, int lineNumber, string filePath, out TodoEntry entry)
        {
            entry = default;

            if (line == null)
                return false;

            int commentIndex = line.IndexOf("//", StringComparison.Ordinal);
            if (commentIndex < 0)
                return false;

            if (line.IndexOf("todo", commentIndex, StringComparison.OrdinalIgnoreCase) == -1 &&
                line.IndexOf("to-do", commentIndex, StringComparison.OrdinalIgnoreCase) == -1)
            {
                return false;
            }

            Match match = TodoRegex.Match(line, commentIndex);
            if (!match.Success)
                return false;

            string segment1 = match.Groups[1].Success ? match.Groups[1].Value : null;
            string segment2 = match.Groups[2].Success ? match.Groups[2].Value : null;
            string message = match.Groups[3].Success ? match.Groups[3].Value.Trim() : string.Empty;

            string user = null;
            TodoPriority priority = TodoPriority.None;

            if (segment1 != null && segment2 != null)
            {
                user = segment1;
                priority = ParsePriority(segment2);
            }
            else if (segment1 != null)
            {
                TodoPriority parsed = ParsePriority(segment1);
                if (parsed != TodoPriority.None)
                {
                    priority = parsed;
                }
                else
                {
                    user = segment1;
                }
            }

            entry = new TodoEntry(filePath, lineNumber, line.Trim(), user, priority, message);
            return true;
        }

        private static TodoPriority ParsePriority(string value)
        {
            if (string.IsNullOrEmpty(value))
                return TodoPriority.None;

            return value.ToLowerInvariant() switch
            {
                "low" => TodoPriority.Low,
                "medium" => TodoPriority.Medium,
                "high" => TodoPriority.High,
                "highest" => TodoPriority.Highest,
                _ => TodoPriority.None
            };
        }
    }
}