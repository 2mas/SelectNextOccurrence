using System.Collections.Generic;
using System.Linq;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Text.Outlining;

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
        /// In case of case-sensitive search this is provided to FindData
        /// </summary>
        private readonly ITextStructureNavigator textStructureNavigator;

        /// <summary>
        /// Expands regions if selected text is in this region
        /// </summary>
        private readonly IOutliningManager outliningManager;

        #endregion

        private readonly IWpfTextView View;

        private readonly HistoryManager historyManager;

        private ITextSnapshot Snapshot => View.TextSnapshot;

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

        internal bool HasWrappedDocument { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Selector"/> class.
        /// </summary>
        /// <param name="view"></param>
        /// <param name="textSearchService"></param>
        /// <param name="editorOperationsService"></param>
        /// <param name="textStructureNavigator"></param>
        /// <param name="outliningManagerService"></param>
        public Selector(
            IWpfTextView view,
            ITextSearchService textSearchService,
            IEditorOperationsFactoryService editorOperationsService,
            ITextStructureNavigator textStructureNavigator,
            IOutliningManagerService outliningManagerService)
        {
            this.View = view;

            // Services
            this.textSearchService = textSearchService;
            this.EditorOperations = editorOperationsService.GetEditorOperations(this.View);
            this.textStructureNavigator = textStructureNavigator;
            this.outliningManager = outliningManagerService?.GetOutliningManager(this.View);
            this.Dte = ServiceProvider.GlobalProvider.GetService(typeof(DTE)) as DTE2;

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
            var start = View.Selection.Start.Position;
            var end = View.Selection.End.Position;
            var caret = View.Selection.IsReversed
                ? start : end;

            Selections.Add(
                new Selection
                {
                    Start = Snapshot.CreateTrackingPoint(start),
                    End = Snapshot.CreateTrackingPoint(end),
                    Caret = Snapshot.CreateTrackingPoint(caret),
                    ColumnPosition = Snapshot.GetLineColumnFromPosition(caret),
                    VirtualSpaces = View.Caret.Position.VirtualSpaces
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
            var findData = new FindData(SearchText, Snapshot);

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

                Selections.Add(
                    new Selection
                    {
                        Start = start,
                        End = end,
                        Caret = caret,
                        ColumnPosition = Snapshot.GetLineColumnFromPosition(caret.GetPoint(Snapshot))
                    }
                );

                outliningManager.ExpandAll(occurrence, r => r.IsCollapsed);

                View.Caret.MoveTo(caret == start ? occurrence.Start : occurrence.End);

                View.ViewScroller.EnsureSpanVisible(
                    new SnapshotSpan(View.Caret.Position.BufferPosition, 0)
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
            if (!Selections.Any() && View.Selection.IsEmpty)
            {
                SelectCurrentWord(View.Caret.Position.BufferPosition);
                return;
            }

            // First selection is selected by user, future selections will be located and selected on command-invocation
            if (!Selections.Any() && !View.Selection.IsEmpty)
                AddCurrentSelectionToSelections();

            // Multiple selections
            if (Selections.Any())
            {
                // Select words at caret again, this is where we have abandoned selections and goes to carets
                if (Selections.Any(s => !s.IsSelection()))
                {
                    var oldSelections = Selections;
                    Selections = new List<Selection>();

                    foreach (var selection in oldSelections)
                    {
                        if (!selection.IsSelection())
                        {
                            View.Caret.MoveTo(selection.Caret.GetPoint(Snapshot));
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
                    var orderedSelections = HasWrappedDocument ? Selections : Selections.OrderBy(n => n.Caret.GetPosition(Snapshot)).ToList();
                    var startSelection = reverseDirection && !HasWrappedDocument ? orderedSelections.First() : orderedSelections.Last();

                    var startIndex = reverseDirection ?
                        startSelection.Start?.GetPosition(Snapshot) ?? startSelection.Caret.GetPosition(Snapshot)
                        : startSelection.End?.GetPosition(Snapshot) ?? startSelection.Caret.GetPosition(Snapshot);

                    var occurrence = textSearchService.FindNext(
                        startIndex,
                        true,
                        GetFindData(reverseDirection, exactMatch)
                    );

                    if (occurrence.HasValue)
                    {
                        ProcessFoundOccurrence(occurrence.Value);

                        if (!reverseDirection && Selections.Last().Caret.GetPosition(Snapshot) <
                            Selections.First().Caret.GetPosition(Snapshot))
                            HasWrappedDocument = true;

                        if (reverseDirection && Selections.Last().Caret.GetPosition(Snapshot) >
                            Selections.First().Caret.GetPosition(Snapshot))
                            HasWrappedDocument = true;
                    }
                }

                View.Selection.Clear();
                View.Caret.MoveTo(Selections.Last().Caret.GetPoint(Snapshot));
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

            if (EditorOperations.SelectedText.Length != 0)
            {
                if (!char.IsLetterOrDigit(EditorOperations.SelectedText[0])
                    && Snapshot.GetLineColumnFromPosition(caretPosition) != 0)
                {
                    var previousChar = Snapshot.ToCharArray(caretPosition - 1, 1)[0];

                    if (char.IsLetterOrDigit(previousChar))
                    {
                        View.Caret.MoveTo(caretPosition - 1);
                        EditorOperations.SelectCurrentWord();
                    }
                }
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
            var beginLineNumber = View.Selection.Start.Position.GetContainingLine().LineNumber;
            var endLineNumber = View.Selection.End.Position.GetContainingLine().LineNumber;

            if (beginLineNumber != endLineNumber)
            {
                for (var lineNumber = beginLineNumber; lineNumber < endLineNumber; lineNumber++)
                {
                    var line = Snapshot.GetLineFromLineNumber(lineNumber);
                    Selections.Add(
                        new Selection
                        {
                            Caret = Snapshot.CreateTrackingPoint(line.End.Position),
                            ColumnPosition = Snapshot.GetLineColumnFromPosition(line.End.Position)
                        }
                    );
                }

                Selections.Add(
                    new Selection
                    {
                        Caret = Snapshot.CreateTrackingPoint(View.Selection.End.Position),
                        ColumnPosition = Snapshot.GetLineColumnFromPosition(View.Selection.End.Position)
                    }
                );

                View.Selection.Clear();
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
                View.Caret.MoveTo(selection.GetVirtualPoint(Snapshot));
                EditorOperations.MoveLineUp(false);
                AddCaretMoveToSelections(selection);
            }

            View.Caret.MoveTo(Selections.Last().GetVirtualPoint(Snapshot));
        }

        internal void AddCaretBelow()
        {
            foreach (var selection in Selections.ToList())
            {
                View.Caret.MoveTo(selection.GetVirtualPoint(Snapshot));
                EditorOperations.MoveLineDown(false);
                AddCaretMoveToSelections(selection);
            }

            View.Caret.MoveTo(Selections.Last().GetVirtualPoint(Snapshot));
        }

        internal void AddCaretMoveToSelections(Selection selection)
        {
            var caretPosition = View.Caret.Position.BufferPosition;
            var newSelection = new Selection
            {
                Start = null,
                End = null,
                Caret = Snapshot.CreateTrackingPoint(caretPosition),
                ColumnPosition = selection.ColumnPosition,
                VirtualSpaces = View.Caret.Position.VirtualSpaces
            };

            var newPosition = newSelection.GetCaretColumnPosition(caretPosition, Snapshot);
            newSelection.Caret = Snapshot.CreateTrackingPoint(newPosition);

            if (Selections.Any(s => s.Caret.GetPoint(Snapshot) == newPosition))
                return;

            Selections.Add(newSelection);
        }

        internal void AddCurrentCaretToSelections()
        {
            var caretPosition = View.Caret.Position.BufferPosition;
            if (!Selections.Any(s => s.Caret.GetPosition(Snapshot) == caretPosition))
            {
                Selections.Add(
                    new Selection
                    {
                        Start = null,
                        End = null,
                        Caret = Snapshot.CreateTrackingPoint(caretPosition),
                        ColumnPosition = Snapshot.GetLineColumnFromPosition(caretPosition)
                            + View.Caret.Position.VirtualSpaces,
                        VirtualSpaces = View.Caret.Position.VirtualSpaces
                    }
                );
            }
        }

        internal void AddMouseCaretToSelections()
        {
            var caretPosition = View.Caret.Position.BufferPosition;
            if (!Selections.Any(s => s.Caret.GetPosition(Snapshot) == caretPosition))
            {
                Selections.Add(
                    new Selection
                    {
                        Start = null,
                        End = null,
                        Caret = Snapshot.CreateTrackingPoint(caretPosition),
                        ColumnPosition = Snapshot.GetLineColumnFromPosition(caretPosition)
                    }
                );
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
        /// Stores previous selections in a temporary buffer.
        /// The selections are only saved if SaveSelectionsHistory is called.
        /// </summary>
        internal void StorePreviousSelectionsHistory()
        {
            historyManager.StoreSelectionsHistory(Snapshot, Selections);
        }

        internal void SaveSelectionsHistory()
        {
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
            StashedCaret = Snapshot.CreateTrackingPoint(View.Caret.Position.BufferPosition);
        }

        internal void ClearStashedCaretPosition()
        {
            StashedCaret = null;
        }

        internal void ApplyStashedCaretPosition()
        {
            var stashedCaretPosition = StashedCaret.GetPosition(Snapshot);
            if (!Selections.Any(s => s.Caret.GetPoint(Snapshot) == stashedCaretPosition))
            {
                Selections.Add(
                    new Selection
                    {
                        Start = null,
                        End = null,
                        Caret = StashedCaret,
                        ColumnPosition = Snapshot.GetLineColumnFromPosition(stashedCaretPosition)
                    }
                );
            }

            StashedCaret = null;
        }
        #endregion

        /// <summary>
        /// Clears all selections and saves copied text in case of later paste at single-caret
        /// </summary>
        internal void DiscardSelections()
        {
            Selections.Clear();
            HasWrappedDocument = false;
            View.Caret.IsHidden = false;
        }

        internal void ClearSavedClipboard()
        {
            SavedClipboard = new List<string>();
        }
    }
}
