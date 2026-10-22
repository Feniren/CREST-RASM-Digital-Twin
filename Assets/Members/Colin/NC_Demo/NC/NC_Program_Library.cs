using System;
using System.Collections.Generic;
using System.IO;

/// <summary>One program ready to run: the parsed text and its plan for a given machine.</summary>
public sealed class NC_Job
{
    public string Name;
    public string File_Path;
    public NC_Program Program;
    public NC_Plan Plan;

    /// <summary>Notices an operator should see: Warning and Alarm (Info is bookkeeping).</summary>
    public int Warning_Count
    {
        get
        {
            int n = 0;
            foreach (var notice in Program.Notices) if (notice.Severity != NC_Notice.Level.Info) n++;
            foreach (var notice in Plan.Notices) if (notice.Severity != NC_Notice.Level.Info) n++;
            return n;
        }
    }
}

/// <summary>
/// Loads .nc programs from a folder. The only file IO in the NC code, so a build that cannot
/// read StreamingAssets synchronously (Android) has one place to replace.
/// </summary>
public static class NC_Program_Library
{
    public const string Pattern = "*.nc";

    /// <summary>Every program in the folder, sorted by file name; empty when the folder is missing.</summary>
    public static List<NC_Job> Load_All(string directory, NC_Machine_Settings settings)
    {
        var jobs = new List<NC_Job>();
        if (!Directory.Exists(directory)) return jobs;
        string[] files = Directory.GetFiles(directory, Pattern);
        Array.Sort(files, StringComparer.Ordinal);
        foreach (string file in files) jobs.Add(Load(file, settings));
        return jobs;
    }

    public static NC_Job Load(string path, NC_Machine_Settings settings)
    {
        var program = NC_Parser.Parse(Path.GetFileName(path), File.ReadAllText(path));
        return new NC_Job { Name = program.Name, File_Path = path, Program = program, Plan = NC_Planner.Plan(program, settings) };
    }
}
