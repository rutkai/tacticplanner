using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Xml;
using System.Drawing;

namespace TacticPlanner.models {
    public class StaticIcon {
        public string id { get; set; }
        public string name { get; set; }
        public string filename { get; set; }
        public Point position { get; set; }

        public StaticIcon(string _id, string _name = "", string _filename = "") {
            id = _id;
            name = _name;
            filename = _filename;
            position = new Point(500, 500);
        }
    }

    public class DynamicIcon {
        public string id { get; set; }
        public string name { get; set; }
        public string filename { get; set; }

        public DynamicIcon(string _id, string _name = "", string _filename = "") {
            id = _id;
            name = _name;
            filename = _filename;
        }
    }

    public class TankIcon {
        public string id { get; set; }
        public bool isAlly { get; set; }
        public TankTypes type { get; set; }
        public string aliveFilename { get; set; }
        public string deadFilename { get; set; }

        public TankIcon(string _id, bool _isAlly = true, TankTypes _type = TankTypes.Heavy, string _aliveFilename = "", string _deadFilename = "") {
            id = _id;
            isAlly = _isAlly;
            type = _type;
            aliveFilename = _aliveFilename;
            deadFilename = _deadFilename;
        }
    }

    public class Icons {
        private Dictionary<string, StaticIcon> staticIcons;
        private Dictionary<string, DynamicIcon> dynamicIcons;
        private Dictionary<TankTypes, TankIcon> allyTankIcons;
        private Dictionary<TankTypes, TankIcon> enemyTankIcons;

        public Icons(string iconsDescriptor) {
            staticIcons = new Dictionary<string, StaticIcon>();
            dynamicIcons = new Dictionary<string, DynamicIcon>();
            allyTankIcons = new Dictionary<TankTypes, TankIcon>();
            enemyTankIcons = new Dictionary<TankTypes, TankIcon>();

            XmlDocument XD = new XmlDocument();
            XD.Load(iconsDescriptor);
            XmlNode XN = XD.DocumentElement;
            XmlNodeList XNL = XN.SelectNodes("/icons/static/icon");

            for (int i = 0; i < XNL.Count; i++) {
                StaticIcon staticIcon = new StaticIcon(
                    XNL.Item(i).Attributes["id"].InnerText,
                    XNL.Item(i).SelectSingleNode("name").InnerText,
                    XNL.Item(i).SelectSingleNode("filename").InnerText
                    );
                staticIcons.Add(staticIcon.id, staticIcon);
            }

            XNL = XN.SelectNodes("/icons/dynamic/icon");
            for (int i = 0; i < XNL.Count; i++) {
                DynamicIcon dynamicIcon = new DynamicIcon(
                    XNL.Item(i).Attributes["id"].InnerText,
                    XNL.Item(i).SelectSingleNode("name").InnerText,
                    XNL.Item(i).SelectSingleNode("filename").InnerText
                    );
                dynamicIcons.Add(dynamicIcon.id, dynamicIcon);
            }

            XNL = XN.SelectNodes("/icons/tanks/icon");
            for (int i = 0; i < XNL.Count; i++) {
                TankTypes type;
                switch (XNL.Item(i).Attributes["type"].InnerText) {
                    case "heavy":
                        type = TankTypes.Heavy;
                        break;
                    case "medium":
                        type = TankTypes.Medium;
                        break;
                    case "light":
                        type = TankTypes.Light;
                        break;
                    case "td":
                        type = TankTypes.TD;
                        break;
                    case "spg":
                        type = TankTypes.SPG;
                        break;
                    default:
                        type = TankTypes.Heavy;
                        break;
                }
                TankIcon tankIcon = new TankIcon(
                    XNL.Item(i).Attributes["id"].InnerText,
                    XNL.Item(i).Attributes["side"].InnerText != "enemy",
                    type,
                    XNL.Item(i).SelectSingleNode("alive").InnerText,
                    XNL.Item(i).SelectSingleNode("dead").InnerText
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

        public TankIcon getTankIcon(TankTypes type, bool isAlly) {
            if (isAlly) {
                return allyTankIcons[type];
            } else {
                return enemyTankIcons[type];
            }
        }
    }
}
