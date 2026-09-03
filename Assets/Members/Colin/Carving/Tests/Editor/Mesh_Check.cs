using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>Closed-surface check for flat-shaded meshes built from unshared vertices.</summary>
public static class Mesh_Check
{
    /// <summary>
    /// Asserts every directed edge appears exactly once and has a twin running the other way:
    /// a closed surface with consistent orientation and no T-junctions. Zero-length edges from
    /// crossings that coincide with a corner are ignored.
    /// </summary>
    public static void Assert_Watertight(List<Vector3> verts, List<int> tris, string label)
    {
        var edges = new Dictionary<(long, long, long, long, long, long), int>();
        for (int i = 0; i < tris.Count; i += 3)
        {
            var a = Key(verts[tris[i]]);
            var b = Key(verts[tris[i + 1]]);
            var c = Key(verts[tris[i + 2]]);
            Add(edges, a, b);
            Add(edges, b, c);
            Add(edges, c, a);
        }

        int unmatched = 0, duplicated = 0;
        foreach (var kv in edges)
        {
            if (kv.Value != 1) duplicated++;
            var k = kv.Key;
            if (!edges.ContainsKey((k.Item4, k.Item5, k.Item6, k.Item1, k.Item2, k.Item3))) unmatched++;
        }

        Assert.That(tris.Count, Is.GreaterThan(0), label + ": no triangles");
        Assert.That(unmatched, Is.EqualTo(0), label + ": boundary edges without a twin");
        Assert.That(duplicated, Is.EqualTo(0), label + ": directed edges used more than once");
    }

    static void Add(Dictionary<(long, long, long, long, long, long), int> edges, (long, long, long) a, (long, long, long) b)
    {
        if (a.Equals(b)) return;
        var key = (a.Item1, a.Item2, a.Item3, b.Item1, b.Item2, b.Item3);
        edges.TryGetValue(key, out int n);
        edges[key] = n + 1;
    }

    static (long, long, long) Key(Vector3 v)
    {
        return ((long)Math.Round(v.x * 1e7), (long)Math.Round(v.y * 1e7), (long)Math.Round(v.z * 1e7));
    }
}
