using System.Collections.Generic;

/// <summary>One address letter with its number, e.g. G01 or X12.5.</summary>
public struct NC_Word
{
    public char Letter;
    public float Value;

    public NC_Word(char letter, float value)
    {
        Letter = letter;
        Value = value;
    }

    public override string ToString() => Letter + Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
}

/// <summary>One executable line of a program: its words in source order, minus comments.</summary>
public sealed class NC_Block
{
    public int Index;                                  // position among code blocks, 0-based
    public int Line;                                   // source line, 1-based
    public string Text;                                // trimmed source without the comment
    public readonly List<NC_Word> Words = new List<NC_Word>();

    public bool Has(char letter)
    {
        for (int i = 0; i < Words.Count; i++)
            if (Words[i].Letter == letter) return true;
        return false;
    }

    /// <summary>First value of the given letter.</summary>
    public bool Try_Get(char letter, out float value)
    {
        for (int i = 0; i < Words.Count; i++)
        {
            if (Words[i].Letter != letter) continue;
            value = Words[i].Value;
            return true;
        }
        value = 0f;
        return false;
    }
}

public struct NC_Notice
{
    public enum Level { Info, Warning, Alarm }

    public Level Severity;
    public int Line;
    public string Message;

    public NC_Notice(Level severity, int line, string message)
    {
        Severity = severity;
        Line = line;
        Message = message;
    }

    public override string ToString() => Severity + " line " + Line + ": " + Message;
}

/// <summary>A parsed program: header key/values from the leading comment lines, then the code blocks.</summary>
public sealed class NC_Program
{
    public string Name;
    public readonly Dictionary<string, string> Header = new Dictionary<string, string>();
    public readonly List<NC_Block> Blocks = new List<NC_Block>();
    public readonly List<NC_Notice> Notices = new List<NC_Notice>();

    public string Category => Header.TryGetValue("CATEGORY", out var c) ? c : "Uncategorized";
}
