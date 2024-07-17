using Autodesk.Revit.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Automation;
using System.Windows.Forms;

namespace RevitToolKit.TriggerManager.BuiltInController
{
    /// <summary>
    /// Represents a class to control Revit's builtin "Transfer Project Standards" UI dialog functions and its related UI dialogs.
    /// </summary>
    /// <remarks>
    /// Note: Should only run externally within a static main entry point and not through an imported Revit addin structure.
    /// </remarks>
    public class TransferProjectStandards
    {
        /// <summary>
        /// Gets or sets the AutomationElement representing the Revit builtIn function window.
        /// </summary>
        private static AutomationElement Window { get; set; }

        /// <summary>
        /// Selects the Revit builtIn function window by the specified function name.
        /// </summary>
        /// <remarks> 
        /// Note: The function must be called and the dialog must be open in Revit for the window to be found. 
        /// </remarks>
        /// <param name="function">The name of the function to find the window.</param>
        /// <returns>An AutomationElement representing the window, or null if not found.</returns>
        static AutomationElement SelectWindow(string function)
        {
            try
            {
                return GetRevitWindow()?.FindFirst(TreeScope.Children, new PropertyCondition(AutomationElement.NameProperty, function));
            }
            catch { return null; }
        }
        /// <summary>
        /// Selects the specified standards in the Revit "Transfer Project Standards" dialog.
        /// </summary>
        /// <param name="function">The name of the function to find the window.</param>
        /// <param name="standards">An array of standards to select.</param>
        static void SelectStandards(string function, string[] standards)
        {
            try
            {
                Window = SelectWindow(function);
                var listBox = Window?.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.List));

                foreach (string i in standards)
                    ItemSelection(listBox, i);
            }
            catch { }
        }
        /// <summary>
        /// Clicks the specified button in the Revit "Transfer Project Standards" dialog and its related UI dialoges.
        /// </summary>
        /// <param name="function">The name of the function to find the window.</param>
        /// <param name="button">The name of the button to click.</param>
        static void ButtonClick(string function, string button)
        {
            try
            {
                Window = SelectWindow(function);
                ButtonPress(Window, button);
            }
            catch { }
        }
        /// <summary>
        /// Selects the specified item in the list box.
        /// </summary>
        /// <param name="listBox">The AutomationElement representing the list box.</param>
        /// <param name="item">The name of the item to select.</param>
        private static void ItemSelection(AutomationElement listBox, string item)
        {
            var checkboxItem = listBox.FindFirst(TreeScope.Children, new PropertyCondition(AutomationElement.NameProperty, item));

            if (checkboxItem != null)
            {
                SelectionItemPattern selectionPattern = checkboxItem?.GetCurrentPattern(SelectionItemPattern.Pattern) as SelectionItemPattern;

                if (selectionPattern != null)
                {
                    selectionPattern.Select();
                    SendKeys.SendWait(" ");
                    Thread.Sleep(10);
                }
            }
        }
        /// <summary>
        /// Presses the specified button in the dialog.
        /// </summary>
        /// <param name="dialog">The AutomationElement representing the dialog.</param>
        /// <param name="button">The name of the button to press.</param>
        private static void ButtonPress(AutomationElement dialog, string button)
        {
            var dialogButton = dialog.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.NameProperty, button));
            InvokePattern invokePattern = (dialogButton?.GetCurrentPattern(InvokePattern.Pattern) as InvokePattern);
            invokePattern?.Invoke();
            Thread.Sleep(10);
        }
        /// <summary>
        /// Gets the main Revit window.
        /// </summary>
        /// <returns>An AutomationElement representing the main Revit window, or null if not found.</returns>
        private static AutomationElement GetRevitWindow()
        {
            var desktop = AutomationElement.RootElement;
            var processes = desktop.FindAll(TreeScope.Children, Condition.TrueCondition);
            var programName = processes.Cast<AutomationElement>().FirstOrDefault(item => item.Current.Name.Contains("Revit"))?.Current.Name;
            var revitWindow = desktop?.FindFirst(TreeScope.Children, new PropertyCondition(AutomationElement.NameProperty, programName));

            return revitWindow;
        }

        /// <summary>
        /// Mockup of Transfer Project Standards class method operation sequence
        /// </summary>
        private static void Main()
        {
            string window = "Transfer Project Standards";
            string[] standards = new string[] 
            { 
                "Wire Sizes",
                "Wall Types",
                "Text Types",
                "View Templates",
                "Project Parameters"
            };

            // Operate "Transfer Project Standrds" dialog control components
            ButtonClick(window, "Check None");
            SelectStandards(window, standards);
            ButtonClick(window, "OK");

            // Prompt case: Operate "Duplicate Types" dialog control operations
            window = "Duplicate Types";
            ButtonClick(window, "Overwrite"); 
        }
    }
}
