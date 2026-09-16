using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel.Design;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace SolutionProjectExplorerZoomInOutVSIX
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class CmdSolutionExplorerZoomInOut
    {
        const string DIALOG_MESSAGE_PROJECT_NOT_LOADED = "Please load a solution with project before active the fonctionality.";
        const string MESSAGE_BOX_TITLE = "Project Explorer Zoom In/Out";

        
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0100;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("5fa5dc91-a262-44ed-ac49-c621b32665fd");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;

        /// <summary>
        /// Initializes a new instance of the <see cref="CmdSolutionExplorerZoomInOut"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private CmdSolutionExplorerZoomInOut(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static CmdSolutionExplorerZoomInOut Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider
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
        public static async Task InitializeAsync(AsyncPackage package)
        {
            var obj = await package.GetServiceAsync(typeof(IMenuCommandService));
            if (obj != null)
            {
                OleMenuCommandService commandService = obj as OleMenuCommandService;
                Instance = new CmdSolutionExplorerZoomInOut(package, commandService);
            }
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        private void Execute(object sender, EventArgs e)
        {
             ThreadHelper.JoinableTaskFactory.RunAsync(async delegate
            {
                if (!await IsSolutionLoadedWithProjectsAsync())
                {
                    VsShellUtilities.ShowMessageBox(
                        this.package,
                        DIALOG_MESSAGE_PROJECT_NOT_LOADED,
                        MESSAGE_BOX_TITLE,
                        OLEMSGICON.OLEMSGICON_INFO,
                        OLEMSGBUTTON.OLEMSGBUTTON_OK,
                        OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                }
                else
                {
                    var pezInOuthack = new SolutionProjectExplorerZoomInOutHack(this.package);
                    await pezInOuthack.HackSolutionExplorerPanelAsync();
                }
            }).GetAwaiter();
        }

        /// <summary>
        /// Verify that there a open solution with a loaded project
        /// </summary>
        internal async Task<bool> IsSolutionLoadedWithProjectsAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            IVsSolution vsSolution = await package.GetServiceAsync(typeof(SVsSolution)) as IVsSolution;
            if (vsSolution == null) 
                return false;
            
            vsSolution.GetProperty((int)__VSPROPID.VSPROPID_ProjectCount, out object projectCount);
            
            return projectCount != null && (int)projectCount > 0;
        }
    }
}
