using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace TacticPlanner.models {
    public class Tanks {
        private Dictionary<string, Tank> tanks;
        private SortedList<string, Tank> sortedTanks;

        private List<string> nations;

        public Tanks(string tanksDescriptor) {
            tanks = new Dictionary<string, Tank>();
            sortedTanks = new SortedList<string, Tank>();
            nations = new List<string>();

            XmlDocument XD = new XmlDocument();
            XD.Load(tanksDescriptor);
            XmlNode XN = XD.DocumentElement;
            XmlNodeList XNL = XN.SelectNodes("/tanks/tank");

            for (int i = 0; i < XNL.Count; i++) {
                TankType type;
                switch (XNL.Item(i).SelectSingleNode("type").InnerText) {
                    case "Heavy":
                        type = TankType.Heavy;
                        break;
                    case "Medium":
                        type = TankType.Medium;
                        break;
                    case "Light":
                        type = TankType.Light;
                        break;
                    case "TD":
                        type = TankType.TD;
                        break;
                    case "SPG":
                        type = TankType.SPG;
                        break;
                    default:
                        type = TankType.Heavy;
                        break;
                }
                Tank tank = new Tank(
                    XNL.Item(i).Attributes["id"].InnerText,
                    XNL.Item(i).SelectSingleNode("nation").InnerText,
                    XNL.Item(i).SelectSingleNode("name").InnerText,
                    type,
					System.IO.Path.GetDirectoryName(tanksDescriptor) + "\\" + XNL.Item(i).SelectSingleNode("filename").InnerText
                    );
                tanks.Add(tank.id, tank);
                sortedTanks.Add(tank.filename, tank);

                if (!nations.Contains(tank.nation)) {
                    nations.Add(tank.nation);
                }
            }
        }

        public Tank getTank(String id) {
            return tanks[id];
        }

        public Tank[] getSortedTanks(string nation = "") {
            if (nation == "") {
                return sortedTanks.Values.ToArray();
            } else {
                List<Tank> ret = new List<Tank>();
                foreach (Tank t in sortedTanks.Values) {
                    if (t.nation == nation) {
                        ret.Add(t);
                    }
                }
                return ret.ToArray();
            }
        }

        public String[] getNations() {
            return nations.ToArray();
        }
    }
}
