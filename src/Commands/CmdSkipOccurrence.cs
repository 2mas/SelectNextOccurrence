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
            if (!View.HasAggregateFocus)
                return;

            AdornmentLayer.Selector.SelectNextOccurrence();

            if (AdornmentLayer.Selector.Selections.Count > 1)
                AdornmentLayer.Selector.Selections.RemoveAt(AdornmentLayer.Selector.Selections.Count - 2);

            if (AdornmentLayer.Selector.Selections.Any())
                AdornmentLayer.DrawAdornments();
        }
    }
}
