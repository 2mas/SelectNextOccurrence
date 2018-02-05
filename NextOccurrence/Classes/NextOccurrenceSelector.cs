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

namespace NextOccurrence
{
    /// <summary>
    /// Class responsible of keeping and making the selections
    /// </summary>
    internal class NextOccurrenceSelector
    {
        #region #services
        private readonly ITextSearchService textSearchService;

        private IEditorOperations editorOperations;

        /// <summary>
        /// In case of case-sensitive search this is provided to FindData
        /// </summary>
        private readonly ITextStructureNavigator textStructureNavigator;

        /// <summary>
        /// The top level in the Visual Studio automation object model.
        /// Needed to get the find-object to determine search-options
        /// </summary>
        internal readonly DTE2 Dte;
        #endregion

        internal readonly IWpfTextView view;

        internal ITextSnapshot Snapshot { get { return this.view.TextSnapshot; } }

        /// <summary>
        /// Contains all tracking-points for selections and carets
        /// This is what is getting drawn in the adornment layer
        /// </summary>
        internal List<NextOccurrenceSelection> Selections;

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
        /// Initializes a new instance of the <see cref="NextOccurrenceSelector"/> class.
        /// </summary>
        /// <param name="view"></param>
        /// <param name="textSearchService"></param>
        /// <param name="IEditorOperationsFactoryService"></param>
        /// <param name="IEditorFormatMapService"></param>
        /// <param name="ITextStructureNavigator"></param>
        public NextOccurrenceSelector(
            IWpfTextView view,
            ITextSearchService textSearchService,
            IEditorOperationsFactoryService editorOperationsService,
            IEditorFormatMapService formatMapService = null,
            ITextStructureNavigator textStructureNavigator = null
            )
        {
            this.view = view;

            // Services
            this.textSearchService = textSearchService ?? throw new ArgumentNullException("textSearchService");
            this.editorOperations = editorOperationsService.GetEditorOperations(this.view);
            this.textStructureNavigator = textStructureNavigator;
            this.Dte = ServiceProvider.GlobalProvider.GetService(typeof(DTE)) as DTE2;

            this.Selections = new List<NextOccurrenceSelection>();
        }

        internal void AddCaretAbove()
        {
            editorOperations.MoveLineUp(false);
            AddCurrentCaretToSelections();
        }

        internal void AddCaretBelow()
        {
            editorOperations.MoveLineDown(false);
            AddCurrentCaretToSelections();
        }

        /// <summary>
        /// Adds start and end position of current selected text. 
        /// Default caret-position is set to the end of the selection, 
        /// caret-position will be moved by the command-filter when editing the text
        /// </summary>
        private void AddCurrentSelectionToSelections()
        {
            var start = view.Selection.Start.Position.Position;
            var end = view.Selection.End.Position.Position;

            var caret = !view.Selection.IsReversed ?
                end : start;

            Selections.Add(
                new NextOccurrenceSelection
                {
                    Start = Snapshot.CreateTrackingPoint(start, PointTrackingMode.Positive),
                    End = Snapshot.CreateTrackingPoint(end, PointTrackingMode.Positive),
                    Caret = Snapshot.CreateTrackingPoint(caret, PointTrackingMode.Positive)
                }
            );

            Selections.ForEach(s => s.CopiedText = null);

            SearchText = editorOperations.SelectedText;
        }


        /// <summary>
        /// Handles finding occurrences, selecting and adding to current selections
        /// </summary>
        internal void SelectNextOccurrence()
        {
            // Caret placed on a word, but nothing selected
            if (!Selections.Any() && view.Selection.IsEmpty)
            {
                editorOperations.SelectCurrentWord();

                if (!String.IsNullOrEmpty(editorOperations.SelectedText))
                    AddCurrentSelectionToSelections();

                IsReversing = false;

                return;
            }

            // First selection is selected by user, future selections will be located and selected on command-invocation
            if (!Selections.Any() && !view.Selection.IsEmpty)
                AddCurrentSelectionToSelections();

            // Multiple selections
            if (Selections.Any())
            {
                // Select words at caret again, this is where we have abandoned selections and goes to carets
                if (Selections.All(s => !s.IsSelection()))
                {
                    var oldSelections = Selections;
                    Selections = new List<NextOccurrenceSelection>();

                    foreach (var selection in oldSelections)
                    {
                        view.Caret.MoveTo(selection.Caret.GetPoint(Snapshot));
                        editorOperations.SelectCurrentWord();
                        AddCurrentSelectionToSelections();
                    }
                }
                else
                {
                    // Start the search from previous end-position if it exists, otherwise caret
                    // If user has toggled match-case in their find-options we use this here too
                    var next_occurrence = textSearchService.FindNext(
                        Selections.Last().End != null ?
                            Selections.Last().End.GetPosition(Snapshot)
                            : Selections.Last().Caret.GetPosition(Snapshot),
                        true,
                        (textStructureNavigator != null && Dte.Find.MatchCase) ?
                        new FindData(
                            SearchText,
                            Snapshot,
                            FindOptions.MatchCase,
                            textStructureNavigator
                        )
                        : new FindData(SearchText, Snapshot)
                    );

                    if (next_occurrence.HasValue && !Selections.Any(s => s.OverlapsWith(next_occurrence.Value, Snapshot)))
                    {
                        var start = Snapshot.CreateTrackingPoint(next_occurrence.Value.Start, PointTrackingMode.Positive);
                        var end = Snapshot.CreateTrackingPoint(next_occurrence.Value.End, PointTrackingMode.Positive);

                        // If previous selection was reversed, set this caret to beginning of this selection
                        var caret = Selections.Last().Caret.GetPosition(Snapshot) == Selections.Last().Start.GetPosition(Snapshot) ?
                            start : end;

                        Selections.Add(
                            new NextOccurrenceSelection
                            {
                                Start = start,
                                End = end,
                                Caret = caret
                            }
                        );

                        view.Caret.MoveTo(caret == start ? next_occurrence.Value.Start : next_occurrence.Value.End);
                        view.ViewScroller.EnsureSpanVisible(
                            new SnapshotSpan(
                                view.Caret.Position.BufferPosition,
                                0
                            )
                        );
                    }
                }

                view.Selection.Clear();
            }

            IsReversing = false;
        }

        internal void AddCurrentCaretToSelections()
        {
            if (!Selections.Any(
                s => s.Caret.GetPoint(Snapshot).Position
                == view.Caret.Position.BufferPosition.Position)
            )
            {
                Selections.Add(
                    new NextOccurrenceSelection
                    {
                        Start = null,
                        End = null,
                        Caret = Snapshot.CreateTrackingPoint(
                            view.Caret.Position.BufferPosition.Position,
                            PointTrackingMode.Positive
                        )
                    }
                );
            }

            Selections.ForEach(s => s.CopiedText = null);
        }

        internal void HandleClick(bool addCursor)
        {
            if (addCursor)
            {
                AddCurrentCaretToSelections();
            }
            else
            {
                Selections.Clear();
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
                .Where(s => s.Count() > 1).Any())
            {
                var distinctSelections = Selections
                    .GroupBy(s => s.Caret.GetPoint(Snapshot).Position)
                    .Select(s => s.First()).ToList();

                Selections = distinctSelections;
            }
        }
    }
}
