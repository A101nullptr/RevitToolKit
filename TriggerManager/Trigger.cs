using Autodesk.Revit.UI;

namespace RevitToolKit.TriggerManager
{
    /// <summary>
    /// Represents a class for triggering Revit commands.
    /// </summary>
    public class Trigger
    {
        /// <summary>
        /// The UIApplication instance used to interact with the Revit application.
        /// </summary>
        UIApplication App { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Trigger"/> class with the specified UIApplication instance.
        /// </summary>
        /// <param name="app">The UIApplication instance representing the current Revit application.</param>
        public Trigger(UIApplication app)
        {
            App = app;
        }

        /// <summary>
        /// Calls the Revit command identified by the specified command ID.
        /// </summary>
        /// <param name="commandId">The string identifier of the Revit command.</param>
        public void CallCommand(string commandId)
        {
            try
            {
                var id = RevitCommandId.LookupCommandId(commandId);
                if (id != null)
                    App.PostCommand(id);
            }
            catch { }
        }

        /// <summary>
        /// Calls the specified Revit command.
        /// </summary>
        /// <param name="command">The PostableCommand representing the Revit command.</param>
        public void CallCommand(PostableCommand command)
        {
            try
            {
                var id = RevitCommandId.LookupPostableCommandId(command);
                if (id != null)
                    App.PostCommand(id);
            }
            catch { }
        }
    }
}
