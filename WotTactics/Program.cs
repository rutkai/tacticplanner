using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace TacticPlanner
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            try {
                Application.Run(new gui.Tactics());
            } catch (Exception ex) {
                MessageBox.Show("Critical error! Application terminated. Message: " + ex.Message, "Critical error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
            }
        }
    }
}
