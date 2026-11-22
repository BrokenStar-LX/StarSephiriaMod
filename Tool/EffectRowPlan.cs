using System;
using System.Collections.Generic;

namespace SPMod;

// Dimensions use the owning RectTransform's units, never physical screen pixels.
internal static class EffectRowPlan
{
    internal readonly struct Box
    {
        internal readonly float X, Y, Width, Height;
        internal Box(float x, float y, float width, float height)
        { X = x; Y = y; Width = width; Height = height; }
    }

    internal static bool IsSpm(string? entityName) =>
        entityName != null && entityName.StartsWith("ModEffectHUD_", StringComparison.Ordinal);

    internal static Box[] Pack(IReadOnlyList<Box> sizes, float width, float gapX, float gapY,
        out float height)
    {
        if (!(width > 0) || float.IsInfinity(width))
            throw new ArgumentOutOfRangeException(nameof(width));
        var result = new Box[sizes.Count];
        float x = 0, y = 0, rowHeight = 0;
        for (int i = 0; i < sizes.Count; i++)
        {
            float w = sizes[i].Width, h = sizes[i].Height;
            if (!(w > 0) || !(h > 0) || float.IsInfinity(w) || float.IsInfinity(h))
                throw new ArgumentOutOfRangeException(nameof(sizes));
            // Keep an oversized icon legible rather than shrinking its counter.
            if (x > 0 && x + w > width + 0.01f)
            { x = 0; y += rowHeight + Math.Max(0, gapY); rowHeight = 0; }
            result[i] = new Box(x, y, w, h);
            x += w + Math.Max(0, gapX);
            rowHeight = Math.Max(rowHeight, h);
        }
        height = sizes.Count == 0 ? 0 : y + rowHeight;
        return result;
    }

    // Upward-positive Y. Reserve the icon/counter bounds against the whole party list.
    internal static float DownwardClearance(Box obstacle, Box party, float gap)
    {
        if (obstacle.Width <= 0 || obstacle.Height <= 0 || party.Width <= 0 || party.Height <= 0 ||
            obstacle.X + obstacle.Width <= party.X || party.X + party.Width <= obstacle.X)
            return 0;
        return Math.Max(0, party.Y + party.Height - obstacle.Y + Math.Max(0, gap));
    }
}
