using Microsoft.VisualStudio.Text;

namespace SelectNextOccurrence
{
    /// <summary>
    /// Represents a selection or/and a caret-position
    /// </summary>
    internal class Selection
    {
        internal ITrackingPoint Start { get; set; }
        internal ITrackingPoint End { get; set; }
        internal ITrackingPoint Caret { get; set; }
        internal string CopiedText { get; set; }

        internal bool OverlapsWith(SnapshotSpan span, ITextSnapshot snapshot)
        {
            if (!IsSelection())
            {
                return span.OverlapsWith(
                        new SnapshotSpan(Caret.GetPoint(snapshot), 1)
                    );
            }
            else
            {
                return new SnapshotSpan(
                        Start.GetPoint(snapshot), End.GetPoint(snapshot)
                    ).OverlapsWith(span);
            }
        }

        internal bool IsSelection()
        {
            return (Start != null && End != null);
        }

        internal bool Reversing(ITextSnapshot snapshot)
        {
            return Caret.GetPosition(snapshot) < End?.GetPosition(snapshot);
        }
    }
}
