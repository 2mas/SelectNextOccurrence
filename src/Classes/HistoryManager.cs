using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.Text;

namespace SelectNextOccurrence
{
    public class HistoryManager
    {
        internal Dictionary<int, List<HistorySelection>> UndoSelectionHistory { get; }
        internal Dictionary<int, List<HistorySelection>> RedoSelectionHistory { get; }
        internal List<HistorySelection> StoredSelections { get; private set; }
        internal int StoredVersion { get; set; }

        internal HistoryManager()
        {
            this.UndoSelectionHistory = new Dictionary<int, List<HistorySelection>>();
            this.RedoSelectionHistory = new Dictionary<int, List<HistorySelection>>();
        }

        internal void StoreSelectionsHistory(ITextSnapshot snapshot, List<Selection> selections)
        {
            StoredVersion = snapshot.Version.ReiteratedVersionNumber;
            StoredSelections = selections.Select(s =>
                new HistorySelection
                {
                    CaretPosition = s.Caret.GetPosition(snapshot),
                    StartPosition = s.Start?.GetPosition(snapshot),
                    EndPosition = s.End?.GetPosition(snapshot),
                    ColumnPosition = s.ColumnPosition,
                    VirtualSpaces = s.VirtualSpaces
            }).ToList();
        }

        internal void SaveSelectionsToHistory(ITextSnapshot snapshot, List<Selection> selections)
        {
            var newVersion = snapshot.Version.ReiteratedVersionNumber;
            if (newVersion > StoredVersion)
            {
                UndoSelectionHistory[StoredVersion] = StoredSelections;
                StoreSelectionsHistory(snapshot, selections);
                RedoSelectionHistory[StoredVersion] = StoredSelections;
            }
        }

        internal static List<Selection> CreateSelections(ITextSnapshot snapshot, List<HistorySelection> selectionItems)
        {
            return selectionItems.Select(s =>
                new Selection
                {
                    Caret = snapshot.CreateTrackingPoint(s.CaretPosition),
                    Start = s.StartPosition.HasValue ? snapshot.CreateTrackingPoint(s.StartPosition.Value) : null,
                    End = s.EndPosition.HasValue ? snapshot.CreateTrackingPoint(s.EndPosition.Value) : null,
                    ColumnPosition = s.ColumnPosition,
                    VirtualSpaces = s.VirtualSpaces
                }).ToList();
        }

    }
}
