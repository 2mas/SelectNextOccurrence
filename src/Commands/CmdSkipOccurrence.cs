using System;
using System.Linq;
using Microsoft.VisualStudio.Text.Editor;

namespace SelectNextOccurrence.Commands
{
    internal class CmdSkipOccurrence : CmdBase
    {
        public CmdSkipOccurrence(IWpfTextView view) : base(view) { }

        /// <summary>
        /// Menu-command handler for skipping one occurrence
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal override void OnCommandInvoked(object sender, EventArgs e)
        {
            if (!view.HasAggregateFocus)
                return;

            adornmentLayer.Selector.SelectNextOccurrence();

            if (adornmentLayer.Selector.Selections.Count > 1)
                adornmentLayer.Selector.Selections.RemoveAt(adornmentLayer.Selector.Selections.Count - 2);

            if (adornmentLayer.Selector.Selections.Any())
                adornmentLayer.DrawAdornments();
        }
    }
}
