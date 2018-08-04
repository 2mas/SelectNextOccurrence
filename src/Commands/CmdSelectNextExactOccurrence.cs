using System;
using System.Linq;
using Microsoft.VisualStudio.Text.Editor;

namespace SelectNextOccurrence.Commands
{
    internal class CmdSelectNextExactOccurrence : CmdBase
    {
        public CmdSelectNextExactOccurrence(IWpfTextView view) : base(view) { }

        /// <summary>
        /// Menu-command handler, aka Ctrl+D
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal override void OnCommandInvoked(object sender, EventArgs e)
        {
            if (!view.HasAggregateFocus)
                return;

            adornmentLayer.Selector.SelectNextExactOccurrence();

            if (adornmentLayer.Selector.Selections.Any())
                adornmentLayer.DrawAdornments();

            adornmentLayer.Selector.IsReversing = false;
        }
    }
}
