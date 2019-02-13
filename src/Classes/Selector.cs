using System;
using System.Collections.Generic;
using System.Linq;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
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

        internal readonly IWpfTextView View;

        internal ITextSnapshot Snapshot => this.View.TextSnapshot;

        /// <summary>
        /// Contains all tracking-points for selections and carets
        /// This is what is getting drawn in the adornment layer
        /// </summary>
        internal List<Selection> Selections;

        /// <summary>
        /// Stores copied texts from selections after they are abandoned for later pasting
        /// when back to one caret or across multiple documents
        /// </summary>
        internal static IEnumerable<string> SavedClipboard = new List<string>();

        /// <summary>
        /// The last search-term
        /// </summary>
        internal string SearchText;

        /// <summary>
        /// An indicator wether we are selecting rtl, reversing happens in the CommandTarget
        /// when moving to the left while selecting
        /// </summary>
        internal bool IsReversing = false;

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
        /// <param name="IEditorOperationsFactoryService"></param>
        /// <param name="IEditorFormatMapService"></param>
        /// <param name="ITextStructureNavigator"></param>
        /// <param name="IOutliningManagerService"></param>
        public Selector(
            IWpfTextView view,
            ITextSearchService textSearchService,
            IEditorOperationsFactoryService editorOperationsService,
            IEditorFormatMapService formatMapService = null,
            ITextStructureNavigator textStructureNavigator = null,
            IOutliningManagerService outliningManagerService = null
            )
        {
            this.View = view;

            // Services
            this.textSearchService = textSearchService ?? throw new ArgumentNullException("textSearchService");
            this.EditorOperations = editorOperationsService.GetEditorOperations(this.View);
            this.textStructureNavigator = textStructureNavigator;
            this.outliningManager = outliningManagerService?.GetOutliningManager(this.View);
            this.Dte = ServiceProvider.GlobalProvider.GetService(typeof(DTE)) as DTE2;

            this.Selections = new List<Selection>();
        }

        /// <summary>
        /// Adds start and end position of current selected text. 
        /// Default caret-position is set to the end of the selection, 
        /// caret-position will be moved by the command-filter when editing the text
        /// </summary>
        private void AddCurrentSelectionToSelections()
        {
            var start = View.Selection.Start.Position.Position;
            var end = View.Selection.End.Position.Position;

            var caret = !View.Selection.IsReversed ?
                end : start;

            Selections.Add(
                new Selection
                {
                    Start = Snapshot.CreateTrackingPoint(start, PointTrackingMode.Positive),
                    End = Snapshot.CreateTrackingPoint(end, PointTrackingMode.Positive),
                    Caret = Snapshot.CreateTrackingPoint(caret, PointTrackingMode.Positive),
                    ColumnPosition = GetCurrentColumnPosition(caret)
                }
            );

            Selections.ForEach(s => s.CopiedText = null);

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
                var start = Snapshot.CreateTrackingPoint(occurrence.Start, PointTrackingMode.Positive);
                var end = Snapshot.CreateTrackingPoint(occurrence.End, PointTrackingMode.Positive);

                // If previous selection was reversed, set this caret to beginning of this selection
                var caret = Selections.Last().Caret.GetPosition(Snapshot) == Selections.Last().Start.GetPosition(Snapshot) ?
                    start : end;

                Selections.Add(
                    new Selection
                    {
                        Start = start,
                        End = end,
                        Caret = caret,
                        ColumnPosition = GetCurrentColumnPosition(caret.GetPoint(Snapshot))
                    }
                );

                outliningManager.ExpandAll(occurrence, r => r.IsCollapsed);

                View.Caret.MoveTo(caret == start ? occurrence.Start : occurrence.End);

                View.ViewScroller.EnsureSpanVisible(
                    new SnapshotSpan(
                        View.Caret.Position.BufferPosition,
                        0
                    )
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
                EditorOperations.SelectCurrentWord();

                if (!string.IsNullOrEmpty(EditorOperations.SelectedText))
                    AddCurrentSelectionToSelections();

                IsReversing = false;

                return;
            }

            // First selection is selected by user, future selections will be located and selected on command-invocation
            if (!Selections.Any() && !View.Selection.IsEmpty)
                AddCurrentSelectionToSelections();

            // Multiple selections
            if (Selections.Any())
            {
                // Select words at caret again, this is where we have abandoned selections and goes to carets
                if (Selections.All(s => !s.IsSelection()))
                {
                    var oldSelections = Selections;
                    Selections = new List<Selection>();

                    foreach (var selection in oldSelections)
                    {
                        View.Caret.MoveTo(selection.Caret.GetPoint(Snapshot));
                        EditorOperations.SelectCurrentWord();
                        AddCurrentSelectionToSelections();
                    }
                }
                else
                {
                    // Start the search from previous end-position if it exists, otherwise caret
                    int startIndex;

                    if (reverseDirection)
                    {
                        startIndex = Selections.Last().Start != null ?
                            Selections.Last().Start.GetPosition(Snapshot)
                            : Selections.Last().Caret.GetPosition(Snapshot);
                    }
                    else
                    {
                        startIndex = Selections.Last().End != null ?
                            Selections.Last().End.GetPosition(Snapshot)
                            : Selections.Last().Caret.GetPosition(Snapshot);
                    }

                    var occurrence = textSearchService.FindNext(
                        startIndex,
                        true,
                        GetFindData(reverseDirection, exactMatch)
                    );

                    if (occurrence.HasValue)
                        ProcessFoundOccurrence(occurrence.Value);
                }

                View.Selection.Clear();
            }

            IsReversing = false;
        }

        /// <summary>
        /// Handles finding all occurrences
        /// </summary>
        internal void SelectAllOccurrences()
        {
            // Get a valid first selection to begin with
            SelectNextOccurrence();

            var occurrences = textSearchService.FindAll(GetFindData());
            foreach (var occurrence in occurrences)
                ProcessFoundOccurrence(occurrence);
        }

        internal void ConvertSelectionToMultipleCursors()
        {
            var start = View.Selection.Start.Position.Position;
            var end = View.Selection.End.Position.Position;

            int beginLineNumber = Snapshot.GetLineFromPosition(start).LineNumber;
            int endLineNumber = Snapshot.GetLineFromPosition(end).LineNumber;

            if (beginLineNumber != endLineNumber)
            {
                for (var lineNumber = beginLineNumber; lineNumber < endLineNumber; lineNumber++)
                {
                    var line = Snapshot.GetLineFromLineNumber(lineNumber);
                    Selections.Add(
                        new Selection
                        {
                            Caret = Snapshot.CreateTrackingPoint(line.End.Position, PointTrackingMode.Positive),
                            ColumnPosition = GetCurrentColumnPosition(line.End.Position)
                        }
                    );
                }

                Selections.Add(
                    new Selection
                    {
                        Caret = Snapshot.CreateTrackingPoint(end, PointTrackingMode.Positive),
                        ColumnPosition = GetCurrentColumnPosition(end)
                    }
                );

                Selections.ForEach(s => s.CopiedText = null);
                View.Selection.Clear();
            }
            else
            {
                AddCurrentSelectionToSelections();
            }
        }

        internal void AddCaretAbove()
        {
            foreach (var selecton in Selections.ToList())
            {
                View.Caret.MoveTo(selecton.Caret.GetPoint(Snapshot));
                EditorOperations.MoveLineUp(false);
                AddCurrentCaretToSelections();
            }
        }

        internal void AddCaretBelow()
        {
            foreach (var selecton in Selections.ToList())
            {
                View.Caret.MoveTo(selecton.Caret.GetPoint(Snapshot));
                EditorOperations.MoveLineDown(false);
                AddCurrentCaretToSelections();
            }
        }

        internal void AddCurrentCaretToSelections()
        {
            var caretPosition = View.Caret.Position.BufferPosition.Position;
            if (!Selections.Any(s => s.Caret.GetPoint(Snapshot).Position == caretPosition)
            )
            {
                Selections.Add(
                    new Selection
                    {
                        Start = null,
                        End = null,
                        Caret = Snapshot.CreateTrackingPoint(
                            caretPosition,
                            PointTrackingMode.Positive
                        ),
                        ColumnPosition = GetCurrentColumnPosition(caretPosition)
                    }
                );
            }

            Selections.ForEach(s => s.CopiedText = null);
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

        #region stashed cursors
        internal void StashCurrentCaretPosition()
        {
            StashedCaret = Snapshot.CreateTrackingPoint(
                            View.Caret.Position.BufferPosition.Position,
                            PointTrackingMode.Positive
                        );
        }

        internal void ClearStashedCaretPosition()
        {
            StashedCaret = null;
        }

        internal void ApplyStashedCaretPosition()
        {
            var stashedCaretPosition = StashedCaret.GetPoint(Snapshot).Position;
            if (!Selections.Any(s => s.Caret.GetPoint(Snapshot).Position == stashedCaretPosition))
            {
                Selections.Add(
                    new Selection
                    {
                        Start = null,
                        End = null,
                        Caret = StashedCaret,
                        ColumnPosition = GetCurrentColumnPosition(stashedCaretPosition)
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
            if (Selections.Any() && Selections.All(s => !string.IsNullOrEmpty(s.CopiedText)))
            {
                SavedClipboard = Selections
                    .Where(s => !string.IsNullOrEmpty(s.CopiedText))
                    .Select(s => s.CopiedText).ToList();
            }

            Selections.Clear();
        }

        internal void ClearSavedClipboard()
        {
            SavedClipboard = new List<string>();
        }

        private int GetCurrentColumnPosition(int caretPosition)
        {
            var snapshotLine = Snapshot.GetLineFromPosition(caretPosition);
            return caretPosition - snapshotLine.Start.Position;
        }
    }
}
