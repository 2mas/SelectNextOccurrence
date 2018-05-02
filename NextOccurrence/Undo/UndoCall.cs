using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.OLE.Interop;

namespace NextOccurrence.Undo
{
    /// <summary>
    /// Main call goes here, and carries out its children in the same call
    /// </summary>
    class UndoCall : IOleUndoUnit
    {
        private readonly List<IOleUndoUnit> children;

        /// <summary>
        /// Wraps several undo's for multi-edit purposes
        /// </summary>
        public UndoCall()
        {
            this.children = new List<IOleUndoUnit>();
        }

        public void Add(IOleUndoUnit unit)
        {
            children.Add(unit);
        }

        /// <summary>
        /// Undoes 
        /// </summary>
        /// <param name="pUndoManager"></param>
        public void Do(IOleUndoManager pUndoManager)
        {
            if (children.Any())
                foreach (var child in children)
                {
                    child.Do(pUndoManager);
                }
        }

        public void GetDescription(out string pBstr)
        {
            pBstr = "Undo multiple edits";
        }

        public void GetUnitType(out Guid pClsid, out int plID)
        {
            throw new NotImplementedException();
        }

        public void OnNextAdd()
        {
        }
    }

}