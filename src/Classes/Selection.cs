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

        internal int ColumnPosition { get; set; }

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

        /// <summary>
        /// Gets the caret column position when moving caret vertically.
        /// If the Caret is already on first or last line the caret is set
        /// to the start of file or to the end of the file, respectively.
        /// If the Caret is positioned left off the stored column position
        /// the caret is set to the stored column position or the end of line.
        /// </summary>
        /// <param name="caretPosition"></param>
        /// <param name="snapshot"></param>
        /// <returns></returns>
        internal int GetCaretColumnPosition(int caretPosition, ITextSnapshot snapshot)
        {
            var previousLineNumber = snapshot.GetLineNumberFromPosition(Caret.GetPosition(snapshot));
            var caretLine = snapshot.GetLineFromPosition(caretPosition);

            if (caretLine.LineNumber == previousLineNumber && caretLine.LineNumber == 0)
            {
                return 0;
            }
            else if (caretLine.LineNumber == previousLineNumber
                && caretLine.LineNumber == snapshot.LineCount - 1)
            {
                return snapshot.Length;
            }
            else if (ColumnPosition > (caretPosition - caretLine.Start.Position))
            {
                var correctColumnPosition = (ColumnPosition > caretLine.Length) ?
                    caretLine.Length
                    : ColumnPosition;
                return caretLine.Start.Position + correctColumnPosition;
            }
            else
            {
                return caretPosition;
            }
        }
    }
}
