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
        public const int SelectNextOccurrenceCommandId = 0x0100;

        public const int SkipOccurrenceCommandId = 0x0110;

        public const int UndoOccurrenceCommandId = 0x0120;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("66543491-5596-4e6c-94d2-bb507832fd49");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
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
                var SelectNextOccurrenceCmd = new MenuCommand(
                    this.SelectNextOccurrenceCallback,
                    new CommandID(CommandSet, SelectNextOccurrenceCommandId)
                );

                commandService.AddCommand(SelectNextOccurrenceCmd);

                var SkipOccurrenceCmd = new MenuCommand(
                    this.SkipOccurrenceCallback,
                    new CommandID(CommandSet, SkipOccurrenceCommandId)
                );

                commandService.AddCommand(SkipOccurrenceCmd);

                var UndoOccurrenceCmd = new MenuCommand(
                    this.UndoOccurrenceCallback,
                    new CommandID(CommandSet, UndoOccurrenceCommandId)
                );

                commandService.AddCommand(UndoOccurrenceCmd);
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

        /// <summary>
        /// The events to be raised when commands are invoked
        /// </summary>
        internal static event EventHandler OnSelectNextOccurrencePressed;
        internal static event EventHandler OnSkipOccurrencePressed;
        internal static event EventHandler OnUndoOccurrencePressed;
    }
}
