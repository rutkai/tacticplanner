using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Globalization;
using System.Xml;

using TacticPlanner.types;
using TacticPlanner.Common;

namespace TacticPlanner.models {
	class Maps {
		private static Maps _instance;

		private Dictionary<String, Map> maps;
		private SortedList<String, Map> sortedMaps;

		private Maps() {
			maps = new Dictionary<string, Map>();
			sortedMaps = new SortedList<string, Map>();

			XmlDocument XD = new XmlDocument();
			XD.Load(App.ApplicationPath + "\\maps\\maps.xml");
			XmlNode XN = XD.DocumentElement;
			XmlNodeList XNL = XN.SelectNodes("/maps/map");

			for (int i = 0; i < XNL.Count; i++) {
				XmlNodeList presets = XNL.Item(i).SelectSingleNode("gamemodes").ChildNodes;
				List<MapIcon> presetIcons = new List<MapIcon>();
				for (int j = 0; j < presets.Count; j++) {
					XmlNodeList iconlist = presets[j].ChildNodes;
					for (int k = 0; k < iconlist.Count; k++) {
						StaticIcon sicon = (StaticIcon)Icons.Instance.getStaticIcon(iconlist[k].Attributes["id"].InnerText).Clone();
						sicon.position = new Point(
							Convert.ToDouble(iconlist[k].Attributes["X"].InnerText, CultureInfo.InvariantCulture) * 1024,
							Convert.ToDouble(iconlist[k].Attributes["Y"].InnerText, CultureInfo.InvariantCulture) * 1024
							);
						MapIcon icon = new MapIcon(
							sicon,
							presets[j].Name == "normal" ? BattleType.Normal :
								presets[j].Name == "encounter" ? BattleType.Encounter :
								presets[j].Name == "assault" ? BattleType.Assault : BattleType.Undefined,
							presets[j].Attributes["variation"].InnerText
							);
						presetIcons.Add(icon);
					}
				}

				Map map = new Map(
					XNL.Item(i).Attributes["id"].InnerText,
					XNL.Item(i).SelectSingleNode("name").InnerText,
					App.ApplicationPath + "\\maps\\" + XNL.Item(i).SelectSingleNode("original").InnerText,
					App.ApplicationPath + "\\maps\\" + XNL.Item(i).SelectSingleNode("hd").InnerText,
					presetIcons.ToArray()
					);
				maps.Add(map.id, map);
				sortedMaps.Add(map.name, map);
			}
		}

		public static Maps Instance {
			get {
				return Lazy.Init(ref _instance, () => new Maps());
			}
		}

		public Map getMap(String id) {
			return maps[id];
		}

		public void setMapPack(MapPack pack) {
			foreach (Map map in maps.Values) {
				map.mapPack = pack;
			}
		}

		public Map[] getSortedMaps() {
			return sortedMaps.Values.ToArray();
		}
	}
}
