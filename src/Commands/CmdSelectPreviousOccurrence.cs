using System;
using System.Linq;
using Microsoft.VisualStudio.Text.Editor;

namespace SelectNextOccurrence.Commands
{
    internal class CmdSelectPreviousOccurrence : CmdBase
    {
        public CmdSelectPreviousOccurrence(IWpfTextView view) : base(view) { }

        /// <summary>
        /// Menu-command handler, selects a previous occurrence
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal override void OnCommandInvoked(object sender, EventArgs e)
        {
            if (!view.HasAggregateFocus)
                return;

            adornmentLayer.Selector.SelectNextOccurrence(reverseDirection: true);

            if (adornmentLayer.Selector.Selections.Any())
                adornmentLayer.DrawAdornments();
        }
    }
}
