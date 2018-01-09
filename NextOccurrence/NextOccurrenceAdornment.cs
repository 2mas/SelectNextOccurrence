using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;

namespace NextOccurrence
{
    internal sealed class NextOccurrenceAdornment : IOleCommandTarget
    {
        /// <summary>
        /// Represents a selection or/and a caret-position
        /// </summary>
        private class NextOccurrenceSelection
        {
            internal ITrackingPoint Start { get; set; }
            internal ITrackingPoint End { get; set; }
            internal ITrackingPoint Caret { get; set; }

            internal bool OverlapsWith(SnapshotSpan span, ITextSnapshot snapshot)
            {
                if (this.Start == null || this.End == null)
                {
                    return span.OverlapsWith(
                            new SnapshotSpan(this.Caret.GetPoint(snapshot), 1)
                        );
                }
                else
                {
                    return new SnapshotSpan(
                            this.Start.GetPoint(snapshot), this.End.GetPoint(snapshot)
                        ).OverlapsWith(span);
                }
            }
        }

        #region members

        /// <summary>
        /// The layer of the adornment.
        /// </summary>
        private readonly IAdornmentLayer layer;

        private readonly IWpfTextView view;

        private ITextSnapshot snapshot { get { return this.view.TextSnapshot; } }

        private readonly ITextSearchService textSearchService;

        private IEditorOperations editorOperations;

        private Brush caretBrush;

        private Brush selectionBrush;

        private List<NextOccurrenceSelection> selections;

        private string searchText;

        /// <summary>
        /// Next commandhandler
        /// </summary>
        internal IOleCommandTarget NextCommandTarget { get; set; }

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="NextOccurrenceAdornment"/> class.
        /// </summary>
        /// <param name="view"></param>
        /// <param name="textSearchService"></param>
        /// <param name="IEditorOperationsFactoryService"></param>
        /// <param name="IEditorFormatMapService"></param>
        public NextOccurrenceAdornment(
            IWpfTextView view,
            ITextSearchService textSearchService,
            IEditorOperationsFactoryService editorOperationsService,
            IEditorFormatMapService formatMapService = null)
        {
            if (editorOperationsService == null)
                throw new ArgumentNullException("editorOperationsService");

            this.view = view ?? throw new ArgumentNullException("view");
            this.textSearchService = textSearchService ?? throw new ArgumentNullException("textSearchService");
            this.editorOperations = editorOperationsService.GetEditorOperations(this.view);
            this.layer = view.GetAdornmentLayer("NextOccurrenceAdornment");

            this.SetupBrushes(formatMapService);
            this.selections = new List<NextOccurrenceSelection>();

            // events
            this.view.LayoutChanged += this.OnLayoutChanged;
            NextOccurrenceCommands.OnSelectNextOccurrencePressed += OnSelectNextOccurrencePressed;
        }

        private void SetupBrushes(IEditorFormatMapService formatMapService = null)
        {
            if (formatMapService == null)
            {
                this.caretBrush = new SolidColorBrush(Color.FromRgb(220, 220, 220));
                this.selectionBrush = new SolidColorBrush(Color.FromArgb(120, 51, 153, 255));
                return;
            }

            IEditorFormatMap formatMap = formatMapService.GetEditorFormatMap(this.view);

            var dictPlainText = formatMap.GetProperties("Plain Text");
            this.caretBrush = (SolidColorBrush)dictPlainText[EditorFormatDefinition.ForegroundBrushId];

            var dictSelectedText = formatMap.GetProperties("Selected Text");
            var b = (SolidColorBrush)dictSelectedText[EditorFormatDefinition.BackgroundBrushId];

            this.selectionBrush = new SolidColorBrush(Color.FromArgb(120, b.Color.R, b.Color.G, b.Color.B));
        }

        /// <summary>
        /// Adds start and end position of current selected text. 
        /// Default caret-position is set to the end of the selection, 
        /// caret-position will be moved by the command-filter when editing the text
        /// </summary>
        private void AddCurrentSelectionToSelections()
        {
            var start = this.view.Selection.Start.Position.Position < this.view.Selection.End.Position.Position ?
                this.view.Selection.Start.Position.Position
                : this.view.Selection.End.Position.Position;

            var end = this.view.Selection.Start.Position.Position < this.view.Selection.End.Position.Position ?
                this.view.Selection.End.Position.Position
                : this.view.Selection.Start.Position.Position;

            this.selections.Add(
                new NextOccurrenceSelection
                {
                    Start = this.snapshot.CreateTrackingPoint(start, PointTrackingMode.Positive),
                    End = this.snapshot.CreateTrackingPoint(end, PointTrackingMode.Positive),
                    Caret = this.snapshot.CreateTrackingPoint(end, PointTrackingMode.Positive)
                }
            );

            this.searchText = this.editorOperations.SelectedText;
        }

        #region interactions

        /// <summary>
        /// Menu-command handler, aka Ctrl+D
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnSelectNextOccurrencePressed(object sender, EventArgs e)
        {
            if (!this.view.HasAggregateFocus)
                return;

            // Caret placed on a word, but nothing selected
            if (!this.selections.Any() && this.view.Selection.IsEmpty)
            {
                this.editorOperations.SelectCurrentWord();

                if (!String.IsNullOrEmpty(this.editorOperations.SelectedText))
                    AddCurrentSelectionToSelections();

                return;
            }

            // First selection is selected by user, future selections will be located and selected on command-invocation
            if (!this.selections.Any() && !this.view.Selection.IsEmpty)
                AddCurrentSelectionToSelections();

            // Multiple selections
            if (this.selections.Any())
            {
                //select words at caret again, this is where have abandoned selections
                if (selections.All(s => s.Start == null && s.End == null))
                {
                    var oldSelections = selections;
                    selections = new List<NextOccurrenceSelection>();

                    foreach (var selection in oldSelections)
                    {
                        this.view.Caret.MoveTo(selection.Caret.GetPoint(this.snapshot));
                        this.editorOperations.SelectCurrentWord();
                        AddCurrentSelectionToSelections();
                    }
                }
                else
                {
                    // Start the search from previous end-position if it exists, otherwise caret
                    var next_occurrence = textSearchService.FindNext(
                        this.selections.Last().End != null ?
                            this.selections.Last().End.GetPosition(this.snapshot)
                            : this.selections.Last().Caret.GetPosition(this.snapshot),
                        true,
                        new FindData(searchText, this.snapshot)
                    );

                    if (next_occurrence.HasValue && !selections.Any(s => s.OverlapsWith(next_occurrence.Value, this.snapshot)))
                    {
                        this.selections.Add(
                            new NextOccurrenceSelection
                            {
                                Start = this.snapshot.CreateTrackingPoint(next_occurrence.Value.Start, PointTrackingMode.Positive),
                                End = this.snapshot.CreateTrackingPoint(next_occurrence.Value.End, PointTrackingMode.Positive),
                                Caret = this.snapshot.CreateTrackingPoint(next_occurrence.Value.End, PointTrackingMode.Positive)
                            }
                        );

                        this.view.Caret.MoveTo(next_occurrence.Value.End);
                        this.editorOperations.ScrollLineBottom();
                    }
                }

                this.view.Selection.Clear();

                DrawAdornments();
            }
        }

        internal void HandleClick(bool addCursor)
        {
            if (addCursor)
            {
                this.selections.Add(
                    new NextOccurrenceSelection
                    {
                        Start = null,
                        End = null,
                        Caret = this.snapshot.CreateTrackingPoint(
                            this.view.Caret.Position.BufferPosition.Position,
                            PointTrackingMode.Positive
                        )
                    }
                );
            }
            else
            {
                this.selections.Clear();
            }

            DrawAdornments();
        }

        #endregion

        /// <summary>
        /// Handles whenever the text displayed in the view changes by adding the adornment to any reformatted lines
        /// </summary>
        /// <remarks><para>This event is raised whenever the rendered text displayed in the <see cref="ITextView"/> changes.</para>
        /// <para>It is raised whenever the view does a layout (which happens when DisplayTextLineContainingBufferPosition is called or in response to text or classification changes).</para>
        /// <para>It is also raised whenever the view scrolls horizontally or when its size changes.</para>
        /// </remarks>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        internal void OnLayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
        {
            DrawAdornments();
        }

        #region drawing
        private void DrawAdornments()
        {
            this.layer.RemoveAllAdornments();

            if (selections.Any())
            {
                foreach (var selection in this.selections)
                {
                    if (selection.Start != null && selection.End != null)
                        DrawSelection(selection);

                    DrawCaret(selection.Caret);
                }
            }
        }

        private void DrawCaret(ITrackingPoint caretPoint)
        {
            if (caretPoint.GetPosition(this.snapshot) >= this.snapshot.Length)
            {
                return;
            }

            var span = new SnapshotSpan(caretPoint.GetPoint(this.snapshot), 1);
            Geometry geometry = this.view.TextViewLines.GetLineMarkerGeometry(span);

            if (geometry != null)
            {
                var drawing = new GeometryDrawing(
                    this.caretBrush,
                    null,
                    geometry
                );

                Rectangle rectangle = new Rectangle()
                {
                    Fill = this.caretBrush,
                    Width = drawing.Bounds.Width / 6,
                    Height = drawing.Bounds.Height,
                    Margin = new System.Windows.Thickness(0, 0, 0, 0),
                };

                Canvas.SetLeft(rectangle, geometry.Bounds.Left);
                Canvas.SetTop(rectangle, geometry.Bounds.Top);

                this.layer.AddAdornment(AdornmentPositioningBehavior.TextRelative, span, "SelectNextOccurrence", rectangle, null);
            }
        }

        private void DrawSelection(NextOccurrenceSelection selection)
        {
            Geometry geometry = this.view.TextViewLines.GetMarkerGeometry(
                    new SnapshotSpan(
                        selection.Start.GetPoint(this.snapshot),
                        selection.End.GetPoint(this.snapshot)
                    )
                );

            if (geometry != null)
            {
                var drawing = new GeometryDrawing(
                    this.selectionBrush,
                    new Pen(),
                    geometry
                );

                drawing.Freeze();

                var drawingImage = new DrawingImage(drawing);
                drawingImage.Freeze();

                var image = new System.Windows.Controls.Image
                {
                    Source = drawingImage,
                };

                // Align the image with the top of the bounds of the text geometry
                Canvas.SetLeft(image, geometry.Bounds.Left);
                Canvas.SetTop(image, geometry.Bounds.Top);

                var span = new SnapshotSpan(
                    selection.Start.GetPoint(this.snapshot),
                    selection.End.GetPoint(this.snapshot)
                );

                this.layer.AddAdornment(AdornmentPositioningBehavior.TextRelative, span, "SelectNextOccurrence", image, null);
            }
        }
        #endregion

        #region command_target
        public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            return NextCommandTarget.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
        }

        public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            if (!selections.Any())
                return NextCommandTarget.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);

            int result = VSConstants.S_OK;
            bool modifySelections = false;
            bool clearSelections = false;

            if (pguidCmdGroup == typeof(VSConstants.VSStd2KCmdID).GUID || pguidCmdGroup == VSConstants.GUID_VSStandardCommandSet97)
            {
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
                            break;
                        case ((uint)VSConstants.VSStd2KCmdID.CANCEL):
                            this.selections.Clear();
                            break;
                        case ((uint)VSConstants.VSStd2KCmdID.PAGEDN):
                        case ((uint)VSConstants.VSStd2KCmdID.PAGEUP):
                        case ((uint)VSConstants.VSStd2KCmdID.END):
                        case ((uint)VSConstants.VSStd2KCmdID.HOME):
                        case ((uint)VSConstants.VSStd2KCmdID.END_EXT):
                        case ((uint)VSConstants.VSStd2KCmdID.HOME_EXT):
                            this.selections.Clear();
                            result = NextCommandTarget.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
                            break;
                        case ((uint)VSConstants.VSStd2KCmdID.WORDPREV_EXT):
                        case ((uint)VSConstants.VSStd2KCmdID.WORDNEXT_EXT):
                        case ((uint)VSConstants.VSStd2KCmdID.BOL_EXT):
                        case ((uint)VSConstants.VSStd2KCmdID.EOL_EXT):
                        case ((uint)VSConstants.VSStd2KCmdID.LEFT_EXT):
                        case ((uint)VSConstants.VSStd2KCmdID.RIGHT_EXT):
                        case ((uint)VSConstants.VSStd2KCmdID.UP_EXT):
                        case ((uint)VSConstants.VSStd2KCmdID.DOWN_EXT):
                            // Modify selections
                            modifySelections = true;
                            break;
                    }
                }

                foreach (var selection in selections)
                {
                    if (selection.Start != null && selection.End != null)
                    {
                        this.view.Selection.Select(
                            new SnapshotSpan(
                                selection.Start.GetPoint(this.snapshot),
                                selection.End.GetPoint(this.snapshot)
                            ),
                            false
                        );
                    }

                    this.view.Caret.MoveTo(selection.Caret.GetPoint(this.snapshot));

                    result = NextCommandTarget.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);

                    // Backspace, delete, paste etc
                    if (this.view.Selection.IsEmpty)
                    {
                        selection.Start = null;
                        selection.End = null;
                    }

                    if (modifySelections)
                    {
                        var newSpan = this.view.Selection.StreamSelectionSpan;
                        selection.Start = this.snapshot.CreateTrackingPoint(newSpan.Start.Position.Position, PointTrackingMode.Positive);
                        selection.End = this.snapshot.CreateTrackingPoint(newSpan.End.Position.Position, PointTrackingMode.Positive);
                        this.view.Selection.Clear();
                    }

                    selection.Caret = this.snapshot.CreateTrackingPoint(
                        this.view.Caret.Position.BufferPosition.Position,
                        PointTrackingMode.Positive
                    );
                }

                // set new searchtext needed if selection is modified
                if (modifySelections)
                {
                    this.searchText = this.snapshot.GetText(
                        this.selections.Last().Start.GetPosition(this.snapshot),
                        this.selections.Last().End.GetPosition(this.snapshot) - this.selections.Last().Start.GetPosition(this.snapshot));
                }

                if (clearSelections)
                {
                    selections.ForEach(s =>
                        {
                            s.Start = null;
                            s.End = null;
                        }
                    );
                }

                this.view.Selection.Clear();
            }
            else
            {
                result = NextCommandTarget.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
            }

            DrawAdornments();
            return result;
        }
        #endregion
    }
}
