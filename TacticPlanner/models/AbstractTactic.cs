using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using System.IO.Packaging;

namespace TacticPlanner.models {
	abstract class AbstractTactic {
		public bool timer { get; set; }

		protected Map map;

		public AbstractTactic() {
			timer = true;
		}

		public virtual void setMap(string id, BattleType type = BattleType.Undefined, string variation = "") {
			this.map = Maps.Instance.getMap(id);
			this.map.Battletype = type;
			this.map.Variation = variation;
		}

		public abstract ImageSource getTacticAt(int time);
		public abstract ImageSource getPlayTacticAt(int time);
		public abstract void save(Package zip);
		public abstract void load(Package zip);

	}
}
