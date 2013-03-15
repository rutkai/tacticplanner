using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Controls;
using TacticPlanner.types;

namespace TacticPlanner.models {
	class MapIcon {
		public StaticIcon icon { get; set; }
		public BattleType battle { get; set; }
		public string variation { get; set; }

		public MapIcon(StaticIcon icon, BattleType battle = BattleType.Normal, string variation = "") {
			this.icon = icon;
			this.battle = battle;
			this.variation = variation;
		}
	}

    class Map {
        public string id { get; set; }
        public string name { get; set; }
        public string originalFilename { get; set; }
		public string hdFilename { get; set; }

		private int _iconsSize;
		public int iconsSize {
			set {
				this._iconsSize = value;
				mapImg = null;
			}
		}

		protected MapIcon[] presets;
		private BattleType _type;
		public BattleType Battletype {
			get {
				return _type;
			}
			set {
				_type = value;
				mapImg = null;
			}
		}
		private string _variation;
		public string Variation {
			get {
				return _variation;
			}
			set {
				_variation = value;
				mapImg = null;
			}
		}

		private BitmapSource mapImg;
		private MapPack _mapPack;
		public MapPack mapPack {
			set {
				this._mapPack = value;
				mapImg = null;
			}
		}

		public Map(string _id, string _name = "", string _originalFilename = "", string _hdFilename = "", MapIcon[] presets = null) {
            this.id = _id;
            this.name = _name;
            this.originalFilename = _originalFilename;
			this.hdFilename = _hdFilename;
			iconsSize = 50;
			this.presets = presets;
			_type = BattleType.Undefined;
			_variation = "";
        }

		public BitmapSource getMapImage() {
			if (mapImg == null) {
				if (_mapPack == MapPack.Original) {
					mapImg = new BitmapImage(new Uri(originalFilename, UriKind.RelativeOrAbsolute));
				} else {
					mapImg = new BitmapImage(new Uri(hdFilename, UriKind.RelativeOrAbsolute));
				}
				if (_type != BattleType.Undefined) {
					DrawingGroup group = new DrawingGroup();
					group.Children.Add(new ImageDrawing(mapImg, new Rect(0, 0, mapImg.PixelWidth, mapImg.PixelHeight)));
					for (int i = 0; i < presets.Length; i++) {
						if (presets[i].battle == _type && presets[i].variation == _variation) {
							BitmapImage icon = presets[i].icon.getImage();
							group.Children.Add(new ImageDrawing(icon, new Rect(presets[i].icon.position.X - _iconsSize / 2, presets[i].icon.position.Y - _iconsSize / 2, _iconsSize, (_iconsSize * icon.Height) / icon.Width)));
						}
					}
					group.ClipGeometry = new RectangleGeometry(new Rect(0, 0, mapImg.PixelWidth, mapImg.PixelHeight));
					group.Freeze();
					var image = new Image { Source = new DrawingImage(group) };
					var bitmap = new RenderTargetBitmap(mapImg.PixelWidth, mapImg.PixelHeight, 96, 96, PixelFormats.Pbgra32);
					image.Arrange(new Rect(0, 0, mapImg.PixelWidth, mapImg.PixelHeight));
					bitmap.Render(image);
					mapImg = BitmapFactory.ConvertToPbgra32Format(bitmap);
				}
			}
			return (BitmapSource)mapImg.Clone();
		}
    }
}
