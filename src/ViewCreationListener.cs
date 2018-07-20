using System.ComponentModel.Composition;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;
using SelectNextOccurrence.Commands;

namespace SelectNextOccurrence
{
    [Export(typeof(IVsTextViewCreationListener))]
    [ContentType("text")]
    [TextViewRole(PredefinedTextViewRoles.Editable)]
    internal sealed class ViewCreationListener : IVsTextViewCreationListener
    {
        // Disable "Field is never assigned to..." and "Field is never used" compiler's warnings. Justification: the field is used by MEF.
#pragma warning disable 649, 169
        [Export(typeof(AdornmentLayerDefinition))]
        [Name(Vsix.Name)]
        [Order(After = PredefinedAdornmentLayers.Selection, Before = PredefinedAdornmentLayers.Text)]
        private AdornmentLayerDefinition editorAdornmentLayer;

        [Import(typeof(IVsEditorAdaptersFactoryService))]
        internal IVsEditorAdaptersFactoryService editorFactory = null;

        [Import(typeof(IEditorOperationsFactoryService))]
        internal IEditorOperationsFactoryService editorOperations = null;

        [Import(typeof(ITextSearchService))]
        internal ITextSearchService textSearchService = null;

        [Import(typeof(IEditorFormatMapService))]
        internal IEditorFormatMapService formatMapService = null;

        [Import]
        internal ITextStructureNavigatorSelectorService navigatorSelector = null;

#pragma warning restore 649, 169

        #region IWpfTextViewCreationListener

        public void VsTextViewCreated(IVsTextView textViewAdapter)
        {
#pragma warning disable S1848 // Objects should not be created to be dropped immediately without being used
            var textView = editorFactory.GetWpfTextView(textViewAdapter);
            new AdornmentLayer(
                    textView,
                    textSearchService,
                    editorOperations,
                    formatMapService,
                    navigatorSelector.GetTextStructureNavigator(textView.TextBuffer)
                );
#pragma warning restore S1848 // Objects should not be created to be dropped immediately without being used

            AddCommandFilter(
                textView,
                new CommandTarget(textView)
            );
        }

        void AddCommandFilter(IWpfTextView textView, CommandTarget commandTarget)
        {
            IOleCommandTarget next;

            if (editorFactory != null)
            {
                IVsTextView viewAdapter = editorFactory.GetViewAdapter(textView);
                if (viewAdapter != null
                    && viewAdapter.AddCommandFilter(commandTarget, out next) == VSConstants.S_OK
                    && next != null)
                {
                    commandTarget.NextCommandTarget = next;
                }
            }
        }

        #endregion
    }
}
