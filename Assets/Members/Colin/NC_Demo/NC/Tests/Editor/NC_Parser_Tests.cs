using NUnit.Framework;

public class NC_Parser_Tests
{
    [Test]
    public void Words_Are_Letter_Number_Pairs_Case_Insensitive()
    {
        var p = NC_Parser.Parse("t", "N10 g01 X10.5 Y-2 F100");
        Assert.That(p.Blocks.Count, Is.EqualTo(1));
        var w = p.Blocks[0].Words;
        Assert.That(w.Count, Is.EqualTo(5));
        Assert.That(w[1].Letter, Is.EqualTo('G'));
        Assert.That(w[1].Value, Is.EqualTo(1f));
        Assert.That(w[2].Value, Is.EqualTo(10.5f));
        Assert.That(w[3].Value, Is.EqualTo(-2f));
        Assert.That(p.Notices, Is.Empty);
    }

    [Test]
    public void Reference_Block_Without_Spaces()
    {
        var p = NC_Parser.Parse("t", "N1G90G01X.5Y.5Z1.5F1");
        var b = p.Blocks[0];
        Assert.That(b.Words.Count, Is.EqualTo(7));   // N1 G90 G01 X.5 Y.5 Z1.5 F1
        Assert.That(b.Try_Get('X', out float x) && x == 0.5f);
        Assert.That(b.Try_Get('Z', out float z) && z == 1.5f);
        Assert.That(b.Has('G'));
    }

    [Test]
    public void Comments_Are_Stripped()
    {
        var p = NC_Parser.Parse("t", "G01 X5 ; move over\n(setup) G00 Z2 (retract)\nG01 X6 ; F999 in a comment");
        Assert.That(p.Blocks.Count, Is.EqualTo(3));
        Assert.That(p.Blocks[0].Text, Is.EqualTo("G01 X5"));
        Assert.That(p.Blocks[1].Words.Count, Is.EqualTo(2));
        Assert.That(p.Blocks[1].Words[1].Letter, Is.EqualTo('Z'));
        Assert.That(p.Blocks[2].Has('F'), Is.False);
    }

    [Test]
    public void Header_Comments_Fill_The_Header()
    {
        var p = NC_Parser.Parse("t", "; NAME: Test part\n; CATEGORY: Drilling\n; a free note: not a key\n;-----\nG00 X1\n; CATEGORY: Late");
        Assert.That(p.Category, Is.EqualTo("Drilling"));
        Assert.That(p.Header["NAME"], Is.EqualTo("Test part"));
        Assert.That(p.Header.Count, Is.EqualTo(2), "keys are single words; comments after the first block are ignored");
    }

    [Test]
    public void Skips_Markers_Labels_And_Block_Skip()
    {
        var p = NC_Parser.Parse("t", "%\nO1234\n/G01 X5\nG00 X1\n%\n");
        Assert.That(p.Blocks.Count, Is.EqualTo(1));
        Assert.That(p.Blocks[0].Text, Is.EqualTo("G00 X1"));
    }

    [Test]
    public void Malformed_Words_Raise_Warnings()
    {
        var p = NC_Parser.Parse("t", "G01 X\nG01 ? X5");
        Assert.That(p.Blocks[0].Words.Count, Is.EqualTo(1));
        Assert.That(p.Blocks[1].Words.Count, Is.EqualTo(2));
        Assert.That(p.Notices.Count, Is.EqualTo(2));
        Assert.That(p.Notices[0].Severity, Is.EqualTo(NC_Notice.Level.Warning));
        Assert.That(p.Notices[1].Line, Is.EqualTo(2));
    }

    [Test]
    public void Absolute_Arc_Centre_Marker_Warns()
    {
        var p = NC_Parser.Parse("t", "$\nG00 X1");
        Assert.That(p.Blocks.Count, Is.EqualTo(1));
        Assert.That(p.Notices.Count, Is.EqualTo(1));
    }

    [Test]
    public void Line_Numbers_Are_Source_Lines()
    {
        var p = NC_Parser.Parse("t", "\r\n; c\r\nG01 X1\r\n");
        Assert.That(p.Blocks[0].Line, Is.EqualTo(3));
        Assert.That(p.Blocks[0].Index, Is.EqualTo(0));
    }
}
