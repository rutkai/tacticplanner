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

		protected Maps maps;
		protected Tanks tanks;
		protected Icons icons;

		protected Map map;

		public AbstractTactic(Maps maps, Tanks tanks, Icons icons) {
			this.maps = maps;
			this.tanks = tanks;
			this.icons = icons;

			timer = true;
		}

		public virtual void setMap(string id, BattleType type = BattleType.Undefined, string variation = "") {
			this.map = maps.getMap(id);
			this.map.Battletype = type;
			this.map.Variation = variation;
		}

		public abstract ImageSource getTacticAt(int time);
		public abstract ImageSource getPlayTacticAt(int time);
		public abstract void save(Package zip);
		public abstract void load(Package zip);

	}
}
