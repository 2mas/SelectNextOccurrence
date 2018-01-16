using System.ComponentModel.Composition;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;

namespace NextOccurrence
{
    /// <summary>
    /// Establishes an <see cref="IAdornmentLayer"/> to place the adornment on and exports the <see cref="IWpfTextViewCreationListener"/>
    /// that instantiates the adornment on the event of a <see cref="IWpfTextView"/>'s creation
    /// </summary>
    [Export(typeof(IWpfTextViewCreationListener))]
    [ContentType("text")]
    [TextViewRole(PredefinedTextViewRoles.Editable)]
    internal sealed class NextOccurrenceAdornmentTextViewCreationListener : IWpfTextViewCreationListener
    {
        // Disable "Field is never assigned to..." and "Field is never used" compiler's warnings. Justification: the field is used by MEF.
#pragma warning disable 649, 169

        /// <summary>
        /// Defines the adornment layer for the adornment. This layer is ordered
        /// after the selection layer in the Z-order
        /// </summary>
        [Export(typeof(AdornmentLayerDefinition))]
        [Name("NextOccurrenceAdornment")]
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

        /// <summary>
        /// Called when a text view having matching roles is created over a text data model having a matching content type.
        /// Instantiates a NextOccurrenceAdornment manager when the textView is created.
        /// </summary>
        /// <param name="textView">The <see cref="IWpfTextView"/> upon which the adornment should be placed</param>
        public void TextViewCreated(IWpfTextView textView)
        {
            // The adornment will listen to any event that changes the layout (text changes, scrolling, etc)
            AddCommandFilter(
                textView,
                new NextOccurrenceAdornment(
                    textView,
                    textSearchService,
                    editorOperations,
                    formatMapService,
                    navigatorSelector.GetTextStructureNavigator(textView.TextBuffer)
                )
            );
        }

        void AddCommandFilter(IWpfTextView textView, NextOccurrenceAdornment commandFilter)
        {
            IOleCommandTarget next;
            IVsTextView view = editorFactory.GetViewAdapter(textView);

            if (view.AddCommandFilter(commandFilter, out next) == VSConstants.S_OK)
            {
                // Needed for MouseProcessor
                textView.Properties.AddProperty(typeof(NextOccurrenceAdornment), commandFilter);

                if (next != null)
                    commandFilter.NextCommandTarget = next;
            }
        }

        #endregion
    }
}
