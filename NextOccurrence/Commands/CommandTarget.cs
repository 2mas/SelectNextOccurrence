using System;
using System.Linq;
using System.Windows;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace NextOccurrence.Commands
{
    /// <summary>
    /// Handles keyboard, typing and combinations in text-editor
    /// </summary>
    class CommandTarget : IOleCommandTarget
    {
        private readonly IWpfTextView view;

        private ITextSnapshot Snapshot { get { return this.view.TextSnapshot; } }

        private NextOccurrenceSelector Selector { get { return this.adornmentLayer.Selector; } }

        /// <summary>
        /// Next commandhandler
        /// </summary>
        public IOleCommandTarget NextCommandTarget { get; set; }

        private NextOccurrenceAdornment adornmentLayer;

        public CommandTarget(IWpfTextView view)
        {
            this.view = view;
            this.adornmentLayer = view.Properties
                .GetProperty<NextOccurrenceAdornment>(typeof(NextOccurrenceAdornment));
        }

        public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            return NextCommandTarget.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
        }

        public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            if (!Selector.Selections.Any())
                return NextCommandTarget.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);

            int result = VSConstants.S_OK;
            bool modifySelections = false;
            bool clearSelections = false;

            if (pguidCmdGroup == typeof(VSConstants.VSStd2KCmdID).GUID
                || pguidCmdGroup == VSConstants.GUID_VSStandardCommandSet97)
            {
                if (pguidCmdGroup == VSConstants.GUID_VSStandardCommandSet97)
                {
                    switch (nCmdID)
                    {
                        case ((uint)VSConstants.VSStd97CmdID.Copy):
                        case ((uint)VSConstants.VSStd97CmdID.Cut):
                            Selector.Dte.UndoContext.Open("SelectNextOccurrence");

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

                            Selector.Dte.UndoContext.Close();
                            return result;
                        case ((uint)VSConstants.VSStd97CmdID.Paste):
                            // Only multi-paste different texts if all our selections have been copied with 
                            // this extension, otherwise paste as default.
                            if (Selector.Selections.All(s => !String.IsNullOrEmpty(s.CopiedText)))
                            {
                                Selector.Dte.UndoContext.Open("SelectNextOccurrence");
                                foreach (var selection in Selector.Selections)
                                {
                                    if (!String.IsNullOrEmpty(selection.CopiedText))
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

                                Selector.Dte.UndoContext.Close();
                                return result;
                            }
                            break;
                        case ((uint)VSConstants.VSStd97CmdID.Undo):
                        case ((uint)VSConstants.VSStd97CmdID.Redo):
                            result = NextCommandTarget.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
                            Selector.IsReversing = false;
                            adornmentLayer.DrawAdornments();
                            return result;
                    }
                }
                if (pguidCmdGroup == typeof(VSConstants.VSStd2KCmdID).GUID)
                {
                    switch (nCmdID)
                    {
                        case ((uint)VSConstants.VSStd2KCmdID.LEFT):
                        case ((uint)VSConstants.VSStd2KCmdID.RIGHT):
                        case ((uint)VSConstants.VSStd2KCmdID.UP):
                        case ((uint)VSConstants.VSStd2KCmdID.DOWN):
                        case ((uint)VSConstants.VSStd2KCmdID.WORDPREV):
                        case ((uint)VSConstants.VSStd2KCmdID.WORDNEXT):
                            // Remove selected spans but keep carets
                            clearSelections = true;
                            Selector.IsReversing = false;
                            break;
                        case ((uint)VSConstants.VSStd2KCmdID.CANCEL):
                            Selector.IsReversing = false;
                            Selector.Selections.Clear();
                            break;
                        case ((uint)VSConstants.VSStd2KCmdID.PAGEDN):
                        case ((uint)VSConstants.VSStd2KCmdID.PAGEUP):
                        case ((uint)VSConstants.VSStd2KCmdID.END):
                        case ((uint)VSConstants.VSStd2KCmdID.HOME):
                        case ((uint)VSConstants.VSStd2KCmdID.END_EXT):
                        case ((uint)VSConstants.VSStd2KCmdID.HOME_EXT):
                            Selector.Selections.Clear();
                            Selector.IsReversing = false;
                            result = NextCommandTarget.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
                            break;
                        case ((uint)VSConstants.VSStd2KCmdID.WORDPREV_EXT):
                        case ((uint)VSConstants.VSStd2KCmdID.BOL_EXT):
                        case ((uint)VSConstants.VSStd2KCmdID.LEFT_EXT):
                        case ((uint)VSConstants.VSStd2KCmdID.UP_EXT):
                            Selector.IsReversing = Selector.Selections.All(s => !s.IsSelection())
                                || Selector.IsReversing
                                || Selector.Selections.Last().Reversing(Snapshot);
                            modifySelections = true;
                            break;
                        case ((uint)VSConstants.VSStd2KCmdID.WORDNEXT_EXT):
                        case ((uint)VSConstants.VSStd2KCmdID.EOL_EXT):
                        case ((uint)VSConstants.VSStd2KCmdID.RIGHT_EXT):
                        case ((uint)VSConstants.VSStd2KCmdID.DOWN_EXT):
                            Selector.IsReversing = !(Selector.Selections.All(s => !s.IsSelection()) || !Selector.IsReversing);
                            modifySelections = true;
                            break;
                    }
                }

                if (Selector.Selections.Any())
                {
                    Selector.Dte.UndoContext.Open("SelectNextOccurrence");

                    foreach (var selection in Selector.Selections)
                    {
                        if (selection.IsSelection())
                        {
                            view.Selection.Select(
                                new SnapshotSpan(
                                    selection.Start.GetPoint(Snapshot),
                                    selection.End.GetPoint(Snapshot)
                                ),
                                Selector.IsReversing
                            );
                        }

                        view.Caret.MoveTo(selection.Caret.GetPoint(Snapshot));

                        result = NextCommandTarget.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);

                        selection.Caret = Snapshot.CreateTrackingPoint(
                            view.Caret.Position.BufferPosition.Position,
                            PointTrackingMode.Positive
                        );

                        if (view.Selection.IsEmpty)
                        {
                            selection.Start = null;
                            selection.End = null;
                            modifySelections = false;
                        }

                        if (modifySelections)
                        {
                            var newSpan = view.Selection.StreamSelectionSpan;

                            selection.Start = Snapshot.CreateTrackingPoint(
                                newSpan.Start.Position.Position > newSpan.End.Position.Position ?
                                newSpan.End.Position.Position
                                : newSpan.Start.Position.Position,
                                PointTrackingMode.Positive
                            );

                            selection.End = Snapshot.CreateTrackingPoint(
                                newSpan.Start.Position.Position > newSpan.End.Position.Position ?
                                newSpan.Start.Position.Position
                                : newSpan.End.Position.Position,
                                PointTrackingMode.Positive
                            );

                            view.Selection.Clear();
                        }
                    }

                    Selector.Dte.UndoContext.Close();

                    // set new searchtext needed if selection is modified
                    if (modifySelections)
                    {
                        var startPosition = Selector.Selections.Last().Start.GetPosition(Snapshot);
                        var endPosition = Selector.Selections.Last().End.GetPosition(Snapshot);

                        Selector.SearchText = Snapshot.GetText(
                            startPosition,
                            endPosition - startPosition
                        );
                    }

                    if (clearSelections)
                    {
                        Selector.Selections.ForEach(s =>
                            {
                                s.Start = null;
                                s.End = null;
                            }
                        );
                    }
                }

                view.Selection.Clear();

                // Duplicate carets to remove. Happens if multiple selection are on same line
                // and hitting home/end
                if (Selector.Selections
                        .GroupBy(s => s.Caret.GetPoint(Snapshot).Position)
                        .Where(s => s.Count() > 1).Any())
                {
                    var distinctSelections = Selector.Selections
                        .GroupBy(s => s.Caret.GetPoint(Snapshot).Position)
                        .Select(s => s.First()).ToList();

                    Selector.Selections = distinctSelections;
                }
            }
            else
            {
                result = NextCommandTarget.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
            }

            adornmentLayer.DrawAdornments();
            return result;
        }
    }
}
