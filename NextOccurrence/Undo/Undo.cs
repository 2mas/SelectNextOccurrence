using System;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using NextOccurrence.Commands;

namespace NextOccurrence.Undo
{
    class UndoUnit : IOleUndoUnit
    {
        private readonly CommandTarget commandTarget;
        private readonly uint editorAction;
        private Guid editorGuid;

        /// <summary>
        /// Wraps several undo's for multi-edit purposes
        /// </summary>
        /// <param name="commandTarget"></param>
        /// <param name="editorAction">What should be carried out to the Exec-function, a typed char should be removed etc.</param>
        public UndoUnit(CommandTarget commandTarget, uint editorAction)
        {
            this.commandTarget = commandTarget;
            this.editorAction = editorAction;
            this.editorGuid = typeof(VSConstants.VSStd2KCmdID).GUID;
        }

        public void Do(IOleUndoManager pUndoManager)
        {
            commandTarget.Exec(ref editorGuid, editorAction, 0, new IntPtr(0x00000), new IntPtr(0x00000));
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