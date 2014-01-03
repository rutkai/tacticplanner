using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using TacticPlanner.ViewModel;

namespace TacticPlanner {
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application {
		public static string[] mArgs;
		public static string ApplicationPath;

		private void Application_Startup(object sender, StartupEventArgs e) {
			mArgs = e.Args;
			ApplicationPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
		}

		protected override void OnStartup(StartupEventArgs e) {
			base.OnStartup(e);

			MainWindow window = new MainWindow();
			TacticPlannerContext context = new TacticPlannerContext(new WPFWindowService(window));

			window.DataContext = context;
			window.Show();
		}
	}
}
