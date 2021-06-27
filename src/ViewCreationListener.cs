using System.ComponentModel.Composition;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Text.Outlining;
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
#pragma warning disable IDE0051 // Remove unused private members
        private readonly AdornmentLayerDefinition editorAdornmentLayer;
#pragma warning restore IDE0051 // Remove unused private members

        [Import(typeof(IVsEditorAdaptersFactoryService))]
        internal IVsEditorAdaptersFactoryService EditorFactory = null;

        [Import(typeof(IEditorOperationsFactoryService))]
        internal IEditorOperationsFactoryService EditorOperations = null;

        [Import(typeof(ITextSearchService))]
        internal ITextSearchService TextSearchService = null;

        [Import(typeof(IEditorFormatMapService))]
        internal IEditorFormatMapService FormatMapService = null;

        [Import]
        internal IOutliningManagerService OutliningManagerService = null;

#pragma warning restore 649, 169

        public void VsTextViewCreated(IVsTextView textViewAdapter)
        {
            var textView = EditorFactory.GetWpfTextView(textViewAdapter);
            new AdornmentLayer(
                    textView,
                    TextSearchService,
                    EditorOperations,
                    FormatMapService,
                    OutliningManagerService
                );

            AddCommandFilter(
                textView,
                new CommandTarget(textView)
            );
        }

        private void AddCommandFilter(IWpfTextView textView, CommandTarget commandTarget)
        {
            if (EditorFactory != null)
            {
                var viewAdapter = EditorFactory.GetViewAdapter(textView);
                if (viewAdapter != null
                    && viewAdapter.AddCommandFilter(commandTarget, out var next) == VSConstants.S_OK
                    && next != null)
                {
                    commandTarget.NextCommandTarget = next;
                }
            }
        }
    }
}
