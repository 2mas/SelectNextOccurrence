using System;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Shell;

namespace NextOccurrence
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class NextOccurrenceCommands
    {
        private readonly Package package;

        /// <summary>
        /// Initializes a new instance of the <see cref="NextOccurrenceCommands"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        private NextOccurrenceCommands(Package package)
        {
            if (package == null)
            {
                throw new ArgumentNullException("package");
            }

            this.package = package;

            OleMenuCommandService commandService = this.ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService != null)
            {
                var ConvertSelectionToMultipleCursorsCmd = new MenuCommand(
                    this.ConvertSelectionToMultipleCursorsCallback,
                    new CommandID(PackageGuids.guidNextOccurrenceCommandsPackageCmdSet, PackageIds.ConvertSelectiontoMultipleCursorsCommandId)
                );

                commandService.AddCommand(ConvertSelectionToMultipleCursorsCmd);

                var SelectNextOccurrenceCmd = new MenuCommand(
                    this.SelectNextOccurrenceCallback,
                    new CommandID(PackageGuids.guidNextOccurrenceCommandsPackageCmdSet, PackageIds.SelectNextOccurrenceCommandId)
                );

                commandService.AddCommand(SelectNextOccurrenceCmd);

                var SkipOccurrenceCmd = new MenuCommand(
                    this.SkipOccurrenceCallback,
                    new CommandID(PackageGuids.guidNextOccurrenceCommandsPackageCmdSet, PackageIds.SkipOccurrenceCommandId)
                );

                commandService.AddCommand(SkipOccurrenceCmd);

                var UndoOccurrenceCmd = new MenuCommand(
                    this.UndoOccurrenceCallback,
                    new CommandID(PackageGuids.guidNextOccurrenceCommandsPackageCmdSet, PackageIds.UndoOccurrenceCommandId)
                );

                commandService.AddCommand(UndoOccurrenceCmd);

                var AddCaretAboveCmd = new MenuCommand(
                    this.AddCaretAboveCallback,
                    new CommandID(PackageGuids.guidNextOccurrenceCommandsPackageCmdSet, PackageIds.AddCaretAboveCommandId)
                );

                commandService.AddCommand(AddCaretAboveCmd);

                var AddCaretBelowCmd = new MenuCommand(
                    this.AddCaretBelowCallback,
                    new CommandID(PackageGuids.guidNextOccurrenceCommandsPackageCmdSet, PackageIds.AddCaretBelowCommandId)
                );

                commandService.AddCommand(AddCaretBelowCmd);

                var SelectAllOccurrencesCmd = new MenuCommand(
                    this.SelectAllOccurrencesCallback,
                    new CommandID(PackageGuids.guidNextOccurrenceCommandsPackageCmdSet, PackageIds.SelectAllOccurrencesCommandId)
                );

                commandService.AddCommand(SelectAllOccurrencesCmd);
            }
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static NextOccurrenceCommands Instance
        {
            get;
            private set;
        }


        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private IServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static void Initialize(Package package)
        {
            Instance = new NextOccurrenceCommands(package);
        }

        private void ConvertSelectionToMultipleCursorsCallback(object sender, EventArgs e)
        {
            OnConvertSelectionToMultipleCursorsPressed?.Invoke(this, e);
        }

        private void SelectNextOccurrenceCallback(object sender, EventArgs e)
        {
            OnSelectNextOccurrencePressed?.Invoke(this, e);
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
        internal static event EventHandler OnSkipOccurrencePressed;
        internal static event EventHandler OnUndoOccurrencePressed;
        internal static event EventHandler OnAddCaretAbovePressed;
        internal static event EventHandler OnAddCaretBelowPressed;
        internal static event EventHandler OnSelectAllOccurrencesPressed;
    }
}
