﻿using System.Linq;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Text.Outlining;
using SelectNextOccurrence.Commands;

namespace SelectNextOccurrence
{
    /// <summary>
    /// Class responsible of drawing selections and carets to the textView
    /// </summary>
    internal sealed class AdornmentLayer
    {
        #region members

        internal Selector Selector;
        internal ITextSnapshot Snapshot => this.view.TextSnapshot;

        private readonly IAdornmentLayer layer;

        private readonly IWpfTextView view;

        private Brush caretBrush;

        private Brush selectionBrush;

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="AdornmentLayer"/> class.
        /// </summary>
        /// <param name="view"></param>
        /// <param name="textSearchService"></param>
        /// <param name="editorOperationsService"></param>
        /// <param name="formatMapService"></param>
        /// <param name="textStructureNavigator"></param>
        /// <param name="outliningManagerService"></param>
        public AdornmentLayer(
            IWpfTextView view,
            ITextSearchService textSearchService,
            IEditorOperationsFactoryService editorOperationsService,
            IEditorFormatMapService formatMapService,
            ITextStructureNavigator textStructureNavigator,
            IOutliningManagerService outliningManagerService
            )
        {
            view.Properties.GetOrCreateSingletonProperty(
                typeof(AdornmentLayer), () => this
            );

            this.view = view;
            this.layer = view.GetAdornmentLayer(Vsix.Name);

            this.Selector = new Selector(
                view,
                textSearchService,
                editorOperationsService,
                textStructureNavigator,
                outliningManagerService
            );

            this.SetupBrushes(formatMapService);

            // events
            this.view.LayoutChanged += this.OnLayoutChanged;

            MenuCommandRegistrations.OnConvertSelectionToMultipleCursorsPressed += new CmdConvertSelectionToMultipleCursors(view).OnCommandInvoked;
            MenuCommandRegistrations.OnSelectNextOccurrencePressed += new CmdSelectNextOccurrence(view).OnCommandInvoked;
            MenuCommandRegistrations.OnSelectNextExactOccurrencePressed += new CmdSelectNextExactOccurrence(view).OnCommandInvoked;
            MenuCommandRegistrations.OnSelectPreviousExactOccurrencePressed += new CmdSelectPreviousExactOccurrence(view).OnCommandInvoked;
            MenuCommandRegistrations.OnSelectPreviousOccurrencePressed += new CmdSelectPreviousOccurrence(view).OnCommandInvoked;
            MenuCommandRegistrations.OnSkipOccurrencePressed += new CmdSkipOccurrence(view).OnCommandInvoked;
            MenuCommandRegistrations.OnUndoOccurrencePressed += new CmdUndoOccurrence(view).OnCommandInvoked;
            MenuCommandRegistrations.OnAddCaretAbovePressed += new CmdAddCaretAbove(view).OnCommandInvoked;
            MenuCommandRegistrations.OnAddCaretBelowPressed += new CmdAddCaretBelow(view).OnCommandInvoked;
            MenuCommandRegistrations.OnSelectAllOccurrencesPressed += new CmdSelectAllOccurrences(view).OnCommandInvoked;
        }

        /// <summary>
        /// Gets the colors from Options/Environment/Fonts and colors.
        /// Default values provided in case the service doesn't exist
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

            var formatMap = formatMapService.GetEditorFormatMap(view);

            var dictPlainText = formatMap.GetProperties("Plain Text");
            caretBrush = (SolidColorBrush) dictPlainText[EditorFormatDefinition.ForegroundBrushId];

            var dictSelectedText = formatMap.GetProperties("Selected Text");
            var b = (SolidColorBrush) dictSelectedText[EditorFormatDefinition.BackgroundBrushId];

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
                if (!view.Caret.IsHidden)
                    view.Caret.IsHidden = true;

                foreach (var selection in Selector.Selections)
                {
                    if (selection.IsSelection())
                        DrawSelection(selection);

                    DrawCaret(selection);
                }
            }
            else
            {
                if (view.Caret.IsHidden)
                    view.Caret.IsHidden = false;
            }
        }

        private void DrawCaret(Selection selection)
        {
            if (selection.Caret.GetPosition(Snapshot) >= Snapshot.Length)
                return;

            var span = new SnapshotSpan(selection.Caret.GetPoint(Snapshot), 1);
            var geometry = view.TextViewLines.GetTextMarkerGeometry(span);

            if (geometry != null)
            {
                var drawing = new GeometryDrawing(
                    caretBrush,
                    null,
                    geometry
                );

                var rectangle = new Rectangle()
                {
                    Fill = caretBrush,
                    Width = drawing.Bounds.Width / 6,
                    Height = drawing.Bounds.Height,
                    Margin = new System.Windows.Thickness(0, 0, 0, 0),
                };

                Canvas.SetLeft(rectangle, geometry.Bounds.Left + (selection.VirtualSpaces * view.TextViewLines[0].VirtualSpaceWidth));
                Canvas.SetTop(rectangle, geometry.Bounds.Top);

                layer.AddAdornment(AdornmentPositioningBehavior.TextRelative, span, Vsix.Name, rectangle, null);
            }
        }

        private void DrawSelection(Selection selection)
        {
            var geometry = view.TextViewLines.GetMarkerGeometry(
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

                var image = new Image
                {
                    Source = drawingImage,
                };

                Canvas.SetLeft(image, geometry.Bounds.Left);
                Canvas.SetTop(image, geometry.Bounds.Top);

                var span = new SnapshotSpan(
                    selection.Start.GetPoint(Snapshot),
                    selection.End.GetPoint(Snapshot)
                );

                layer.AddAdornment(AdornmentPositioningBehavior.TextRelative, span, Vsix.Name, image, null);
            }
        }
        #endregion
    }
}
