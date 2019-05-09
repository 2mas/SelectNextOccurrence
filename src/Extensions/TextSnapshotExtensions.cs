using Microsoft.VisualStudio.Text;

namespace SelectNextOccurrence
{
    public static class TextSnapshotExtensions
    {
        public static ITrackingPoint CreateTrackingPoint(this ITextSnapshot snapshot, int position)
        {
            return snapshot.CreateTrackingPoint(position, PointTrackingMode.Positive);
        }

        public static int GetLineColumnFromPosition(this ITextSnapshot snapshot, int caretPosition)
        {
            var snapshotLine = snapshot.GetLineFromPosition(caretPosition);
            return caretPosition - snapshotLine.Start.Position;
        }
    }
}
