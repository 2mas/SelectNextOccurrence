using System;
using System.Linq;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace SelectNextOccurrence.Commands
{
    internal class CmdUndoOccurrence : CmdBase
    {
        public CmdUndoOccurrence(IWpfTextView view) : base(view) { }

        /// <summary>
        /// Menu-command handler, undo the last occurrence
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal override void OnCommandInvoked(object sender, EventArgs e)
        {
            if (!view.HasAggregateFocus)
                return;

            if (adornmentLayer.Selector.Selections.Count > 1)
                adornmentLayer.Selector.Selections.RemoveAt(adornmentLayer.Selector.Selections.Count - 1);

            if (adornmentLayer.Selector.Selections.Any())
            {
                adornmentLayer.DrawAdornments();

                view.Caret.MoveTo(adornmentLayer.Selector.Selections.Last().Caret.GetPoint(adornmentLayer.Snapshot));
                view.ViewScroller.EnsureSpanVisible(
                    new SnapshotSpan(
                        view.Caret.Position.BufferPosition,
                        0
                    )
                );
            }
        }
    }
}
