using NUnit.Framework;

// Edit Mode tests for Module 1's pure logic (Window > General > Test Runner
// > EditMode). No scene, rack or Play mode needed.
public class ASRS_Module1_Logic_Tests
{
    // ------------------------------------------------------------ addressing

    [Test]
    public void Addressing_CoversAll72CellsOnce()
    {
        var seen = new System.Collections.Generic.HashSet<string>();
        foreach (string id in ASRS_Rack_Addressing.AllTableIds())
            Assert.IsTrue(seen.Add(id), $"duplicate {id}");

        Assert.AreEqual(72, seen.Count);
        Assert.IsTrue(seen.Contains("010001"));
        Assert.IsTrue(seen.Contains("060006"));
        Assert.IsTrue(seen.Contains("070001"));
        Assert.IsTrue(seen.Contains("120006"));
    }

    [Test]
    public void Addressing_IndexRoundTrips()
    {
        for (int i = 0; i < ASRS_Rack_Addressing.CellCount; i++)
        {
            string id = ASRS_Rack_Addressing.FromIndex(i);
            Assert.AreEqual(i, ASRS_Rack_Addressing.ToIndex(int.Parse(id)), id);
        }
    }

    [Test]
    public void Addressing_MatchesArmTesterConvention()
    {
        // ASRSArmTester.IndexToTableId takes a 1-based index (1-72).
        for (int i = 0; i < ASRS_Rack_Addressing.CellCount; i++)
            Assert.AreEqual(ASRSArmTester.IndexToTableId(i + 1), ASRS_Rack_Addressing.FromIndex(i), $"index {i}");
    }

    [TestCase("010001", 1, 1)]
    [TestCase("070003", 7, 3)]
    [TestCase("120006", 12, 6)]
    public void Addressing_ParsesValidIds(string id, int row, int col)
    {
        Assert.IsTrue(ASRS_Rack_Addressing.TryParse(id, out int r, out int c));
        Assert.AreEqual(row, r);
        Assert.AreEqual(col, c);
    }

    [TestCase("")]
    [TestCase("abc")]
    [TestCase("000001")]
    [TestCase("130001")]
    [TestCase("010000")]
    [TestCase("010007")]
    public void Addressing_RejectsInvalidIds(string id)
    {
        Assert.IsFalse(ASRS_Rack_Addressing.TryParse(id, out _, out _));
        Assert.AreEqual(-1, ASRS_Rack_Addressing.ToIndex(int.TryParse(id, out int raw) ? raw : 0));
    }

    [Test]
    public void Addressing_SidesSplitAtRowSix()
    {
        StringAssert.StartsWith("Side A", ASRS_Rack_Addressing.SideName(6));
        StringAssert.StartsWith("Side B", ASRS_Rack_Addressing.SideName(7));
    }

    // ------------------------------------------------------------------ quiz

    private static ASRS_Quiz_Session Run(params bool[] answers)
    {
        var session = new ASRS_Quiz_Session(3, 2);
        foreach (bool correct in answers)
        {
            Assert.IsTrue(session.Submit(correct));
            Assert.IsTrue(session.Advance());
        }
        return session;
    }

    [TestCase(false, false, false, 0, false)]
    [TestCase(true, false, false, 1, false)]
    [TestCase(false, true, true, 2, true)]
    [TestCase(true, true, true, 3, true)]
    public void Quiz_ScoresAndPassMark(bool a, bool b, bool c, int score, bool passed)
    {
        ASRS_Quiz_Session session = Run(a, b, c);
        Assert.IsTrue(session.IsFinished);
        Assert.AreEqual(score, session.Score);
        Assert.AreEqual(passed, session.Passed);
    }

    [Test]
    public void Quiz_SecondSubmitOnSameQuestionIsIgnored()
    {
        var session = new ASRS_Quiz_Session(3, 2);
        Assert.IsTrue(session.Submit(true));
        Assert.IsFalse(session.Submit(true));
        Assert.AreEqual(1, session.Score);
    }

    [Test]
    public void Quiz_CannotAdvanceWithoutAnswering()
    {
        var session = new ASRS_Quiz_Session(3, 2);
        Assert.IsFalse(session.Advance());
        Assert.AreEqual(0, session.CurrentIndex);
    }

    [Test]
    public void Quiz_NotPassedUntilFinished()
    {
        var session = new ASRS_Quiz_Session(3, 2);
        session.Submit(true); session.Advance();
        session.Submit(true);
        Assert.IsFalse(session.Passed, "2 correct but question 3 still open");
    }

    [Test]
    public void Quiz_NoAnswersAfterFinish()
    {
        ASRS_Quiz_Session session = Run(true, true, true);
        Assert.IsFalse(session.Submit(true));
        Assert.IsFalse(session.Advance());
        Assert.AreEqual(3, session.Score);
    }

    [Test]
    public void Quiz_ResetClearsEverything()
    {
        ASRS_Quiz_Session session = Run(false, false, true);
        session.Reset();
        Assert.AreEqual(0, session.Score);
        Assert.AreEqual(0, session.CurrentIndex);
        Assert.IsFalse(session.CurrentAnswered);
        Assert.IsFalse(session.IsFinished);
        Assert.IsFalse(session.Passed);
    }
}
