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
            if (!View.HasAggregateFocus || View.Selection.IsEmpty)
                return;

            AdornmentLayer.Selector.ConvertSelectionToMultipleCursors();

            if (AdornmentLayer.Selector.Selections.Any())
                AdornmentLayer.DrawAdornments();
        }
    }
}
