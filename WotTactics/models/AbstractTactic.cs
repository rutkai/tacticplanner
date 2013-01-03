using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
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

		public virtual void setMap(string id) {
			this.map = maps.getMap(id);
		}

		public abstract Bitmap getTacticAt(int time);
		public abstract Bitmap getPlayTacticAt(int time);
		public abstract void save(Package zip);
		public abstract void load(Package zip);

	}
}
