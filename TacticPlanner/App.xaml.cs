using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;

namespace TacticPlanner {
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application {
		public static string[] mArgs;

		private void Application_Startup(object sender, StartupEventArgs e) {
			mArgs = e.Args;
		}
	}
}
