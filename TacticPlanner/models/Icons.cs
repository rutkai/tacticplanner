using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace TacticPlanner.models {
    public class StaticIcon : ICloneable {
        public string id { get; set; }
        public string name { get; set; }
        public string filename { get; set; }
        public Point position { get; set; }

		private BitmapImage iconImg;

        public StaticIcon(string _id, string _name = "", string _filename = "") {
            id = _id;
            name = _name;
            filename = _filename;
            position = new Point(500, 500);
        }

		public BitmapImage getImage() {
			if (iconImg == null) {
				iconImg = new BitmapImage(new Uri(filename, UriKind.RelativeOrAbsolute));
			}
			return (BitmapImage)iconImg.Clone();
		}

		#region ICloneable Members

		public object Clone() {
			StaticIcon clone = new StaticIcon(
				this.id,
				this.name,
				this.filename
				);
			clone.position = new Point(this.position.X, this.position.Y);
			return clone;
		}

		#endregion
    }

    public class DynamicIcon {
        public string id { get; set; }
        public string name { get; set; }
		public string filename { get; set; }

		private BitmapImage iconImg;

        public DynamicIcon(string _id, string _name = "", string _filename = "") {
            id = _id;
            name = _name;
            filename = _filename;
        }

		public BitmapImage getImage() {
			if (iconImg == null) {
				iconImg = new BitmapImage(new Uri(filename, UriKind.RelativeOrAbsolute));
			}
			return (BitmapImage)iconImg.Clone();
		}
    }

    public class TankIcon {
        public string id { get; set; }
        public bool isAlly { get; set; }
        public TankType type { get; set; }
        public string aliveFilename { get; set; }
        public string deadFilename { get; set; }

		private BitmapImage aliveIconImg;
		private BitmapImage deadIconImg;

        public TankIcon(string _id, bool _isAlly = true, TankType _type = TankType.Heavy, string _aliveFilename = "", string _deadFilename = "") {
            id = _id;
            isAlly = _isAlly;
            type = _type;
            aliveFilename = _aliveFilename;
            deadFilename = _deadFilename;
        }

		public BitmapImage getAliveImage() {
			if (aliveIconImg == null) {
				aliveIconImg = new BitmapImage(new Uri(aliveFilename, UriKind.RelativeOrAbsolute));
			}
			return (BitmapImage)aliveIconImg.Clone();
		}
		public BitmapImage getDeadImage() {
			if (deadIconImg == null) {
				deadIconImg = new BitmapImage(new Uri(deadFilename, UriKind.RelativeOrAbsolute));
			}
			return (BitmapImage)deadIconImg.Clone();
		}
    }

    public class Icons {
        private Dictionary<string, StaticIcon> staticIcons;
        private Dictionary<string, DynamicIcon> dynamicIcons;
        private Dictionary<TankType, TankIcon> allyTankIcons;
        private Dictionary<TankType, TankIcon> enemyTankIcons;

        public Icons(string iconsDescriptor) {
            staticIcons = new Dictionary<string, StaticIcon>();
            dynamicIcons = new Dictionary<string, DynamicIcon>();
            allyTankIcons = new Dictionary<TankType, TankIcon>();
            enemyTankIcons = new Dictionary<TankType, TankIcon>();

            XmlDocument XD = new XmlDocument();
            XD.Load(iconsDescriptor);
            XmlNode XN = XD.DocumentElement;
            XmlNodeList XNL = XN.SelectNodes("/icons/static/icon");

            for (int i = 0; i < XNL.Count; i++) {
                StaticIcon staticIcon = new StaticIcon(
                    XNL.Item(i).Attributes["id"].InnerText,
                    XNL.Item(i).SelectSingleNode("name").InnerText,
					System.IO.Path.GetDirectoryName(iconsDescriptor) + "\\" + XNL.Item(i).SelectSingleNode("filename").InnerText
                    );
                staticIcons.Add(staticIcon.id, staticIcon);
            }

            XNL = XN.SelectNodes("/icons/dynamic/icon");
            for (int i = 0; i < XNL.Count; i++) {
                DynamicIcon dynamicIcon = new DynamicIcon(
                    XNL.Item(i).Attributes["id"].InnerText,
                    XNL.Item(i).SelectSingleNode("name").InnerText,
					System.IO.Path.GetDirectoryName(iconsDescriptor) + "\\" + XNL.Item(i).SelectSingleNode("filename").InnerText
                    );
                dynamicIcons.Add(dynamicIcon.id, dynamicIcon);
            }

            XNL = XN.SelectNodes("/icons/tanks/icon");
            for (int i = 0; i < XNL.Count; i++) {
                TankType type;
                switch (XNL.Item(i).Attributes["type"].InnerText) {
                    case "heavy":
                        type = TankType.Heavy;
                        break;
                    case "medium":
                        type = TankType.Medium;
                        break;
                    case "light":
                        type = TankType.Light;
                        break;
                    case "td":
                        type = TankType.TD;
                        break;
                    case "spg":
                        type = TankType.SPG;
                        break;
                    default:
                        type = TankType.Heavy;
                        break;
                }
                TankIcon tankIcon = new TankIcon(
                    XNL.Item(i).Attributes["id"].InnerText,
                    XNL.Item(i).Attributes["side"].InnerText != "enemy",
                    type,
					System.IO.Path.GetDirectoryName(iconsDescriptor) + "\\" + XNL.Item(i).SelectSingleNode("alive").InnerText,
					System.IO.Path.GetDirectoryName(iconsDescriptor) + "\\" + XNL.Item(i).SelectSingleNode("dead").InnerText
                    );
                if (XNL.Item(i).Attributes["side"].InnerText == "enemy") {
                    enemyTankIcons.Add(tankIcon.type, tankIcon);
                } else {
                    allyTankIcons.Add(tankIcon.type, tankIcon);
                }
            }
        }

        public List<StaticIcon> getStaticIconList() {
            return staticIcons.Values.ToList();
        }

        public StaticIcon getStaticIcon(string id) {
            return staticIcons[id];
        }

        public List<DynamicIcon> getDynamicIconList() {
            return dynamicIcons.Values.ToList();
        }

        public DynamicIcon getDynamicIcon(string id) {
            return dynamicIcons[id];
        }

        public TankIcon getTankIcon(TankType type, bool isAlly) {
            if (isAlly) {
                return allyTankIcons[type];
            } else {
                return enemyTankIcons[type];
            }
        }
    }
}
