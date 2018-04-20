using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;
using NextOccurrence.Options;

namespace NextOccurrence
{
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", Vsix.Version, IconResourceID = 400)] // Info on this package for Help/About
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideOptionPage(typeof(ExtensionOptions), Vsix.Name, "Mouse options", 0, 0, true)]
    [Guid(PackageGuids.guidNextOccurrenceCommandsPackageString)]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    public sealed class VSPackage : Package
    {
        protected override void Initialize()
        {
            NextOccurrenceCommands.Initialize(this);
            base.Initialize();
        }
    }
}
