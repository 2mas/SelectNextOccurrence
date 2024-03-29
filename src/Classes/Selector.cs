﻿using System;
using System.Collections.Generic;
using System.Linq;
using EnvDTE;
using EnvDTE80;
using Microsoft;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Text.Outlining;
using SelectNextOccurrence.Options;

namespace SelectNextOccurrence
{
    /// <summary>
    /// Class responsible of keeping and making the selections
    /// </summary>
    internal class Selector
    {
        #region #services
        internal readonly IEditorOperations EditorOperations;

        /// <summary>
        /// The top level in the Visual Studio automation object model.
        /// Needed to get the find-object to determine search-options
        /// </summary>
        internal readonly DTE2 Dte;

        private readonly ITextSearchService textSearchService;

        /// <summary>
        /// Expands regions if selected text is in this region
        /// </summary>
        private readonly IOutliningManager outliningManager;

        #endregion

        private readonly IWpfTextView view;

        private readonly HistoryManager historyManager;

        private ITextSnapshot Snapshot => view.TextSnapshot;

        /// <summary>
        /// Contains all tracking-points for selections and carets
        /// This is what is getting drawn in the adornment layer
        /// </summary>
        internal List<Selection> Selections { get; set; }

        /// <summary>
        /// Stores copied texts from selections after they are abandoned for later pasting
        /// when back to one caret or across multiple documents
        /// </summary>
        internal static IEnumerable<string> SavedClipboard { get; set; } = new List<string>();

        /// <summary>
        /// The last search-term
        /// </summary>
        internal string SearchText { get; set; }

        /// <summary>
        /// Saves a caretposition to be added to selections at a later time
        /// Usecase is when adding the first caret by mouse-clicking, checks needs to
        /// made when releasing the cursor
        /// </summary>
        internal ITrackingPoint StashedCaret { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Selector"/> class.
        /// </summary>
        /// <param name="view"></param>
        /// <param name="textSearchService"></param>
        /// <param name="editorOperationsService"></param>
        /// <param name="outliningManagerService"></param>
        public Selector(
            IWpfTextView view,
            ITextSearchService textSearchService,
            IEditorOperationsFactoryService editorOperationsService,
            IOutliningManagerService outliningManagerService)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            this.view = view;

            // Services
            this.textSearchService = textSearchService;
            this.EditorOperations = editorOperationsService.GetEditorOperations(this.view);
            this.outliningManager = outliningManagerService?.GetOutliningManager(this.view);
            this.Dte = (DTE2) Package.GetGlobalService(typeof(DTE));
            Assumes.Present(Dte);
            this.Selections = new List<Selection>();
            this.historyManager = new HistoryManager();
        }

        /// <summary>
        /// Adds start and end position of current selected text. 
        /// Default caret-position is set to the end of the selection, 
        /// caret-position will be moved by the command-filter when editing the text
        /// </summary>
        private void AddCurrentSelectionToSelections()
        {
            var start = view.Selection.Start.Position;
            var end = view.Selection.End.Position;
            var caret = view.Selection.IsReversed
                ? start : end;

            Selections.Add(
                new Selection
                {
                    Start = Snapshot.CreateTrackingPoint(start),
                    End = Snapshot.CreateTrackingPoint(end),
                    Caret = Snapshot.CreateTrackingPoint(caret),
                    ColumnPosition = GetColumnPosition(),
                    VirtualSpaces = view.Caret.Position.VirtualSpaces
                }
            );

            SearchText = EditorOperations.SelectedText;
        }

        /// <summary>
        /// The FindData to be used in search
        /// If user has toggled match-case/match whole word in their find-options we use this here too,
        /// if not overriden by exact-parameter
        /// </summary>
        /// <param name="reverse">Search in reverse direction</param>
        /// <param name="exact">Override find-dialog and searches for an exact match</param>
        /// <returns></returns>
        private FindData GetFindData(bool reverse = false, bool exact = false)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var findData = new FindData(SearchText, Snapshot)
            {
                FindOptions = FindOptions.Multiline
            };

            if (exact)
            {
                findData.FindOptions |= FindOptions.MatchCase | FindOptions.WholeWord;
            }
            else
            {   
                if (Dte.Find.MatchCase)
                    findData.FindOptions |= FindOptions.MatchCase;

                if (Dte.Find.MatchWholeWord)
                    findData.FindOptions |= FindOptions.WholeWord;
            }

            if (reverse)
                findData.FindOptions |= FindOptions.SearchReverse;

            return findData;
        }

        private void ProcessFoundOccurrence(SnapshotSpan occurrence)
        {
            if (!Selections.Any(s => s.OverlapsWith(occurrence, Snapshot)))
            {
                var start = Snapshot.CreateTrackingPoint(occurrence.Start);
                var end = Snapshot.CreateTrackingPoint(occurrence.End);
                var caret = Selections.Last().IsReversed(Snapshot)
                    ? start : end;

                view.Caret.MoveTo(caret == start ? occurrence.Start : occurrence.End);

                Selections.Add(
                    new Selection
                    {
                        Start = start,
                        End = end,
                        Caret = caret,
                        ColumnPosition = GetColumnPosition()
                    }
                );

                outliningManager.ExpandAll(occurrence, r => r.IsCollapsed);

                view.ViewScroller.EnsureSpanVisible(
                    new SnapshotSpan(view.Caret.Position.BufferPosition, 0)
                );
            }
        }

        /// <summary>
        /// Handles finding occurrences, selecting and adding to current selections
        /// </summary>
        /// <param name="reverseDirection">Search document in reverse direction for an occurrence</param>
        /// <param name="exactMatch">Search document for an exact match, overrides find-dialog settings</param>
        internal void SelectNextOccurrence(bool reverseDirection = false, bool exactMatch = false)
        {
            // Caret placed on a word, but nothing selected
            if (!Selections.Any() && view.Selection.IsEmpty)
            {
                SelectCurrentWord(view.Caret.Position.BufferPosition);
                return;
            }

            // First selection is selected by user, future selections will be located and selected on command-invocation
            if (!Selections.Any() && !view.Selection.IsEmpty)
                AddCurrentSelectionToSelections();

            // Multiple selections
            if (Selections.Any())
            {
                // Select words at caret again, this is where we have abandoned selections and goes to carets
                if (Selections.Any(s => !s.IsSelection()))
                {
                    var oldSelections = Selections;
                    Selections = new List<Selection>();

                    // Note: The list is in reverse order to fix a bug in EditorOperations.SelectCurrentWord()
                    foreach (var selection in oldSelections.OrderByDescending(n => n.Caret.GetPosition(Snapshot)))
                    {
                        if (!selection.IsSelection())
                        {
                            view.Caret.MoveTo(selection.Caret.GetPoint(Snapshot));
                            SelectCurrentWord(selection.Caret.GetPoint(Snapshot));
                        }
                        else
                        {
                            Selections.Add(selection);
                        }
                    }
                }
                else
                {
                    var orderedSelections = Selections.OrderBy(n => n.Caret.GetPosition(Snapshot)).ToList();
                    var startIndex = ExtensionOptions.Instance.InwardSelection ^ reverseDirection ? 0 : orderedSelections.Count - 1;
                    var direction = reverseDirection ? -1 : 1;

                    var index = startIndex;
                    do
                    {
                        var position = reverseDirection
                            ? orderedSelections[index].Start.GetPosition(Snapshot)
                            : orderedSelections[index].End.GetPosition(Snapshot);

                        index = (index + direction + orderedSelections.Count ) % orderedSelections.Count;
                        if (textSearchService.FindNext(position, true, GetFindData(reverseDirection, exactMatch))
                            is SnapshotSpan occurrence
                            && !orderedSelections[index].OverlapsWith(occurrence, Snapshot))
                        {
                            ProcessFoundOccurrence(occurrence);
                            break;
                        }
                    } while (startIndex != index);
                }

                view.Selection.Clear();
                view.Caret.MoveTo(Selections.Last().Caret.GetPoint(Snapshot));
            }
        }

        /// <summary>
        /// Selects the current word at caret, but shifts the selection to
        /// the left to the previous word if the character at the caretposition is not a letter or digit and is
        /// immediately preceded by a word
        /// </summary>
        /// <param name="caretPosition"></param>
        private void SelectCurrentWord(SnapshotPoint caretPosition)
        {
            EditorOperations.SelectCurrentWord();

            if (EditorOperations.SelectedText.Length != 0
                && !char.IsLetterOrDigit(EditorOperations.SelectedText[0])
                && Snapshot.GetLineColumnFromPosition(caretPosition) != 0
                && char.IsLetterOrDigit(Snapshot[caretPosition - 1]))
            {
                view.Caret.MoveTo(caretPosition - 1);
                EditorOperations.SelectCurrentWord();
            }

            AddCurrentSelectionToSelections();
        }

        /// <summary>
        /// Handles finding all occurrences
        /// </summary>
        internal void SelectAllOccurrences()
        {
            // Get a valid first selection to begin with
            SelectNextOccurrence();

            foreach (var occurrence in textSearchService.FindAll(GetFindData()))
                ProcessFoundOccurrence(occurrence);
        }

        internal void ConvertSelectionToMultipleCursors()
        {
            var beginLineNumber = view.Selection.Start.Position.GetContainingLine().LineNumber;
            var endLineNumber = view.Selection.End.Position.GetContainingLine().LineNumber;

            var currentPosition = view.Caret.Position.BufferPosition;
            if (beginLineNumber != endLineNumber)
            {
                for (var lineNumber = beginLineNumber; lineNumber < endLineNumber; lineNumber++)
                {
                    var line = Snapshot.GetLineFromLineNumber(lineNumber);
                    view.Caret.MoveTo(line.End);
                    Selections.Add(
                        new Selection
                        {
                            Caret = Snapshot.CreateTrackingPoint(line.End.Position),
                            ColumnPosition = GetColumnPosition(),
                        }
                    );
                }

                view.Caret.MoveTo(view.Selection.End.Position);
                Selections.Add(
                    new Selection
                    {
                        Caret = Snapshot.CreateTrackingPoint(view.Selection.End.Position),
                        ColumnPosition = GetColumnPosition()
                    }
                );

                view.Caret.MoveTo(currentPosition);
                view.Selection.Clear();
            }
            else
            {
                AddCurrentSelectionToSelections();
            }
        }

        internal void AddCaretAbove()
        {
            foreach (var selection in Selections.ToList())
            {
                view.Caret.MoveTo(selection.GetVirtualPoint(Snapshot));
                EditorOperations.MoveLineUp(false);
                AddCaretMoveToSelections(selection);
            }

            view.Caret.MoveTo(Selections.Last().GetVirtualPoint(Snapshot));
        }

        internal void AddCaretBelow()
        {
            foreach (var selection in Selections.ToList())
            {
                view.Caret.MoveTo(selection.GetVirtualPoint(Snapshot));
                EditorOperations.MoveLineDown(false);
                AddCaretMoveToSelections(selection);
            }

            view.Caret.MoveTo(Selections.Last().GetVirtualPoint(Snapshot));
        }

        internal void AddCaretMoveToSelections(Selection selection)
        {
            var caretPosition = view.Caret.Position.BufferPosition;
            var newSelection = new Selection
            {
                Start = null,
                End = null,
                Caret = Snapshot.CreateTrackingPoint(caretPosition),
                ColumnPosition = selection.ColumnPosition,
                VirtualSpaces = view.Caret.Position.VirtualSpaces
            };

            var newPosition = newSelection.GetCaretColumnPosition(
                caretPosition,
                Snapshot,
                view.FormattedLineSource.TabSize);

            newSelection.Caret = Snapshot.CreateTrackingPoint(newPosition);

            if (Selections.Any(s => s.Caret.GetPoint(Snapshot) == newPosition))
                return;

            Selections.Add(newSelection);
        }

        internal void AddCurrentCaretToSelections()
        {
            var caretPosition = view.Caret.Position.BufferPosition;

            if (!Selections.Any(s => s.Caret.GetPosition(Snapshot) == caretPosition))
            {
                var newSelection = new Selection
                {
                    Caret = Snapshot.CreateTrackingPoint(caretPosition),
                    ColumnPosition = GetColumnPosition() + view.Caret.Position.VirtualSpaces,
                    VirtualSpaces = view.Caret.Position.VirtualSpaces
                };

                if (!view.Selection.IsEmpty)
                {
                    newSelection.Start = Snapshot.CreateTrackingPoint(view.Selection.Start.Position);
                    newSelection.End = Snapshot.CreateTrackingPoint(view.Selection.End.Position);
                }

                Selections.Add(newSelection);
            }
        }

        /// <summary>
        /// Adds the ALT-clicked caret to selections if it doesn't exist or removes the selection if an existing selection is ALT-clicked
        /// </summary>
        internal void AddMouseCaretToSelectionsOrRemoveExistingSelection()
        {
            var caretPosition = view.Caret.Position.BufferPosition;

            // Checks if any existing caret overlaps, or if this clicked careposition is within another selection, or at the beginning or the end of it, which is considered an overlap here
            var overlaps = Selections.Where(s =>
                s.Caret.GetPosition(Snapshot) == caretPosition
                || (s.IsSelection() && (s.Start.GetPosition(Snapshot) == caretPosition || s.End.GetPosition(Snapshot) == caretPosition))
                || s.OverlapsWith(new SnapshotSpan(caretPosition, Snapshot.Length > caretPosition ? 1 : 0), Snapshot));

            if (overlaps.Count() == 0)
            {
                AddCurrentCaretToSelections();
            }
            else
            {
                overlaps.ToList().ForEach(s => Selections.Remove(s));
            }
        }

        /// <summary>
        /// Duplicate carets to remove. Happens if multiple selection are on same line
        /// and hitting home/end
        /// </summary>
        internal void RemoveDuplicates()
        {
            if (Selections
                .GroupBy(s => s.Caret.GetPoint(Snapshot).Position)
                .Any(s => s.Count() > 1))
            {
                var distinctSelections = Selections
                    .GroupBy(s => s.Caret.GetPoint(Snapshot).Position)
                    .Select(s => s.First()).ToList();

                Selections = distinctSelections;
            }
        }

        internal void CombineOverlappingSelections()
        {
            var overlappingSelections = new List<int>();

            var selections = Selections
                .Where(s => s.IsSelection())
                .Select((selection, index) => new { index, selection })
                .OrderBy(s => s.selection.Caret.GetPoint(Snapshot))
                .ToList();

            for (var index = 0; index < selections.Count - 1; index++)
            {
                var selection = selections[index].selection;
                var nextSelection = selections[index + 1].selection;
                if (selection.End.GetPoint(Snapshot) > nextSelection.Start.GetPoint(Snapshot))
                {
                    nextSelection.Start = Snapshot.CreateTrackingPoint(selection.Start.GetPosition(Snapshot));

                    if (selection.IsReversed(Snapshot))
                    {
                        nextSelection.Caret = selection.Caret;
                    }
                    overlappingSelections.Add(selections[index].index);
                }
            }

            foreach (var index in overlappingSelections.OrderByDescending(n => n))
            {
                Selections.RemoveAt(index);
            }
        }

        /// <summary>
        /// Opens up the undo context for changes and stores previous selections
        /// in a temporary buffer that are only saved if CloseUndoContext is called.
        /// </summary>
        internal void OpenUndoContext()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (!Dte.UndoContext.IsOpen)
                Dte.UndoContext.Open(Vsix.Name);

            historyManager.StoreSelectionsHistory(Snapshot, Selections);
        }

        /// <summary>
        /// Closes the undo context and saves selections to history.
        /// </summary>
        internal void CloseUndoContext()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (Dte.UndoContext.IsOpen)
                Dte.UndoContext.Close();

            historyManager.SaveSelectionsToHistory(Snapshot, Selections);
        }

        internal void RedoSelectionsHistory()
        {
            SetSelectionsFromHistory(historyManager.RedoSelectionHistory);
        }

        internal void UndoSelectionsHistory()
        {
            SetSelectionsFromHistory(historyManager.UndoSelectionHistory);
        }

        private void SetSelectionsFromHistory(Dictionary<int, List<HistorySelection>> selectionHistory)
        {
            if (selectionHistory.TryGetValue(Snapshot.Version.ReiteratedVersionNumber, out var selections))
            {
                Selections = HistoryManager.CreateSelections(Snapshot, selections);
            }
            else
            {
                DiscardSelections();
            }
        }

        internal int GetColumnPosition()
        {
            var lineSource = view.FormattedLineSource;
            return Convert.ToInt32(( view.Caret.Left - lineSource.BaseIndentation ) / lineSource.ColumnWidth);
        }

        internal string GetCurrentlySelectedText()
        {
            return EditorOperations.SelectedText;
        }

        internal bool InsertText(string text)
        {
            return EditorOperations.InsertText(text);
        }

        #region stashed cursors
        internal void StashCurrentCaretPosition()
        {
            StashedCaret = Snapshot.CreateTrackingPoint(view.Caret.Position.BufferPosition);
        }

        internal void ClearStashedCaretPosition()
        {
            StashedCaret = null;
        }

        internal SnapshotPoint ApplyStashedCaretPosition()
        {
            var stashedCaretPosition = StashedCaret.GetPosition(Snapshot);

            var position = view.Caret.Position.BufferPosition;

            view.Caret.MoveTo(StashedCaret.GetPoint(Snapshot));

            if (!Selections.Any(s => s.Caret.GetPoint(Snapshot) == stashedCaretPosition))
            {
                Selections.Add(
                    new Selection
                    {
                        Caret = StashedCaret,
                        ColumnPosition = GetColumnPosition()
                    }
                );
            }
            StashedCaret = null;
            return position;
        }

        internal void MoveToPosition(SnapshotPoint position)
        {
            view.Caret.MoveTo(position);
        }
        #endregion

        /// <summary>
        /// Clears all selections and saves copied text in case of later paste at single-caret
        /// </summary>
        internal void DiscardSelections()
        {
            Selections.Clear();
            view.Caret.IsHidden = false;
        }

        internal void CancelSelectNextOccurence()
        {
            if (ExtensionOptions.Instance.KeepFirstEntry)
            {
                view.Caret.MoveTo(Selections.First().Caret.GetPoint(view.TextSnapshot));
            }
            Selections.Clear();
            view.Caret.IsHidden = false;
        }

        internal void ClearSavedClipboard()
        {
            SavedClipboard = new List<string>();
        }
    }
}
