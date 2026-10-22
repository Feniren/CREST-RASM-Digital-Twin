using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

public class NC_Program_Library_Tests
{
    // The shipped demo programs cut the 100 x 50 x 100 mm NC_Stock block from its front-left-top corner.
    const float Stock_Mm = 100f;
    const float Max_Depth_Mm = 25f;     // cuts stay in the top half
    const float Margin_Mm = 6f;         // a tool radius of clearance outside the block

    static string Shipped_Dir => Path.Combine(Application.dataPath, "StreamingAssets/NC");

    [Test]
    public void Missing_Folder_Loads_Nothing()
    {
        Assert.That(NC_Program_Library.Load_All(Path.Combine(Shipped_Dir, "does_not_exist"), new NC_Machine_Settings()), Is.Empty);
    }

    [Test]
    public void Shipped_Programs_Plan_Cleanly()
    {
        var jobs = NC_Program_Library.Load_All(Shipped_Dir, new NC_Machine_Settings());
        Assert.That(jobs.Count, Is.GreaterThanOrEqualTo(5));
        Assert.That(jobs.Select(j => j.Name), Is.Ordered, "run order is file-name order");
        var categories = new HashSet<string>();
        foreach (var job in jobs)
        {
            var plan = job.Plan;
            Assert.That(job.Program.Notices, Is.Empty, job.Name);
            Assert.That(plan.Notices, Is.Empty, job.Name + " " + string.Join("; ", plan.Notices));
            Assert.That(plan.Has_Alarm, Is.False, job.Name);
            Assert.That(job.Warning_Count, Is.EqualTo(0), job.Name);
            Assert.That(plan.Cut_Mm, Is.GreaterThan(0f), job.Name);
            Assert.That(plan.Moves[plan.Moves.Count - 1].Kind, Is.EqualTo(NC_Move_Kind.End), job.Name);
            Assert.That(plan.Moves.Where(m => m.Is_Motion).Min(m => m.To.z), Is.GreaterThanOrEqualTo(-Max_Depth_Mm), job.Name + ": cuts stay in the top half of the stock");
            Assert.That(plan.Moves.Where(m => m.Is_Motion).All(m => m.To.x >= -Margin_Mm && m.To.x <= Stock_Mm + Margin_Mm && m.To.y >= -Margin_Mm && m.To.y <= Stock_Mm + Margin_Mm),
                        job.Name + ": stays over the stock");
            categories.Add(job.Program.Category);
        }
        Assert.That(categories.Count, Is.EqualTo(jobs.Count), "every shipped program declares its own category");

        var multi = jobs.First(j => j.Name == "05_multi_tool.nc");
        Assert.That(multi.Plan.Tools_Used, Is.EqualTo(new[] { 1, 3, 4 }));
        Assert.That(multi.Plan.Moves.Count(m => m.Kind == NC_Move_Kind.Tool_Change), Is.EqualTo(3));
    }
}
