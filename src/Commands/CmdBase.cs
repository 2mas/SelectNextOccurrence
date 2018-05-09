using System;
using Microsoft.VisualStudio.Text.Editor;

namespace SelectNextOccurrence.Commands
{
    internal abstract class CmdBase
    {
        internal AdornmentLayer adornmentLayer;

        internal IWpfTextView view;

        protected CmdBase(IWpfTextView view)
        {
            this.view = view;
            this.adornmentLayer = view.Properties
                .GetProperty<AdornmentLayer>(typeof(AdornmentLayer));
        }

        internal abstract void OnCommandInvoked(object sender, EventArgs e);
    }
}
