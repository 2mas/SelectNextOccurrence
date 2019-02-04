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

        /// <summary>
        /// Contains the copied/cut text of the current selection for use in the same document when pasting into the same active cursors.
        /// When pasting across documents the static <see cref="Selector.SavedClipboard"/> is used
        /// </summary>
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

        internal void SetNewSelection(int previousCaretPosition, ITextSnapshot Snapshot)
        {
            var caretPosition = Caret.GetPosition(Snapshot);
            if (IsSelection())
            {
                var startPos = Start.GetPosition(Snapshot);
                var endPos = End.GetPosition(Snapshot);
                if (caretPosition < startPos && startPos < previousCaretPosition)
                {
                    End = Start;
                    Start = Snapshot.CreateTrackingPoint(caretPosition, PointTrackingMode.Positive);
                }
                else if (previousCaretPosition < endPos && endPos < caretPosition)
                {
                    Start = End;
                    End = Snapshot.CreateTrackingPoint(caretPosition, PointTrackingMode.Positive);
                }
                else if (caretPosition > startPos && startPos != previousCaretPosition)
                {
                    End = Snapshot.CreateTrackingPoint(caretPosition, PointTrackingMode.Positive);
                }
                else
                {
                    Start = Snapshot.CreateTrackingPoint(caretPosition, PointTrackingMode.Positive);
                }
            }
            else
            {
                Start = Snapshot.CreateTrackingPoint(
                    caretPosition > previousCaretPosition ? previousCaretPosition : caretPosition,
                    PointTrackingMode.Positive
                );

                End = Snapshot.CreateTrackingPoint(
                    caretPosition > previousCaretPosition ? caretPosition : previousCaretPosition,
                    PointTrackingMode.Positive
                );
            }
        }
    }
}
