using System;
using System.Collections.Generic;

// Picks what one quiz attempt asks — no UnityEngine types, so it can be unit
// tested outside Play mode.
public static class ASRS_Quiz_Draw
{
    // Indices into the question pool for one attempt: up to `multipleChoice`
    // multiple-choice questions, chosen at random and in random order, then
    // up to `cell` rack-cell questions, always last (they send the trainee
    // over to the rack). Asks fewer if the pool doesn't have enough.
    public static int[] Draw(IList<bool> isCellQuestion, int multipleChoice, int cell, Random rng)
    {
        List<int> choice = new List<int>();
        List<int> cells = new List<int>();
        for (int i = 0; i < isCellQuestion.Count; i++)
            (isCellQuestion[i] ? cells : choice).Add(i);

        Shuffle(choice, rng);
        Shuffle(cells, rng);

        List<int> result = new List<int>();
        result.AddRange(choice.GetRange(0, Clamp(multipleChoice, choice.Count)));
        result.AddRange(cells.GetRange(0, Clamp(cell, cells.Count)));
        return result.ToArray();
    }

    // The given items in a random order (a new array; the input is untouched).
    public static int[] Shuffled(IList<int> items, Random rng)
    {
        int[] copy = new int[items.Count];
        items.CopyTo(copy, 0);
        Shuffle(copy, rng);
        return copy;
    }

    // Fisher–Yates: every order equally likely.
    public static void Shuffle<T>(IList<T> list, Random rng)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            T swap = list[i];
            list[i] = list[j];
            list[j] = swap;
        }
    }

    private static int Clamp(int wanted, int available) => Math.Max(0, Math.Min(wanted, available));
}
