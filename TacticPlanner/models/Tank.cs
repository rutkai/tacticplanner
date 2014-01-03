using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace TacticPlanner.models {
	public class Tank : ICloneable {
		public string id { get; set; }
		public string nation { get; set; }
		public string name { get; set; }
		public TankType type { get; set; }
		public string filename { get; set; }

		private BitmapImage tankImg;

		public Tank(string _id, string _nation = "", string _name = "", TankType _type = TankType.Heavy, string _filename = "") {
			this.id = _id;
			this.nation = _nation;
			this.name = _name;
			this.type = _type;
			this.filename = _filename;
		}

		public object Clone() {
			return new Tank(
				this.id,
				this.nation,
				this.name,
				this.type,
				this.filename
			);
		}

		public BitmapImage getImage() {
			if (tankImg == null) {
				tankImg = new BitmapImage(new Uri(filename, UriKind.RelativeOrAbsolute));
			}
			return (BitmapImage)tankImg.Clone();
		}
	}
}
