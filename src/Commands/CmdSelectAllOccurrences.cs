﻿using System;
using System.Linq;
using Microsoft.VisualStudio.Text.Editor;

namespace SelectNextOccurrence.Commands
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
            if (!View.HasAggregateFocus)
                return;

            AdornmentLayer.Selector.SelectAllOccurrences();

            if (AdornmentLayer.Selector.Selections.Any())
                AdornmentLayer.DrawAdornments();
        }
    }
}
