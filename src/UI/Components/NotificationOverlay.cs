using Spectre.Console.Rendering;

namespace lazydotnet.UI.Components;

public sealed class NotificationOverlay(IRenderable background) : IRenderable
{
    private const int RightMargin = 2;
    private const int TopMargin = 1;

    public Measurement Measure(RenderOptions options, int maxWidth)
    {
        return background.Measure(options, maxWidth);
    }

    public IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
    {
        var backgroundSegments = background.Render(options, maxWidth).ToList();
        var notification = Notification.GetRenderable();

        if (notification == null) return backgroundSegments;

        var notificationSegments = notification.Render(options, maxWidth).ToList();
        var notificationLines = Segment.SplitLines(notificationSegments).ToList();

        if (notificationLines.Count == 0) return backgroundSegments;

        var notificationWidth = notificationLines.Max(l => l.Sum(s => s.CellCount()));

        var backgroundLines = Segment.SplitLines(backgroundSegments).ToList();
        var bgHeight = backgroundLines.Count;
        var notifHeight = notificationLines.Count;

        var result = new List<Segment>();

        for (var y = 0; y < bgHeight; y++)
        {
            if (y >= TopMargin && y < TopMargin + notifHeight)
            {
                var bgLine = backgroundLines[y];
                var bgWidth = bgLine.Sum(s => s.CellCount());

                var notifLine = notificationLines[y - TopMargin];
                var notifLineLen = notifLine.Sum(s => s.CellCount());

                var startX = Math.Max(0, bgWidth - notificationWidth - RightMargin);
                var prefix = TakeCells(bgLine, startX);
                var suffix = SkipCells(bgLine, startX + notifLineLen);

                result.AddRange(prefix);
                result.AddRange(notifLine);
                result.AddRange(suffix);
            }
            else
            {
                result.AddRange(backgroundLines[y]);
            }

            result.Add(Segment.LineBreak);
        }

        return result;
    }

    private static List<Segment> TakeCells(List<Segment> line, int count)
    {
        var result = new List<Segment>();
        var current = 0;
        foreach (var segment in line)
        {
            var remaining = count - current;
            if (remaining <= 0) break;

            if (segment.CellCount() <= remaining)
            {
                result.Add(segment);
                current += segment.CellCount();
            }
            else
            {
                var take = Math.Min(remaining, segment.Text.Length);
                result.Add(new Segment(segment.Text[..take], segment.Style));
                current += take;
            }
        }

        if (current < count)
        {
            result.Add(new Segment(new string(' ', count - current)));
        }

        return result;
    }

    private static List<Segment> SkipCells(List<Segment> line, int count)
    {
        var result = new List<Segment>();
        var current = 0;
        foreach (var segment in line)
        {
            var cellCount = segment.CellCount();
            if (current + cellCount <= count)
            {
                current += cellCount;
                continue;
            }

            var toSkip = count - current;
            if (toSkip > 0)
            {
                var skip = Math.Min(toSkip, segment.Text.Length);
                result.Add(new Segment(segment.Text[skip..], segment.Style));
                current += cellCount;
            }
            else
            {
                result.Add(segment);
            }
        }
        return result;
    }
}
