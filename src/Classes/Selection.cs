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

        internal bool IsReversed()
        {
            return IsSelection() && Start == Caret;
        }

        internal bool Reversing(ITextSnapshot snapshot)
        {
            return Caret.GetPosition(snapshot) < End?.GetPosition(snapshot);
        }

        internal void SetSelection(int previousCaretPosition, ITextSnapshot Snapshot)
        {
            var caretPosition = Caret.GetPosition(Snapshot);

            if (IsSelection())
            {
                var startPosition = Start.GetPosition(Snapshot);
                var endPosition = End.GetPosition(Snapshot);

                if (caretPosition < startPosition && startPosition < previousCaretPosition)
                {
                    End = Start;
                    Start = Caret;
                }
                else if (previousCaretPosition < endPosition && endPosition < caretPosition)
                {
                    Start = End;
                    End = Caret;
                }
                else if (caretPosition > startPosition && startPosition != previousCaretPosition)
                {
                    End = Caret;
                }
                else
                {
                    Start = Caret;
                }
            }
            else
            {
                Start = caretPosition > previousCaretPosition ?
                    Snapshot.CreateTrackingPoint(previousCaretPosition, PointTrackingMode.Positive)
                    : Caret;

                End = caretPosition > previousCaretPosition ?
                    Caret
                    : Snapshot.CreateTrackingPoint(previousCaretPosition, PointTrackingMode.Positive);
            }
        }
    }
}
