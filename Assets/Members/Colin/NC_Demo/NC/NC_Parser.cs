using System.Globalization;
using System.Text;

/// <summary>
/// Text → NC_Program for the Intelitek ProMill (CNCBase) dialect: a block is a string of
/// letter+number words, ';' comments to end of line (parenthesised comments are tolerated
/// too), '%' program markers, '/' block skip, and an optional 'O' program label. Comment
/// lines before the first code block that read "; KEY: value" fill the header.
/// </summary>
public static class NC_Parser
{
    public static NC_Program Parse(string name, string text)
    {
        var p = new NC_Program { Name = name };
        string[] lines = (text ?? "").Split('\n');
        bool in_header = true;
        for (int i = 0; i < lines.Length; i++)
        {
            string code = Strip_Comment(lines[i].TrimEnd('\r'), out string comment).Trim();
            int line = i + 1;

            if (code.Length == 0)
            {
                if (in_header && comment != null) Read_Header(p, comment);
                continue;
            }
            char first = char.ToUpperInvariant(code[0]);
            if (first == '%' || first == '/' || first == '\\') continue;          // marker, block skip, skip
            if (first == '$')
            {
                p.Notices.Add(new NC_Notice(NC_Notice.Level.Warning, line, "$ (absolute arc centres) is not supported; I/J are read as incremental"));
                continue;
            }
            if (first == 'O' && Is_Label(code)) continue;                          // program number

            in_header = false;
            var block = new NC_Block { Index = p.Blocks.Count, Line = line, Text = code };
            Tokenise(code, block, p);
            p.Blocks.Add(block);
        }
        return p;
    }

    static string Strip_Comment(string raw, out string comment)
    {
        comment = null;
        int semi = raw.IndexOf(';');
        if (semi >= 0)
        {
            comment = raw.Substring(semi + 1);
            raw = raw.Substring(0, semi);
        }
        int open = raw.IndexOf('(');
        while (open >= 0)
        {
            int close = raw.IndexOf(')', open + 1);
            if (close < 0) close = raw.Length - 1;
            if (comment == null) comment = raw.Substring(open + 1, close - open - 1);
            raw = raw.Remove(open, close - open + 1);
            open = raw.IndexOf('(');
        }
        return raw;
    }

    static void Read_Header(NC_Program p, string comment)
    {
        int colon = comment.IndexOf(':');
        if (colon <= 0) return;
        string key = comment.Substring(0, colon).Trim().ToUpperInvariant();
        string value = comment.Substring(colon + 1).Trim();
        if (key.Length == 0 || value.Length == 0) return;
        for (int i = 0; i < key.Length; i++)
            if (!char.IsLetter(key[i]) && key[i] != '_') return;
        p.Header[key] = value;
    }

    static bool Is_Label(string code)
    {
        for (int i = 1; i < code.Length; i++)
            if (!char.IsDigit(code[i])) return false;
        return code.Length > 1;
    }

    static readonly StringBuilder number = new StringBuilder();

    static void Tokenise(string code, NC_Block block, NC_Program p)
    {
        int i = 0;
        while (i < code.Length)
        {
            char c = code[i];
            if (char.IsWhiteSpace(c)) { i++; continue; }
            if (!char.IsLetter(c))
            {
                p.Notices.Add(new NC_Notice(NC_Notice.Level.Warning, block.Line, "unexpected '" + c + "' skipped"));
                i++;
                continue;
            }
            char letter = char.ToUpperInvariant(c);
            i++;
            number.Clear();
            while (i < code.Length && char.IsWhiteSpace(code[i])) i++;
            if (i < code.Length && (code[i] == '+' || code[i] == '-')) number.Append(code[i++]);
            while (i < code.Length && (char.IsDigit(code[i]) || code[i] == '.')) number.Append(code[i++]);
            if (number.Length == 0 || !float.TryParse(number.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
            {
                p.Notices.Add(new NC_Notice(NC_Notice.Level.Warning, block.Line, "word " + letter + " has no number and was skipped"));
                continue;
            }
            block.Words.Add(new NC_Word(letter, value));
        }
    }
}
