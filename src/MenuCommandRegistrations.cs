using System;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Shell;

namespace SelectNextOccurrence
{
    /// <summary>
    /// Command handler registrations for commands defined in .vsct
    /// </summary>
    internal sealed class MenuCommandRegistrations
    {
        private readonly Package package;

        /// <summary>
        /// Registers command handlers for commands defined in .vsct
        /// </summary>
        /// <param name="package"></param>
        private MenuCommandRegistrations(Package package)
        {
            this.package = package ?? throw new ArgumentNullException("package");

            if (this.ServiceProvider.GetService(typeof(IMenuCommandService)) is OleMenuCommandService commandService)
            {
                var convertSelectionToMultipleCursorsCmd = new MenuCommand(
                    this.ConvertSelectionToMultipleCursorsCallback,
                    new CommandID(PackageGuids.guidNextOccurrenceCommandsPackageCmdSet, PackageIds.ConvertSelectiontoMultipleCursorsCommandId)
                );

                commandService.AddCommand(convertSelectionToMultipleCursorsCmd);

                var selectNextOccurrenceCmd = new MenuCommand(
                    this.SelectNextOccurrenceCallback,
                    new CommandID(PackageGuids.guidNextOccurrenceCommandsPackageCmdSet, PackageIds.SelectNextOccurrenceCommandId)
                );

                commandService.AddCommand(selectNextOccurrenceCmd);

                var selectNextExactOccurrenceCmd = new MenuCommand(
                    this.SelectNextExactOccurrenceCallback,
                    new CommandID(PackageGuids.guidNextOccurrenceCommandsPackageCmdSet, PackageIds.SelectNextExactOccurrenceCommandId)
                );

                commandService.AddCommand(selectNextExactOccurrenceCmd);

                var selectPreviousExactOccurrenceCmd = new MenuCommand(
                    this.SelectPreviousExactOccurrenceCallback,
                    new CommandID(PackageGuids.guidNextOccurrenceCommandsPackageCmdSet, PackageIds.SelectPreviousExactOccurrenceCommandId)
                );

                commandService.AddCommand(selectPreviousExactOccurrenceCmd);

                var selectPreviousOccurrenceCmd = new MenuCommand(
                    this.SelectPreviousOccurrenceCallback,
                    new CommandID(PackageGuids.guidNextOccurrenceCommandsPackageCmdSet, PackageIds.SelectPreviousOccurrenceCommandId)
                );

                commandService.AddCommand(selectPreviousOccurrenceCmd);

                var skipOccurrenceCmd = new MenuCommand(
                    this.SkipOccurrenceCallback,
                    new CommandID(PackageGuids.guidNextOccurrenceCommandsPackageCmdSet, PackageIds.SkipOccurrenceCommandId)
                );

                commandService.AddCommand(skipOccurrenceCmd);

                var undoOccurrenceCmd = new MenuCommand(
                    this.UndoOccurrenceCallback,
                    new CommandID(PackageGuids.guidNextOccurrenceCommandsPackageCmdSet, PackageIds.UndoOccurrenceCommandId)
                );

                commandService.AddCommand(undoOccurrenceCmd);

                var addCaretAboveCmd = new MenuCommand(
                    this.AddCaretAboveCallback,
                    new CommandID(PackageGuids.guidNextOccurrenceCommandsPackageCmdSet, PackageIds.AddCaretAboveCommandId)
                );

                commandService.AddCommand(addCaretAboveCmd);

                var addCaretBelowCmd = new MenuCommand(
                    this.AddCaretBelowCallback,
                    new CommandID(PackageGuids.guidNextOccurrenceCommandsPackageCmdSet, PackageIds.AddCaretBelowCommandId)
                );

                commandService.AddCommand(addCaretBelowCmd);

                var selectAllOccurrencesCmd = new MenuCommand(
                    this.SelectAllOccurrencesCallback,
                    new CommandID(PackageGuids.guidNextOccurrenceCommandsPackageCmdSet, PackageIds.SelectAllOccurrencesCommandId)
                );

                commandService.AddCommand(selectAllOccurrencesCmd);
            }
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static MenuCommandRegistrations Instance { get; private set; }


        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private IServiceProvider ServiceProvider => this.package;

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static void Initialize(Package package)
        {
            Instance = new MenuCommandRegistrations(package);
        }

        private void ConvertSelectionToMultipleCursorsCallback(object sender, EventArgs e)
        {
            OnConvertSelectionToMultipleCursorsPressed?.Invoke(this, e);
        }

        private void SelectNextOccurrenceCallback(object sender, EventArgs e)
        {
            OnSelectNextOccurrencePressed?.Invoke(this, e);
        }

        private void SelectNextExactOccurrenceCallback(object sender, EventArgs e)
        {
            OnSelectNextExactOccurrencePressed?.Invoke(this, e);
        }

        private void SelectPreviousExactOccurrenceCallback(object sender, EventArgs e)
        {
            OnSelectPreviousExactOccurrencePressed?.Invoke(this, e);
        }

        private void SelectPreviousOccurrenceCallback(object sender, EventArgs e)
        {
            OnSelectPreviousOccurrencePressed?.Invoke(this, e);
        }

        private void SkipOccurrenceCallback(object sender, EventArgs e)
        {
            OnSkipOccurrencePressed?.Invoke(this, e);
        }

        private void UndoOccurrenceCallback(object sender, EventArgs e)
        {
            OnUndoOccurrencePressed?.Invoke(this, e);
        }

        private void AddCaretAboveCallback(object sender, EventArgs e)
        {
            OnAddCaretAbovePressed?.Invoke(this, e);
        }

        private void AddCaretBelowCallback(object sender, EventArgs e)
        {
            OnAddCaretBelowPressed?.Invoke(this, e);
        }

        private void SelectAllOccurrencesCallback(object sender, EventArgs e)
        {
            OnSelectAllOccurrencesPressed?.Invoke(this, e);
        }

        /// <summary>
        /// The events to be raised when commands are invoked
        /// </summary>
        internal static event EventHandler OnConvertSelectionToMultipleCursorsPressed;
        internal static event EventHandler OnSelectNextOccurrencePressed;
        internal static event EventHandler OnSelectNextExactOccurrencePressed;
        internal static event EventHandler OnSelectPreviousExactOccurrencePressed;
        internal static event EventHandler OnSelectPreviousOccurrencePressed;
        internal static event EventHandler OnSkipOccurrencePressed;
        internal static event EventHandler OnUndoOccurrencePressed;
        internal static event EventHandler OnAddCaretAbovePressed;
        internal static event EventHandler OnAddCaretBelowPressed;
        internal static event EventHandler OnSelectAllOccurrencesPressed;
    }
}
