using System;
using Microsoft.VisualStudio.Text.Editor;

namespace SelectNextOccurrence.Commands
{
    internal abstract class CmdBase
    {
        internal AdornmentLayer AdornmentLayer;

        internal IWpfTextView View;

        protected CmdBase(IWpfTextView view)
        {
            this.View = view;
            this.AdornmentLayer = view.Properties
                .GetProperty<AdornmentLayer>(typeof(AdornmentLayer));
        }

        internal abstract void OnCommandInvoked(object sender, EventArgs e);
    }
}
