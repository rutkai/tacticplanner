using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

using TacticPlanner.Common;

namespace TacticPlanner.models {
	public class Tanks {
		private static Tanks _instance = null;

		private Dictionary<string, Tank> tanks;
		private List<Tank> sortedTanks;

		private List<string> nations;

		private static int tankSorter(Tank a, Tank b) {
			if (a == null && b == null) {
				return 0;
			} else if (a == null) {
				return -1;
			} else if (b == null) {
				return 1;
			} else {
				return String.Compare(a.name, b.name);
			}
		}

		private Tanks() {
			tanks = new Dictionary<string, Tank>();
			sortedTanks = new List<Tank>();
			nations = new List<string>();

			XmlDocument XD = new XmlDocument();
			XD.Load(App.ApplicationPath + "\\stamps\\tanks\\tanks.xml");
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
					App.ApplicationPath + "\\stamps\\tanks\\" + XNL.Item(i).SelectSingleNode("filename").InnerText
					);
				tanks.Add(tank.id, tank);
				sortedTanks.Add(tank);

				if (!nations.Contains(tank.nation)) {
					nations.Add(tank.nation);
				}
			}

			sortedTanks.Sort(tankSorter);
		}

		public static Tanks Instance {
			get {
				return Lazy.Init(ref _instance, () => new Tanks());
			}
		}

		public Tank getTank(String id) {
			return tanks[id];
		}

		public Tank[] getSortedTanks(string nation = "") {
			if (nation == "") {
				return sortedTanks.ToArray();
			} else {
				List<Tank> ret = new List<Tank>();
				foreach (Tank t in sortedTanks) {
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
