using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Threading;
using TacticPlanner.models;

namespace TacticPlanner.controllers {
	class BriefingController {
		private Tactic tactic;
		private MainWindow mainWindow;
		private Briefing adapter;

		private DispatcherTimer refresher;

		public BriefingController(MainWindow window, Briefing briefing) {
			mainWindow = window;
			adapter = briefing;
		}

		public void setTactic(Tactic tactic) {
			this.tactic = tactic;
		}
	}
}
