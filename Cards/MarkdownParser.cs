using System.Text;
using System.Text.RegularExpressions;

namespace IMK.SettingsUI.Cards
{
    public static class MarkdownParser
    {
        // Regexes cached
        private static readonly Regex BoldRx = new Regex(@"\*\*(.+?)\*\*", RegexOptions.Compiled);
        private static readonly Regex ItalicRx = new Regex(@"(?<!\*)\*(?!\*)(.+?)(?<!\*)\*(?!\*)", RegexOptions.Compiled); // single * not part of **
        private static readonly Regex CodeRx = new Regex(@"`([^`]+?)`", RegexOptions.Compiled);

        public static string ToRichText(string md)
        {
            if (string.IsNullOrEmpty(md)) return string.Empty;
            var lines = md.Replace('\r', '\n').Split('\n');
            var sb = new StringBuilder();
            foreach (var raw in lines)
            {
                var line = raw.TrimEnd();
                if (string.IsNullOrWhiteSpace(line)) { sb.Append('\n'); continue; }
                var formattedLine = FormatInline(ParseBlock(line));
                sb.Append(formattedLine).Append('\n');
            }
            return sb.ToString();
        }
        private static string ParseBlock(string line)
        {
            // Headings: count leading '#'
            int hashCount = 0; for (int i = 0; i < line.Length && line[i] == '#'; i++) hashCount++;
            if (hashCount > 0 && line.Length > hashCount && line[hashCount] == ' ')
            {
                string content = line[(hashCount + 1)..].Trim();
                int size = hashCount switch { 1 => 20, 2 => 16, 3 => 14, 4 => 13, _ => 12 };
                return $"<b><size={size}>{content}</size></b>";
            }
            // Block quote
            if (line.StartsWith("> "))
            {
                string content = line[2..].Trim();
                return $"<color=#88ccee><i>{content}</i></color>";
            }
            // Unordered list '- ' keep dash; indent slight with color
            if (line.StartsWith("- "))
            {
                string content = line[2..].Trim();
                return $"- {content}"; // keep raw '-' to avoid missing glyph
            }
            return line; // plain
        }
        private static string FormatInline(string line)
        {
            // code first (avoid formatting insides)
            line = CodeRx.Replace(line, m => $"<color=#cccc66><i>{Escape(m.Groups[1].Value)}</i></color>");
            // bold then italic (avoid interference)
            line = BoldRx.Replace(line, m => $"<b>{m.Groups[1].Value}</b>");
            line = ItalicRx.Replace(line, m => $"<i>{m.Groups[1].Value}</i>");
            return line;
        }
        private static string Escape(string s)
        {
            return s.Replace("<", "&lt;").Replace(">", "&gt;");
        }
    }
}
