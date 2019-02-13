using System;
using System.Linq;
using Microsoft.VisualStudio.Text.Editor;

namespace SelectNextOccurrence.Commands
{
    internal class CmdSelectPreviousExactOccurrence : CmdBase
    {
        public CmdSelectPreviousExactOccurrence(IWpfTextView view) : base(view) { }

        /// <summary>
        /// Menu-command handler, selects previous exact occurrence
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal override void OnCommandInvoked(object sender, EventArgs e)
        {
            if (!View.HasAggregateFocus)
                return;

            AdornmentLayer.Selector.SelectNextOccurrence(reverseDirection: true, exactMatch: true);

            if (AdornmentLayer.Selector.Selections.Any())
                AdornmentLayer.DrawAdornments();

            AdornmentLayer.Selector.IsReversing = false;
        }
    }
}
