using System;
using Microsoft.VisualStudio.Text.Editor;

namespace NextOccurrence.Commands
{
    internal abstract class CmdBase
    {
        internal NextOccurrenceAdornment adornmentLayer;

        internal IWpfTextView view;

        public CmdBase(IWpfTextView view)
        {
            this.view = view;
            this.adornmentLayer = view.Properties
                .GetProperty<NextOccurrenceAdornment>(typeof(NextOccurrenceAdornment));
        }

        internal abstract void OnCommandInvoked(object sender, EventArgs e);
    }
}
