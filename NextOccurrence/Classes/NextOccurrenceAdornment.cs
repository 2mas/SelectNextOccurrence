using System.Linq;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using NextOccurrence.Commands;

namespace NextOccurrence
{
    /// <summary>
    /// Class responsible of drawing selections and carets to the textView
    /// This is the main class for this extension
    /// </summary>
    internal sealed class NextOccurrenceAdornment
    {
        #region members

        /// <summary>
        /// The layer of the adornment.
        /// </summary>
        private readonly IAdornmentLayer layer;

        private readonly IWpfTextView view;

        internal ITextSnapshot Snapshot { get { return this.view.TextSnapshot; } }

        private Brush caretBrush;

        private Brush selectionBrush;

        internal NextOccurrenceSelector Selector;

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
            IEditorFormatMapService formatMapService = null,
            ITextStructureNavigator textStructureNavigator = null
            )
        {
            view.Properties.GetOrCreateSingletonProperty(
                typeof(NextOccurrenceAdornment), () => this
            );

            this.view = view;
            this.layer = view.GetAdornmentLayer("NextOccurrenceAdornment");

            this.Selector = new NextOccurrenceSelector(
                view,
                textSearchService,
                editorOperationsService,
                formatMapService,
                textStructureNavigator
            );

            this.SetupBrushes(formatMapService);

            // events
            this.view.LayoutChanged += this.OnLayoutChanged;

            NextOccurrenceCommands.OnConvertSelectionToMultipleCursorsPressed += new CmdConvertSelectionToMultipleCursors(view).OnCommandInvoked;
            NextOccurrenceCommands.OnSelectNextOccurrencePressed += new CmdSelectNextOccurrence(view).OnCommandInvoked;
            NextOccurrenceCommands.OnSelectPreviousOccurrencePressed += new CmdSelectPreviousOccurrence(view).OnCommandInvoked;
            NextOccurrenceCommands.OnSkipOccurrencePressed += new CmdSkipOccurrence(view).OnCommandInvoked;
            NextOccurrenceCommands.OnUndoOccurrencePressed += new CmdUndoOccurrence(view).OnCommandInvoked;
            NextOccurrenceCommands.OnAddCaretAbovePressed += new CmdAddCaretAbove(view).OnCommandInvoked;
            NextOccurrenceCommands.OnAddCaretBelowPressed += new CmdAddCaretBelow(view).OnCommandInvoked;
            NextOccurrenceCommands.OnSelectAllOccurrencesPressed += new CmdSelectAllOccurrences(view).OnCommandInvoked;
        }

        /// <summary>
        /// Gets the colors from Options/Environment/Fonts and colors.
        /// Default values provided in case the service doesnt exist
        /// </summary>
        /// <param name="formatMapService"></param>
        private void SetupBrushes(IEditorFormatMapService formatMapService = null)
        {
            if (formatMapService == null)
            {
                caretBrush = new SolidColorBrush(Color.FromRgb(220, 220, 220));
                selectionBrush = new SolidColorBrush(Color.FromArgb(120, 51, 153, 255));
                return;
            }

            IEditorFormatMap formatMap = formatMapService.GetEditorFormatMap(view);

            var dictPlainText = formatMap.GetProperties("Plain Text");
            caretBrush = (SolidColorBrush)dictPlainText[EditorFormatDefinition.ForegroundBrushId];

            var dictSelectedText = formatMap.GetProperties("Selected Text");
            var b = (SolidColorBrush)dictSelectedText[EditorFormatDefinition.BackgroundBrushId];

            selectionBrush = new SolidColorBrush(Color.FromArgb(120, b.Color.R, b.Color.G, b.Color.B));
        }

        internal void OnLayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
        {
            DrawAdornments();
        }

        #region drawing
        internal void DrawAdornments()
        {
            layer.RemoveAllAdornments();

            if (Selector.Selections.Any())
            {
                foreach (var selection in Selector.Selections)
                {
                    if (selection.Start != null && selection.End != null)
                        DrawSelection(selection);

                    DrawCaret(selection.Caret);
                }
            }
        }

        private void DrawCaret(ITrackingPoint caretPoint)
        {
            if (caretPoint.GetPosition(Snapshot) >= Snapshot.Length)
            {
                return;
            }

            var span = new SnapshotSpan(caretPoint.GetPoint(Snapshot), 1);
            Geometry geometry = view.TextViewLines.GetLineMarkerGeometry(span);

            if (geometry != null)
            {
                var drawing = new GeometryDrawing(
                    caretBrush,
                    null,
                    geometry
                );

                Rectangle rectangle = new Rectangle()
                {
                    Fill = caretBrush,
                    Width = drawing.Bounds.Width / 6,
                    Height = drawing.Bounds.Height,
                    Margin = new System.Windows.Thickness(0, 0, 0, 0),
                };

                Canvas.SetLeft(rectangle, geometry.Bounds.Left);
                Canvas.SetTop(rectangle, geometry.Bounds.Top);

                layer.AddAdornment(AdornmentPositioningBehavior.TextRelative, span, "SelectNextOccurrence", rectangle, null);
            }
        }

        private void DrawSelection(NextOccurrenceSelection selection)
        {
            Geometry geometry = view.TextViewLines.GetMarkerGeometry(
                    new SnapshotSpan(
                        selection.Start.GetPoint(Snapshot),
                        selection.End.GetPoint(Snapshot)
                    )
                );

            if (geometry != null)
            {
                var drawing = new GeometryDrawing(
                    selectionBrush,
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
                    selection.Start.GetPoint(Snapshot),
                    selection.End.GetPoint(Snapshot)
                );

                layer.AddAdornment(AdornmentPositioningBehavior.TextRelative, span, "SelectNextOccurrence", image, null);
            }
        }
        #endregion


    }
}
