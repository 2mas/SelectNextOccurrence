using System;
using System.Linq;
using Microsoft.VisualStudio.Text.Editor;

namespace SelectNextOccurrence.Commands
{
    internal class CmdAddCaretBelow : CmdBase
    {
        public CmdAddCaretBelow(IWpfTextView view) : base(view) { }

        /// <summary>
        /// Menu-command handler, adds a new caret one line below the active caret
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal override void OnCommandInvoked(object sender, EventArgs e)
        {
            if (!View.HasAggregateFocus)
                return;

            if (!AdornmentLayer.Selector.Selections.Any())
            {
                // Add current caret
                AdornmentLayer.Selector.AddCurrentCaretToSelections();
            }

            AdornmentLayer.Selector.AddCaretBelow();
            AdornmentLayer.DrawAdornments();
        }
    }
}
