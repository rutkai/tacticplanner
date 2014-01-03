using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using TacticPlanner.Interfaces;

namespace TacticPlanner.ViewModel {
	public class TacticPlannerContext : ViewModelBase {

		protected IWindowService WindowService;

		public TacticPlannerContext(IWindowService service) {
			this.WindowService = service;
		}



	}
}
