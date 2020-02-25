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
                var caretPoint = Caret.GetPoint(snapshot);
                return span.OverlapsWith(new SnapshotSpan(caretPoint, snapshot.Length > caretPoint.Position ? 1 : 0));
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
        /// Gets the correct caret column position. If the caret is positioned left
        /// of the stored column position the caret is set to the stored column position.
        /// </summary>
        /// <param name="caretPosition"></param>
        /// <param name="snapshot"></param>
        /// <returns></returns>
        internal int GetCaretColumnPosition(int caretPosition, ITextSnapshot snapshot, int tabSize)
        {
            var caretLine = snapshot.GetLineFromPosition(caretPosition);
            return (ColumnPosition > ( caretPosition - caretLine.Start.Position ))
                ? caretLine.Start.Position + ColumnOffset(caretLine.GetText(), tabSize)
                : caretPosition;
        }

        private int ColumnOffset(string lineText, int tabSize)
        {
            var caretOffset = 0;
            if (lineText.Length != 0)
            {
                var yPosition = 0;
                do
                {
                    yPosition += lineText[caretOffset] == '\t' ? tabSize : 1;
                    caretOffset++;
                }
                while (yPosition < ColumnPosition && caretOffset < lineText.Length);
            }

            return caretOffset;
        }
    }
}
