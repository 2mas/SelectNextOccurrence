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
            if (!View.HasAggregateFocus)
                return;

            if (AdornmentLayer.Selector.Selections.Count > 1)
            {
                AdornmentLayer.Selector.Selections.RemoveAt(AdornmentLayer.Selector.Selections.Count - 1);

                if (AdornmentLayer.Selector.Selections.Count == 1)
                    AdornmentLayer.Selector.HasWrappedDocument = false;
            }

            if (AdornmentLayer.Selector.Selections.Any())
            {
                AdornmentLayer.DrawAdornments();

                View.Caret.MoveTo(AdornmentLayer.Selector.Selections.Last().Caret.GetPoint(AdornmentLayer.Snapshot));
                View.ViewScroller.EnsureSpanVisible(
                    new SnapshotSpan(
                        View.Caret.Position.BufferPosition,
                        0
                    )
                );
            }
        }
    }
}
