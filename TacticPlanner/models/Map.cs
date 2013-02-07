using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Windows.Media;
using System.Windows.Media.Imaging;
using TacticPlanner.types;

namespace TacticPlanner.models {
    class Map {
        public string id { get; set; }
        public string name { get; set; }
        public string originalFilename { get; set; }
		public string hdFilename { get; set; }

		private BitmapImage mapImg;
		private MapPack _mapPack;
		public MapPack mapPack {
			set {
				this._mapPack = value;
				mapImg = null;
			}
		}

        public Map(string _id, string _name = "", string _originalFilename = "", string _hdFilename = "") {
            this.id = _id;
            this.name = _name;
            this.originalFilename = _originalFilename;
			this.hdFilename = _hdFilename;
        }

		public BitmapImage getMapImage() {
			if (mapImg == null) {
				if (_mapPack == MapPack.Original) {
					mapImg = new BitmapImage(new Uri(originalFilename, UriKind.RelativeOrAbsolute));
				} else {
					mapImg = new BitmapImage(new Uri(hdFilename, UriKind.RelativeOrAbsolute));
				}
			}
			return (BitmapImage)mapImg.Clone();
		}
    }
}
