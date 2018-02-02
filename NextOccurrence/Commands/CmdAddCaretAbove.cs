﻿using System;
using Microsoft.VisualStudio.Text.Editor;

namespace NextOccurrence.Commands
{
    internal class CmdAddCaretAbove : CmdBase
    {
        public CmdAddCaretAbove(IWpfTextView view) : base(view) { }

        /// <summary>
        /// Menu-command handler, adds a new caret one line above the active caret
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal override void OnCommandInvoked(object sender, EventArgs e)
        {
            if (!view.HasAggregateFocus)
                return;
        }
    }
}
