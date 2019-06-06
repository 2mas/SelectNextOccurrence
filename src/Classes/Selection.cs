using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

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

        internal int VirtualSpaces { get; set; }

        internal VirtualSnapshotPoint GetVirtualPoint(ITextSnapshot snapshot)
        {
            return new VirtualSnapshotPoint(Caret.GetPoint(snapshot), VirtualSpaces);
        }

        internal SnapshotSpan GetSpan(ITextSnapshot snapshot)
        {
            return new SnapshotSpan(Start.GetPoint(snapshot), End.GetPoint(snapshot));
        }

        internal bool OverlapsWith(SnapshotSpan span, ITextSnapshot snapshot)
        {
            if (IsSelection())
            {
                return span.OverlapsWith(GetSpan(snapshot));
            }
            else
            {
                return span.OverlapsWith(new SnapshotSpan(Caret.GetPoint(snapshot), 1));
            }
        }

        internal bool IsSelection()
        {
            return Start != null && End != null;
        }

        internal bool IsReversed(ITextSnapshot snapshot)
        {
            return Caret.GetPosition(snapshot) == Start?.GetPosition(snapshot);
        }

        internal void SetCaretPosition(CaretPosition caretPosition, bool verticalMove, ITextSnapshot snapshot)
        {
            var position = caretPosition.BufferPosition.Position;

            if (verticalMove)
            {
                position = GetCaretColumnPosition(position, snapshot);
            }
            else
            {
                ColumnPosition = position - snapshot.GetLineFromPosition(position).Start.Position;
            }

            Caret = snapshot.CreateTrackingPoint(position);
            VirtualSpaces = caretPosition.VirtualSpaces;
        }

        internal void UpdateSelection(int previousCaretPosition, ITextSnapshot snapshot)
        {
            var caretPosition = Caret.GetPosition(snapshot);

            if (IsSelection())
            {
                var startPosition = Start.GetPosition(snapshot);
                var endPosition = End.GetPosition(snapshot);
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
                else if (caretPosition >= startPosition && startPosition != previousCaretPosition)
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
                    snapshot.CreateTrackingPoint(previousCaretPosition)
                    : Caret;

                End = caretPosition > previousCaretPosition ?
                    Caret
                    : snapshot.CreateTrackingPoint(previousCaretPosition);
            }
            if (Start.GetPosition(snapshot) == End.GetPosition(snapshot))
            {
                Start = null;
                End = null;
            }
        }

        internal void SetSelection(VirtualSnapshotSpan newSpan, ITextSnapshot snapshot)
        {
            Start = snapshot.CreateTrackingPoint(
                newSpan.Start.Position.Position > newSpan.End.Position.Position ?
                newSpan.End.Position.Position
                : newSpan.Start.Position.Position
            );

            End = snapshot.CreateTrackingPoint(
                newSpan.Start.Position.Position > newSpan.End.Position.Position ?
                newSpan.Start.Position.Position
                : newSpan.End.Position.Position
            );
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
            else if (ColumnPosition > ( caretPosition - caretLine.Start.Position ))
            {
                var correctColumnPosition = ( ColumnPosition > caretLine.Length ) ?
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
