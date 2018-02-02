using System;
using System.Linq;
using Microsoft.VisualStudio.Text.Editor;

namespace NextOccurrence.Commands
{
    internal class CmdSelectNextOccurrence : CmdBase
    {
        public CmdSelectNextOccurrence(IWpfTextView view) : base(view) { }

        /// <summary>
        /// Menu-command handler, aka Ctrl+D
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal override void OnCommandInvoked(object sender, EventArgs e)
        {
            if (!view.HasAggregateFocus)
                return;

            adornmentLayer.Selector.SelectNextOccurrence();

            if (adornmentLayer.Selector.Selections.Any())
                adornmentLayer.DrawAdornments();

            adornmentLayer.Selector.IsReversing = false;
        }
    }
}
