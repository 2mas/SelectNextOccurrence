using System;
using System.Linq;
using Microsoft.VisualStudio.Text.Editor;

namespace NextOccurrence.Commands
{
    internal class CmdSelectAllOccurrences : CmdBase
    {
        public CmdSelectAllOccurrences(IWpfTextView view) : base(view) { }

        /// <summary>
        /// Menu-command handler, aka Ctrl+D
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal override void OnCommandInvoked(object sender, EventArgs e)
        {
            if (!view.HasAggregateFocus)
                return;

            adornmentLayer.Selector.SelectAllOccurrences();

            if (adornmentLayer.Selector.Selections.Any())
                adornmentLayer.DrawAdornments();

            adornmentLayer.Selector.IsReversing = false;
        }
    }
}
