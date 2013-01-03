using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Drawing;
using TacticPlanner.types;

namespace TacticPlanner.models {
    class Map {
        public string id { get; set; }
        public string name { get; set; }
        public string originalFilename { get; set; }
		public string hdFilename { get; set; }

		private Bitmap mapImg;
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

		public Bitmap getMapImage() {
			if (mapImg == null) {
				if (_mapPack == MapPack.Original) {
					mapImg = (Bitmap)Bitmap.FromFile(originalFilename);
				} else {
					mapImg = (Bitmap)Bitmap.FromFile(hdFilename);
				}
			}
			return (Bitmap)mapImg.Clone();
		}
    }
}
