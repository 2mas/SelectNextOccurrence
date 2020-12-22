using System;
using System.Collections.Generic;
using System.Diagnostics;
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

            if (!Selector.Selections.Any())
                return ProcessSingleCursor(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut, ref result);

            var modifySelections = false;
            var clearSelections = false;
            var verticalMove = false;
            var invokeCommand = false;
            var processOrder = ProcessOrder.Normal;

            if (pguidCmdGroup == typeof(VSConstants.VSStd97CmdID).GUID)
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
                            // Copy/cut has been made from a non multi-edit place, proceed normal multi-processing
                            if (Clipboard.GetText() != string.Join(Environment.NewLine, Selector.SavedClipboard))
                            {
                                Selector.ClearSavedClipboard();
                            }
                            else
                            {
                                return HandleMultiPaste();
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
                    default:
                        Debug.WriteLine($"{nameof(VSConstants.VSStd97CmdID)}, com: {(VSConstants.VSStd97CmdID) nCmdID}");
                        break;
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
                        Selector.CancelSelectNextOccurence();
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
                    case VSConstants.VSStd2KCmdID.TOGGLE_OVERTYPE_MODE:
                    case VSConstants.VSStd2KCmdID.TOGGLEVISSPACE:
                    case VSConstants.VSStd2KCmdID.TOGGLEWORDWRAP:
                        result = NextCommandTarget.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
                        adornmentLayer.DrawAdornments();
                        return result;
                    default:
                        Debug.WriteLine($"{nameof(VSConstants.VSStd2KCmdID)}, com: {(VSConstants.VSStd2KCmdID) nCmdID}");
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
                    default:
                        Debug.WriteLine($"{nameof(VSConstants.VSStd12CmdID)}, com: {(VSConstants.VSStd12CmdID) nCmdID}");
                        break;
                }
            }
            else if (pguidCmdGroup == PackageGuids.guidVS16Commands)
            {
                // Support for toggle line comment and toggle block comment command that was introduced in VS2019
                if (nCmdID == 48 || nCmdID == 49)
                {
                    invokeCommand = true;
                }
                else
                {
                    Debug.WriteLine($"{nameof(PackageGuids.guidVS16Commands)}, id: {nCmdID}");
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
                    default:
                        Debug.WriteLine($"{nameof(PackageGuids.guidExtensionSubWordNavigation)}, id: {nCmdID}");
                        break;
                }
            }
            else if (pguidCmdGroup == PackageGuids.guidNextOccurrenceCommandsPackageCmdSet)
            {
                return NextCommandTarget.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
            }
            else
            {
                Debug.WriteLine($"group: {pguidCmdGroup}, id: {nCmdID}");
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
                else if (command == VSConstants.VSStd97CmdID.Redo)
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

            Selector.OpenUndoContext();

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
                    view.Selection.Select(selection.GetSpan(Snapshot), false);
                }

                var previousCaretPosition = selection.VirtualSpaces == 0
                    ? view.Caret.MoveTo(selection.Caret.GetPoint(Snapshot))
                    : view.Caret.MoveTo(selection.GetVirtualPoint(Snapshot));

                result = NextCommandTarget.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);

                var newCaretPosition = view.Caret.Position.BufferPosition.Position;

                if (verticalMove)
                {
                    // Sets new caret position to start- or end-position of snapshot if the new/processed caret
                    // is on the same line as the previous caret after a vertical move has been made.
                    var previousLine = Snapshot.GetLineNumberFromPosition(selection.Caret.GetPosition(Snapshot));
                    var newLine = Snapshot.GetLineFromPosition(newCaretPosition).LineNumber;

                    if (previousLine == newLine && (newLine == 0 || newLine == Snapshot.LineCount - 1))
                    {
                        newCaretPosition = newLine == 0 ? 0 : Snapshot.Length;
                    }
                    else
                    {
                        newCaretPosition = selection
                            .GetCaretColumnPosition(newCaretPosition, Snapshot, view.FormattedLineSource.TabSize);
                    }
                }
                else
                {
                    selection.ColumnPosition = Selector.GetColumnPosition();
                }

                selection.Caret = Snapshot.CreateTrackingPoint(newCaretPosition);
                selection.VirtualSpaces = view.Caret.Position.VirtualSpaces;

                if (view.Selection.IsEmpty)
                {
                    selection.Start = null;
                    selection.End = null;
                }
                else if (modifySelections)
                {
                    selection.UpdateSelection(previousCaretPosition.BufferPosition, Snapshot);
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

                // Set new search text. Needed if selection is modified
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

            Selector.CloseUndoContext();

            view.Caret.MoveTo(Selector.Selections.Last().GetVirtualPoint(Snapshot));
            view.Selection.Clear();

            // Goes to caret-only mode
            if (clearSelections)
            {
                foreach (var selection in Selector.Selections)
                {
                    selection.Start = null;
                    selection.End = null;
                }
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
            if (Selector.Selections.Count == 1)
                return NextCommandTarget.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);

            var result = VSConstants.S_OK;

            Selector.OpenUndoContext();

            var copiedTexts = new List<string>();

            foreach (var selection in Selector.Selections.OrderBy(s => s.Caret.GetPosition(Snapshot)))
            {
                if (selection.IsSelection())
                {
                    view.Selection.Select(selection.GetSpan(Snapshot), false);

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
                Debug.WriteLine("Clipboard copy error saving: {0}", string.Join(Environment.NewLine, Selector.SavedClipboard));
            }

            Selector.CloseUndoContext();

            return result;
        }

        /// <summary>
        /// If a previous multi-copy/cut has been made, this pastes the saved text at cursor-positions
        /// Selections are processed from top to bottom
        /// </summary>
        /// <returns></returns>
        private int HandleMultiPaste()
        {
            var result = VSConstants.S_OK;
            var clipboardCount = Selector.SavedClipboard.Count();

            if (clipboardCount > 0)
            {
                Selector.OpenUndoContext();

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
                            view.Selection.Select(selection.GetSpan(Snapshot), false);
                        }

                        view.Caret.MoveTo(selection.GetVirtualPoint(Snapshot));
                        Selector.InsertText(copiedText);
                    }

                    index++;
                }

                Selector.CloseUndoContext();
            }

            return result;
        }
    }
}
