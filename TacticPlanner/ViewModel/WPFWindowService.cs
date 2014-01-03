using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

using TacticPlanner.Interfaces;

namespace TacticPlanner.ViewModel {
	public class WPFWindowService : IWindowService {

		protected Window window;

		public WPFWindowService(Window window) {
			this.window = window;
		}

		public void Close() {
			window.Close();
		}

		public void ShowMessageBox(string message, string title = "Tactic planner", MessageBoxButton buttons = MessageBoxButton.OK, MessageBoxImage icon = MessageBoxImage.None) {
			MessageBox.Show(message, title, buttons, icon);
		}

	}
}
