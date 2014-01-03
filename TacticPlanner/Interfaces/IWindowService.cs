using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace TacticPlanner.Interfaces {
	public interface IWindowService {
		/// <summary>
		/// Bezárás
		/// </summary>
		void Close();

		/// <summary>
		/// MessageBox feldobása
		/// </summary>
		/// <param name="message">Üzenet</param>
		/// <param name="title">Cím</param>
		void ShowMessageBox(string message, string title = "Tactic planner", MessageBoxButton buttons = MessageBoxButton.OK, MessageBoxImage icon = MessageBoxImage.None);
	}
}
