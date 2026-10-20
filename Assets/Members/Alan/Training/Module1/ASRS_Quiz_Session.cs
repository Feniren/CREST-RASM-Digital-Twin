// Score keeping for a fixed-length quiz — no UnityEngine types, so it can be
// unit tested outside Play mode. One answer per question: Submit() locks the
// current question, Advance() only moves on once it has been answered, and
// Reset() clears everything for a retry.
public sealed class ASRS_Quiz_Session
{
    public int QuestionCount { get; }
    public int PassMark { get; }

    public int CurrentIndex { get; private set; }
    public int Score { get; private set; }
    public bool CurrentAnswered { get; private set; }
    public bool LastAnswerCorrect { get; private set; }

    public bool IsFinished => CurrentIndex >= QuestionCount;
    public bool Passed => IsFinished && Score >= PassMark;

    public ASRS_Quiz_Session(int questionCount, int passMark)
    {
        QuestionCount = questionCount < 0 ? 0 : questionCount;
        PassMark = passMark;
        Reset();
    }

    // False (and no change) if this question was already answered or the
    // quiz is over — guards against a double press scoring twice.
    public bool Submit(bool correct)
    {
        if (IsFinished || CurrentAnswered)
            return false;

        CurrentAnswered = true;
        LastAnswerCorrect = correct;
        if (correct)
            Score++;

        return true;
    }

    // False if the current question hasn't been answered yet.
    public bool Advance()
    {
        if (IsFinished || !CurrentAnswered)
            return false;

        CurrentIndex++;
        CurrentAnswered = false;
        LastAnswerCorrect = false;
        return true;
    }

    public void Reset()
    {
        CurrentIndex = 0;
        Score = 0;
        CurrentAnswered = false;
        LastAnswerCorrect = false;
    }
}
