using System;
using System.Linq;
using System.Windows;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace SelectNextOccurrence.Commands
{
    /// <summary>
    /// Handles keyboard, typing and combinations in text-editor
    /// </summary>
    class CommandTarget : IOleCommandTarget
    {
        private enum ProcessOrder { Normal, TopToBottom, BottomToTop }

        private readonly IWpfTextView view;

        private ITextSnapshot Snapshot => this.view.TextSnapshot;

        private Selector Selector => this.adornmentLayer.Selector;

        private readonly AdornmentLayer adornmentLayer;

        public IOleCommandTarget NextCommandTarget { get; set; }

        public CommandTarget(IWpfTextView view)
        {
            this.view = view;
            this.adornmentLayer = view.Properties
                .GetProperty<AdornmentLayer>(typeof(AdornmentLayer));
        }

        public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            return NextCommandTarget.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
        }

        public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            // Return not supported when the command does nothing
            var result = unchecked((int)Constants.OLECMDERR_E_NOTSUPPORTED);

            if (pguidCmdGroup == VSConstants.VSStd2K)
            {
                switch ((VSConstants.VSStd2KCmdID) nCmdID)
                {
                    case VSConstants.VSStd2KCmdID.SolutionPlatform:
                        return result;
                }
            }

            if (pguidCmdGroup == VSConstants.GUID_VSStandardCommandSet97)
            {
                switch ((VSConstants.VSStd97CmdID) nCmdID)
                {
                    case VSConstants.VSStd97CmdID.SolutionCfg:
                        return result;
                }
            }

            result = VSConstants.S_OK;
            System.Diagnostics.Debug.WriteLine("grp: {0}, id: {1}", pguidCmdGroup.ToString(), nCmdID.ToString());

            if (!Selector.Selections.Any())
                return ProcessSingleCursor(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut, ref result);

            var modifySelections = false;
            var clearSelections = false;
            var verticalMove = false;
            var processOrder = ProcessOrder.Normal;
            var invokeCommand = false;

            if (pguidCmdGroup == typeof(VSConstants.VSStd2KCmdID).GUID
                || pguidCmdGroup == VSConstants.GUID_VSStandardCommandSet97
                || pguidCmdGroup == typeof(VSConstants.VSStd12CmdID).GUID
                || pguidCmdGroup == PackageGuids.guidNextOccurrenceCommandsPackageCmdSet)
            {
                if (pguidCmdGroup == VSConstants.GUID_VSStandardCommandSet97)
                {
                    switch (nCmdID)
                    {
                        case ((uint)VSConstants.VSStd97CmdID.Copy):
                        case ((uint)VSConstants.VSStd97CmdID.Cut):
                            return HandleMultiCopyCut(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
                        case ((uint)VSConstants.VSStd97CmdID.Paste):
                            // Only multi-paste different texts if all our selections have been copied with 
                            // this extension, otherwise paste as default. 
                            // Copied text get reset when new new selections are added
                            if (Selector.Selections.All(s => !string.IsNullOrEmpty(s.CopiedText)))
                            {
                                return HandleMultiPaste(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
                            }
                            break;
                        case ((uint)VSConstants.VSStd97CmdID.Undo):
                        case ((uint)VSConstants.VSStd97CmdID.Redo):
                            result = NextCommandTarget.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
                            adornmentLayer.DrawAdornments();
                            return result;
                    }
                }
                if (pguidCmdGroup == typeof(VSConstants.VSStd2KCmdID).GUID)
                {
                    switch (nCmdID)
                    {
                        case ((uint)VSConstants.VSStd2KCmdID.UP):
                        case ((uint)VSConstants.VSStd2KCmdID.DOWN):
                            verticalMove = true;
                            clearSelections = true;
                            break;
                        case ((uint)VSConstants.VSStd2KCmdID.LEFT):
                        case ((uint)VSConstants.VSStd2KCmdID.RIGHT):
                        case ((uint)VSConstants.VSStd2KCmdID.WORDPREV):
                        case ((uint)VSConstants.VSStd2KCmdID.WORDNEXT):
                            // Remove selected spans but keep carets
                            clearSelections = true;
                            break;
                        case ((uint)VSConstants.VSStd2KCmdID.CANCEL):
                            Selector.DiscardSelections();
                            break;
                        case ((uint)VSConstants.VSStd2KCmdID.PAGEDN):
                        case ((uint)VSConstants.VSStd2KCmdID.PAGEUP):
                        case ((uint)VSConstants.VSStd2KCmdID.END):
                        case ((uint)VSConstants.VSStd2KCmdID.HOME):
                        case ((uint)VSConstants.VSStd2KCmdID.END_EXT):
                        case ((uint)VSConstants.VSStd2KCmdID.HOME_EXT):
                            Selector.DiscardSelections();
                            result = NextCommandTarget.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
                            break;
                        case ((uint)VSConstants.VSStd2KCmdID.UP_EXT):
                        case ((uint)VSConstants.VSStd2KCmdID.DOWN_EXT):
                            verticalMove = true;
                            modifySelections = true;
                            break;
                        case ((uint)VSConstants.VSStd2KCmdID.WORDPREV_EXT):
                        case ((uint)VSConstants.VSStd2KCmdID.BOL_EXT):
                        case ((uint)VSConstants.VSStd2KCmdID.LEFT_EXT):
                        case ((uint)VSConstants.VSStd2KCmdID.WORDNEXT_EXT):
                        case ((uint)VSConstants.VSStd2KCmdID.EOL_EXT):
                        case ((uint)VSConstants.VSStd2KCmdID.RIGHT_EXT):
                            modifySelections = true;
                            break;
                        case ((uint)VSConstants.VSStd2KCmdID.SELLOWCASE):
                        case ((uint)VSConstants.VSStd2KCmdID.SELUPCASE):
                        case ((uint)VSConstants.VSStd2KCmdID.COMMENT_BLOCK):
                        case ((uint)VSConstants.VSStd2KCmdID.UNCOMMENT_BLOCK):
                            invokeCommand = true;
                            break;
                    }
                }

                if (pguidCmdGroup == typeof(VSConstants.VSStd12CmdID).GUID)
                {
                    switch (nCmdID)
                    {
                        case ((uint)VSConstants.VSStd12CmdID.MoveSelLinesUp):
                            invokeCommand = true;
                            processOrder = ProcessOrder.TopToBottom;
                            break;
                        case ((uint)VSConstants.VSStd12CmdID.MoveSelLinesDown):
                            invokeCommand = true;
                            processOrder = ProcessOrder.BottomToTop;
                            break;
                    }
                }

                if (pguidCmdGroup == PackageGuids.guidNextOccurrenceCommandsPackageCmdSet)
                {
                    verticalMove = nCmdID == PackageIds.AddCaretAboveCommandId
                        || nCmdID == PackageIds.AddCaretBelowCommandId;
                }
            }

            if (Selector.Selections.Any())
            {
                result = ProcessSelections(
                    modifySelections,
                    clearSelections,
                    verticalMove,
                    processOrder,
                    invokeCommand,
                    ref pguidCmdGroup,
                    nCmdID,
                    nCmdexecopt,
                    pvaIn,
                    pvaOut
                );
            }

            Selector.RemoveDuplicates();
            view.Selection.Clear();

            adornmentLayer.DrawAdornments();

            return result;
        }

        /// <summary>
        /// When no multiple selections are active, perform checks for multi-paste.
        /// Multi-paste gets active if its previously stored on selections that are now discarded
        /// and the current clipboards content equals the last stored clipboard-item
        /// </summary>
        /// <param name="pguidCmdGroup"></param>
        /// <param name="nCmdID"></param>
        /// <param name="nCmdexecopt"></param>
        /// <param name="pvaIn"></param>
        /// <param name="pvaOut"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        private int ProcessSingleCursor(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut, ref int result)
        {
            // if paste, see if we have a saved clipboard to apply
            if (pguidCmdGroup == VSConstants.GUID_VSStandardCommandSet97
                    && nCmdID == (uint)VSConstants.VSStd97CmdID.Paste
                    && Selector.SavedClipboard.Any())
            {
                // Clipboard saved, paste these on new lines if current clipboard does match the last item
                // If they dont match, a copy/cut has been made from somewhere else
                if (Clipboard.GetText() != Selector.SavedClipboard.Last())
                    Selector.ClearSavedClipboard();

                if (Selector.SavedClipboard.Count() > 1)
                {
                    var count = 1;
                    var clipboardCount = Selector.SavedClipboard.Count();

                    if (!Selector.Dte.UndoContext.IsOpen)
                        Selector.Dte.UndoContext.Open(Vsix.Name);

                    foreach (var clipboardText in Selector.SavedClipboard)
                    {
                        Clipboard.SetText(clipboardText);
                        result = NextCommandTarget.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);

                        if (count < clipboardCount)
                        {
                            Selector.EditorOperations.InsertNewLine();
                            count++;
                        }
                    }

                    if (Selector.Dte.UndoContext.IsOpen)
                        Selector.Dte.UndoContext.Close();

                    return result;
                }
                else
                {
                    return NextCommandTarget.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
                }
            }
            else
            {
                // if copy/cut, clear saved clipboard
                if (pguidCmdGroup == VSConstants.GUID_VSStandardCommandSet97
                            && (nCmdID == (uint)VSConstants.VSStd97CmdID.Copy
                                || (nCmdID == (uint)VSConstants.VSStd97CmdID.Cut)
                                )
                            )
                    Selector.ClearSavedClipboard();

                // continue normal processing
                return NextCommandTarget.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
            }
        }

        private int ProcessSelections(
            bool modifySelections,
            bool clearSelections,
            bool verticalMove,
            ProcessOrder processOrder,
            bool invokeCommand,
            ref Guid pguidCmdGroup,
            uint nCmdID,
            uint nCmdexecopt,
            IntPtr pvaIn,
            IntPtr pvaOut)
        {
            var result = VSConstants.S_OK;

            if (!Selector.Dte.UndoContext.IsOpen)
                Selector.Dte.UndoContext.Open(Vsix.Name);

            // Contains the same selection-elements but possibly re-ordered
            // Selector keeps original order to support undo
            var selectionsToProcess = Selector.Selections;

            switch (processOrder)
            {
                case ProcessOrder.TopToBottom:
                    selectionsToProcess = Selector.Selections
                        .OrderBy(n => n.Caret.GetPosition(Snapshot)).ToList();
                    break;
                case ProcessOrder.BottomToTop:
                    selectionsToProcess = Selector.Selections
                        .OrderByDescending(n => n.Caret.GetPosition(Snapshot)).ToList();
                    break;
                case ProcessOrder.Normal:
                default:
                    selectionsToProcess = Selector.Selections;
                    break;
            }

            foreach (var selection in selectionsToProcess)
            {
                if (selection.IsSelection())
                {
                    view.Selection.Select(
                        new SnapshotSpan(
                            selection.Start.GetPoint(Snapshot),
                            selection.End.GetPoint(Snapshot)
                        ),
                        selection.IsReversed(Snapshot)
                    );
                }
                if (selection.VirtualSpaces == 0)
                {
                    view.Caret.MoveTo(selection.Caret.GetPoint(Snapshot));
                }
                else
                {
                    view.Caret.MoveTo(selection.GetVirtualPoint(Snapshot));
                }

                var previousCaretPosition = selection.Caret.GetPosition(Snapshot);

                result = NextCommandTarget.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);

                selection.VirtualSpaces = view.Caret.Position.VirtualSpaces;

                selection.SetCaretPosition(view.Caret.Position.BufferPosition, verticalMove, Snapshot);

                if (view.Selection.IsEmpty)
                {
                    selection.Start = null;
                    selection.End = null;
                }
                else if (modifySelections)
                {
                    selection.UpdateSelection(previousCaretPosition, Snapshot);
                }
                else if (invokeCommand)
                {
                    selection.SetSelection(view.Selection.StreamSelectionSpan, Snapshot);
                }
                view.Selection.Clear();
            }

            if (modifySelections)
            {
                Selector.CombineOverlappingSelections();
            }

            if (Selector.Dte.UndoContext.IsOpen)
                Selector.Dte.UndoContext.Close();

            // Set new search text. Needed if selection is modified
            if (modifySelections)
            {
                var lastSelection = Selector.Selections.Last();
                if (lastSelection.IsSelection())
                {
                    var startPosition = lastSelection.Start.GetPosition(Snapshot);
                    var endPosition = lastSelection.End.GetPosition(Snapshot);

                    Selector.SearchText = Snapshot.GetText(
                        startPosition,
                        endPosition - startPosition
                    );
                }
            }

            view.Caret.MoveTo(Selector.Selections.Last().GetVirtualPoint(Snapshot));
            view.Selection.Clear();

            // Goes to caret-only mode
            if (clearSelections)
            {
                Selector.Selections.ForEach(s =>
                    {
                        s.Start = null;
                        s.End = null;
                    }
                );
            }

            return result;
        }

        /// <summary>
        /// Copies/cuts each selection as normal, and saves the text into the selection-item
        /// </summary>
        /// <param name="pguidCmdGroup"></param>
        /// <param name="nCmdID"></param>
        /// <param name="nCmdexecopt"></param>
        /// <param name="pvaIn"></param>
        /// <param name="pvaOut"></param>
        /// <returns></returns>
        private int HandleMultiCopyCut(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            var result = VSConstants.S_OK;

            if (!Selector.Dte.UndoContext.IsOpen)
                Selector.Dte.UndoContext.Open(Vsix.Name);

            foreach (var selection in Selector.Selections)
            {
                if (selection.IsSelection())
                {
                    view.Selection.Select(
                        new SnapshotSpan(
                            selection.Start.GetPoint(Snapshot),
                            selection.End.GetPoint(Snapshot)
                        ),
                        false
                    );

                    // Copies/cuts and saves the text on the selection
                    result = NextCommandTarget.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
                    selection.CopiedText = Clipboard.GetText();
                }
            }

            Selector.SavedClipboard = Selector.Selections.Where(s => !string.IsNullOrEmpty(s.CopiedText))
                .Select(s => s.CopiedText);

            if (Selector.Dte.UndoContext.IsOpen)
                Selector.Dte.UndoContext.Close();

            return result;
        }

        /// <summary>
        /// If a previous multi-copy/cut has been made, this pastes the saved text at cursor-positions
        /// </summary>
        /// <param name="pguidCmdGroup"></param>
        /// <param name="nCmdID"></param>
        /// <param name="nCmdexecopt"></param>
        /// <param name="pvaIn"></param>
        /// <param name="pvaOut"></param>
        /// <returns></returns>
        private int HandleMultiPaste(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            var result = VSConstants.S_OK;

            if (!Selector.Dte.UndoContext.IsOpen)
                Selector.Dte.UndoContext.Open(Vsix.Name);

            foreach (var selection in Selector.Selections)
            {
                if (!string.IsNullOrEmpty(selection.CopiedText))
                {
                    if (selection.IsSelection())
                    {
                        view.Selection.Select(
                            new SnapshotSpan(
                                selection.Start.GetPoint(Snapshot),
                                selection.End.GetPoint(Snapshot)
                            ),
                            false
                        );
                    }

                    view.Caret.MoveTo(selection.Caret.GetPoint(Snapshot));

                    Clipboard.SetText(selection.CopiedText);
                    result = NextCommandTarget.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
                }
            }

            if (Selector.Dte.UndoContext.IsOpen)
                Selector.Dte.UndoContext.Close();

            return result;
        }
    }
}
