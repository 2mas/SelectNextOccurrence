using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Input;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using SelectNextOccurrence.Options;

namespace SelectNextOccurrence
{
    [Export(typeof(IMouseProcessorProvider))]
    [ContentType("text")]
    [TextViewRole(PredefinedTextViewRoles.Editable)]
    [Name("SelectNextOccurrenceMouseProcessorProvider")]
    public class MouseProcessorProvider : IMouseProcessorProvider
    {
        public IMouseProcessor GetAssociatedProcessor(IWpfTextView wpfTextView)
        {
            return new MouseProcessor(wpfTextView);
        }
    }

    internal class MouseProcessor : IMouseProcessor
    {
        private readonly IWpfTextView textView;

        private AdornmentLayer AdornmentLayer => this.textView.Properties
            .GetProperty<AdornmentLayer>(typeof(AdornmentLayer));

        public MouseProcessor(IWpfTextView wpfTextView)
        {
            textView = wpfTextView;
        }

        private bool CheckModifiers()
        {
            return Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt);
        }

        public void PostprocessMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            // Only act on single clicks, not selections
            if (AdornmentLayer != null && textView.Selection.IsEmpty)
            {
                if (ExtensionOptions.Instance.AddMouseCursors && CheckModifiers())
                {
                    if (AdornmentLayer.Selector.StashedCaret != null)
                    {
                        var previousPosition = AdornmentLayer.Selector.ApplyStashedCaretPosition();
                        AdornmentLayer.Selector.MoveToPosition(previousPosition);
                    }
                    AdornmentLayer.Selector.AddMouseCaretToSelections();
                }
                else
                {
                    AdornmentLayer.Selector.DiscardSelections();
                }

                AdornmentLayer.DrawAdornments();
            }

            AdornmentLayer?.Selector.ClearStashedCaretPosition();
        }

        /// <summary>
        /// Stashes the first cursor if no previous selections has been made
        /// Stash gets applied on mouse-up if conditions are met
        /// </summary>
        /// <param name="e"></param>
        public void PreprocessMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            if (!ExtensionOptions.Instance.AddMouseCursors)
                return;

            // Only act on single clicks, not selections
            if (AdornmentLayer != null
                && textView.Selection.IsEmpty
                && AdornmentLayer.Selector.Selections.Count == 0)
            {
                if (CheckModifiers())
                {
                    AdornmentLayer.Selector.StashCurrentCaretPosition();
                }
                else
                {
                    AdornmentLayer.Selector.ClearStashedCaretPosition();
                    AdornmentLayer.Selector.DiscardSelections();
                }
            }
        }

#pragma warning disable S1186 // Methods should not be empty
        public void PostprocessDragEnter(DragEventArgs e)
        {
        }

        public void PostprocessDragLeave(DragEventArgs e)
        {
        }

        public void PostprocessDragOver(DragEventArgs e)
        {
        }

        public void PostprocessDrop(DragEventArgs e)
        {
        }

        public void PostprocessGiveFeedback(GiveFeedbackEventArgs e)
        {
        }

        public void PostprocessMouseDown(MouseButtonEventArgs e)
        {
        }

        public void PostprocessMouseEnter(MouseEventArgs e)
        {
        }

        public void PostprocessMouseLeave(MouseEventArgs e)
        {
        }

        public void PostprocessMouseLeftButtonDown(MouseButtonEventArgs e)
        {
        }

        public void PostprocessMouseMove(MouseEventArgs e)
        {
        }

        public void PostprocessMouseRightButtonDown(MouseButtonEventArgs e)
        {
        }

        public void PostprocessMouseRightButtonUp(MouseButtonEventArgs e)
        {
        }

        public void PostprocessMouseUp(MouseButtonEventArgs e)
        {
        }

        public void PostprocessMouseWheel(MouseWheelEventArgs e)
        {
        }

        public void PostprocessQueryContinueDrag(QueryContinueDragEventArgs e)
        {
        }

        public void PreprocessDragEnter(DragEventArgs e)
        {
        }

        public void PreprocessDragLeave(DragEventArgs e)
        {
        }

        public void PreprocessDragOver(DragEventArgs e)
        {
        }

        public void PreprocessDrop(DragEventArgs e)
        {
        }

        public void PreprocessGiveFeedback(GiveFeedbackEventArgs e)
        {
        }

        public void PreprocessMouseDown(MouseButtonEventArgs e)
        {
        }

        public void PreprocessMouseEnter(MouseEventArgs e)
        {
        }

        public void PreprocessMouseLeave(MouseEventArgs e)
        {
        }

        public void PreprocessMouseLeftButtonUp(MouseButtonEventArgs e)
        {
        }

        public void PreprocessMouseMove(MouseEventArgs e)
        {
        }

        public void PreprocessMouseRightButtonDown(MouseButtonEventArgs e)
        {
        }

        public void PreprocessMouseRightButtonUp(MouseButtonEventArgs e)
        {
        }

        public void PreprocessMouseUp(MouseButtonEventArgs e)
        {
        }

        public void PreprocessMouseWheel(MouseWheelEventArgs e)
        {
        }

        public void PreprocessQueryContinueDrag(QueryContinueDragEventArgs e)
        {
        }
#pragma warning restore S1186 // Methods should not be empty

    }
}
