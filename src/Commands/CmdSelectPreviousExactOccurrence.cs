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
            if (!view.HasAggregateFocus)
                return;

            adornmentLayer.Selector.SelectNextOccurrence(reverseDirection: true, exactMatch: true);

            if (adornmentLayer.Selector.Selections.Any())
                adornmentLayer.DrawAdornments();
        }
    }
}
