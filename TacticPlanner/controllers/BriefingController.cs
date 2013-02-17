using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Threading;
using TacticPlanner.models;

namespace TacticPlanner.controllers {
	class BriefingController {
		private TacticsController tactics;
		private Briefing adapter;

		private DispatcherTimer refresher;

		public BriefingController(TacticsController tactics, Briefing briefing) {
			this.tactics = tactics;
			adapter = briefing;
		}
	}
}
