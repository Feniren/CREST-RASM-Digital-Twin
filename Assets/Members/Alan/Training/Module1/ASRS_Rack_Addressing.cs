using System.Collections.Generic;

// Pure address math for the 72-slot Unity rack — no UnityEngine types, so
// it can be unit tested outside Play mode. Mirrors the conventions already
// used by Item_ASRS.GetIndex() and ASRSArmTester.IndexToTableId():
//   TableID  = row * 10000 + col, written as six digits (e.g. 010003)
//   rows 1-6  = Side A (SlotsA, the non-U side), 36 cells
//   rows 7-12 = Side B (SlotsB, the U side), 36 cells
//   index     = (row - 1) * 6 + (col - 1), 0-71; shown to trainees as 1-72
public static class ASRS_Rack_Addressing
{
    public const int Rows = 12;
    public const int Cols = 6;
    public const int CellCount = Rows * Cols;
    public const int RowsPerSide = 6;

    // Item_ASRS.Start() treats this row as empty on load (isVacantRow).
    public const int VacantRow = 5;

    public static bool TryParse(string tableId, out int row, out int col)
    {
        row = col = 0;
        if (string.IsNullOrWhiteSpace(tableId) || !int.TryParse(tableId, out int raw))
            return false;

        return TrySplit(raw, out row, out col);
    }

    public static bool TrySplit(int rawTableId, out int row, out int col)
    {
        row = rawTableId / 10000;
        col = rawTableId % 10000;
        return row >= 1 && row <= Rows && col >= 1 && col <= Cols;
    }

    public static int ToRawId(int row, int col) => row * 10000 + col;

    public static string Format(int row, int col) => ToRawId(row, col).ToString("D6");

    // 0-71, matching Item_ASRS.GetIndex() for valid ids; -1 when invalid.
    public static int ToIndex(int rawTableId)
    {
        return TrySplit(rawTableId, out int row, out int col) ? (row - 1) * Cols + (col - 1) : -1;
    }

    public static string FromIndex(int zeroBasedIndex)
    {
        if (zeroBasedIndex < 0 || zeroBasedIndex >= CellCount)
            return null;

        return Format(zeroBasedIndex / Cols + 1, zeroBasedIndex % Cols + 1);
    }

    public static string SideName(int row) => row <= RowsPerSide ? "Side A (rows 1-6)" : "Side B (rows 7-12)";

    public static IEnumerable<string> AllTableIds()
    {
        for (int i = 0; i < CellCount; i++)
            yield return FromIndex(i);
    }
}
