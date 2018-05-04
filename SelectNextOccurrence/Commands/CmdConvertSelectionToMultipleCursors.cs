using System;
using System.Linq;
using Microsoft.VisualStudio.Text.Editor;

namespace SelectNextOccurrence.Commands
{
    internal class CmdConvertSelectionToMultipleCursors : CmdBase
    {
        public CmdConvertSelectionToMultipleCursors(IWpfTextView view) : base(view) { }

        /// <summary>
        /// Menu-command handler, puts cursors at a selections line-ends
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal override void OnCommandInvoked(object sender, EventArgs e)
        {
            if (!view.HasAggregateFocus || view.Selection.IsEmpty)
                return;

            adornmentLayer.Selector.ConvertSelectionToMultipleCursors();

            if (adornmentLayer.Selector.Selections.Any())
                adornmentLayer.DrawAdornments();

            adornmentLayer.Selector.IsReversing = false;
        }
    }
}
