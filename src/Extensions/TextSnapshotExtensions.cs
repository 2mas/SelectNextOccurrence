using Microsoft.VisualStudio.Text;

namespace SelectNextOccurrence
{
    public static class TextSnapshotExtensions
    {
        public static ITrackingPoint CreateTrackingPoint(this ITextSnapshot snapshot, int position)
        {
            return snapshot.CreateTrackingPoint(position, PointTrackingMode.Positive);
        }
    }
}
