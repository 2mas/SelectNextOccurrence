using System;
using System.Collections.Generic;
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
    internal class CommandTarget : IOleCommandTarget
    {
        private enum ProcessOrder { Normal, TopToBottom, BottomToTop }

        private readonly IWpfTextView view;

        private readonly AdornmentLayer adornmentLayer;

        private ITextSnapshot Snapshot => view.TextSnapshot;

        private Selector Selector => adornmentLayer.Selector;

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
            if (pguidCmdGroup == VSConstants.VSStd2K)
            {
                switch ((VSConstants.VSStd2KCmdID) nCmdID)
                {
                    case VSConstants.VSStd2KCmdID.SolutionPlatform:
                        return NextCommandTarget.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
                }
            }

            if (pguidCmdGroup == VSConstants.GUID_VSStandardCommandSet97)
            {
                switch ((VSConstants.VSStd97CmdID) nCmdID)
                {
                    case VSConstants.VSStd97CmdID.SolutionCfg:
                    case VSConstants.VSStd97CmdID.SearchCombo:
                        return NextCommandTarget.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
                }
            }

            var result = VSConstants.S_OK;
            System.Diagnostics.Debug.WriteLine("grp: {0}, id: {1}", pguidCmdGroup.ToString(), nCmdID.ToString());

            if (!Selector.Selections.Any())
                return ProcessSingleCursor(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut, ref result);

            var modifySelections = false;
            var clearSelections = false;
            var verticalMove = false;
            var processOrder = ProcessOrder.Normal;
            var invokeCommand = false;

            if (pguidCmdGroup == VSConstants.GUID_VSStandardCommandSet97)
            {
                switch ((VSConstants.VSStd97CmdID) nCmdID)
                {
                    case VSConstants.VSStd97CmdID.Copy:
                    case VSConstants.VSStd97CmdID.Cut:
                        return HandleMultiCopyCut(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
                    case VSConstants.VSStd97CmdID.Paste:
                        // Only perform multi-paste if the saved clipboard have been copied with 
                        // this extension, otherwise paste as default.
                        if (Selector.SavedClipboard.Any() && Selector.Selections.Any())
                        {
                            // Copy/cut has been made from a non-multiedit place, proceed normal multi-processing
                            if (Clipboard.GetText() != string.Join(Environment.NewLine, Selector.SavedClipboard))
                            {
                                Selector.ClearSavedClipboard();
                            }
                            else
                            {
                                return HandleMultiPaste(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
                            }
                        }

                        break;
                    case VSConstants.VSStd97CmdID.Undo:
                        result = NextCommandTarget.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
                        Selector.UndoSelectionsHistory();
                        adornmentLayer.DrawAdornments();
                        return result;
                    case VSConstants.VSStd97CmdID.Redo:
                        result = NextCommandTarget.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
                        Selector.RedoSelectionsHistory();
                        adornmentLayer.DrawAdornments();
                        return result;
                }
            }
            else if (pguidCmdGroup == typeof(VSConstants.VSStd2KCmdID).GUID)
            {
                switch ((VSConstants.VSStd2KCmdID) nCmdID)
                {
                    case VSConstants.VSStd2KCmdID.UP:
                    case VSConstants.VSStd2KCmdID.DOWN:
                        verticalMove = true;
                        clearSelections = true;
                        break;
                    case VSConstants.VSStd2KCmdID.LEFT:
                    case VSConstants.VSStd2KCmdID.RIGHT:
                    case VSConstants.VSStd2KCmdID.WORDPREV:
                    case VSConstants.VSStd2KCmdID.WORDNEXT:
                        clearSelections = true;
                        break;
                    case VSConstants.VSStd2KCmdID.CANCEL:
                        Selector.DiscardSelections();
                        break;
                    case VSConstants.VSStd2KCmdID.PAGEDN:
                    case VSConstants.VSStd2KCmdID.PAGEUP:
                    case VSConstants.VSStd2KCmdID.END:
                    case VSConstants.VSStd2KCmdID.HOME:
                    case VSConstants.VSStd2KCmdID.END_EXT:
                    case VSConstants.VSStd2KCmdID.HOME_EXT:
                        Selector.DiscardSelections();
                        result = NextCommandTarget.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
                        break;
                    case VSConstants.VSStd2KCmdID.UP_EXT:
                    case VSConstants.VSStd2KCmdID.DOWN_EXT:
                        verticalMove = true;
                        modifySelections = true;
                        break;
                    case VSConstants.VSStd2KCmdID.WORDPREV_EXT:
                    case VSConstants.VSStd2KCmdID.BOL_EXT:
                    case VSConstants.VSStd2KCmdID.LEFT_EXT:
                    case VSConstants.VSStd2KCmdID.WORDNEXT_EXT:
                    case VSConstants.VSStd2KCmdID.EOL_EXT:
                    case VSConstants.VSStd2KCmdID.RIGHT_EXT:
                        modifySelections = true;
                        break;
                    case VSConstants.VSStd2KCmdID.SELLOWCASE:
                    case VSConstants.VSStd2KCmdID.SELUPCASE:
                    case VSConstants.VSStd2KCmdID.COMMENT_BLOCK:
                    case VSConstants.VSStd2KCmdID.UNCOMMENT_BLOCK:
                        invokeCommand = true;
                        break;
                }
            }
            else if (pguidCmdGroup == typeof(VSConstants.VSStd12CmdID).GUID)
            {
                switch ((VSConstants.VSStd12CmdID) nCmdID)
                {
                    case VSConstants.VSStd12CmdID.MoveSelLinesUp:
                        invokeCommand = true;
                        processOrder = ProcessOrder.TopToBottom;
                        break;
                    case VSConstants.VSStd12CmdID.MoveSelLinesDown:
                        invokeCommand = true;
                        processOrder = ProcessOrder.BottomToTop;
                        break;
                }

                if (pguidCmdGroup == PackageGuids.guidNextOccurrenceCommandsPackageCmdSet)
                {
                    verticalMove = nCmdID == PackageIds.AddCaretAboveCommandId
                                   || nCmdID == PackageIds.AddCaretBelowCommandId;
                }
            }
            else if (pguidCmdGroup == PackageGuids.guidExtensionSubWordNavigation)
            {
                switch (nCmdID)
                {
                    case PackageIds.ExtensionSubwordNavigationNextExtend:
                    case PackageIds.ExtensionSubwordNavigationPreviousExtend:
                        modifySelections = true;
                        break;
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

        private int ProcessSingleCursor(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut, ref int result)
        {
            // if copy/cut, clear saved clipboard
            if (pguidCmdGroup == VSConstants.GUID_VSStandardCommandSet97)
            {
                var command = (VSConstants.VSStd97CmdID) nCmdID;
                if (command == VSConstants.VSStd97CmdID.Copy || command == VSConstants.VSStd97CmdID.Cut)
                {
                    Selector.ClearSavedClipboard();
                }
                else if (command == VSConstants.VSStd97CmdID.Undo)
                {
                    result = NextCommandTarget.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
                    Selector.UndoSelectionsHistory();
                    adornmentLayer.DrawAdornments();
                    return result;
                }
                else if(command == VSConstants.VSStd97CmdID.Redo)
                {
                    result = NextCommandTarget.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
                    Selector.RedoSelectionsHistory();
                    adornmentLayer.DrawAdornments();
                    return result;
                }
            }
            return NextCommandTarget.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
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

            Selector.StorePreviousSelectionsHistory();

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

            Selector.SaveSelectionsHistory();

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
        /// Copies/cuts each selection as normal, saves the texts into static <see cref="Selector.SavedClipboard"/>
        /// Selections are processed from top to bottom
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

            var copiedTexts = new List<string>();

            foreach (var selection in Selector.Selections.OrderBy(s => s.Caret.GetPosition(Snapshot)))
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

                    var copiedText = Selector.GetCurrentlySelectedText();

                    if (!string.IsNullOrEmpty(copiedText))
                        copiedTexts.Add(copiedText);

                    // Copies/cuts and saves the text as static list of strings
                    result = NextCommandTarget.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
                }
            }

            Selector.SavedClipboard = copiedTexts;

            try
            {
                Clipboard.SetText(string.Join(Environment.NewLine, Selector.SavedClipboard));
            }
            catch
            {
                System.Diagnostics.Debug.WriteLine("Clipboard copy error saving: {0}", string.Join(Environment.NewLine, Selector.SavedClipboard));
            }

            if (Selector.Dte.UndoContext.IsOpen)
                Selector.Dte.UndoContext.Close();

            return result;
        }

        /// <summary>
        /// If a previous multi-copy/cut has been made, this pastes the saved text at cursor-positions
        /// Selections are processed from top to bottom
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
            var clipboardCount = Selector.SavedClipboard.Count();

            if (clipboardCount > 0)
            {
                if (!Selector.Dte.UndoContext.IsOpen)
                    Selector.Dte.UndoContext.Open(Vsix.Name);

                var index = 0;

                foreach (var selection in Selector.Selections.OrderBy(s => s.Caret.GetPosition(Snapshot)))
                {
                    if (index == clipboardCount)
                        break;

                    var copiedText = Selector.SavedClipboard.ElementAt(index);
                    if (!string.IsNullOrEmpty(copiedText))
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

                        view.Caret.MoveTo(selection.GetVirtualPoint(Snapshot));
                        Selector.InsertText(copiedText);
                    }

                    index++;
                }

                if (Selector.Dte.UndoContext.IsOpen)
                    Selector.Dte.UndoContext.Close();
            }

            return result;
        }
    }
}
