using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Xml;
using TacticPlanner.types;

namespace TacticPlanner.models {
    class Maps {
        private Dictionary<String, Map> maps;
        private SortedList<String, Map> sortedMaps;

        public Maps(string mapsDescriptor) {
            maps = new Dictionary<string, Map>();
            sortedMaps = new SortedList<string, Map>();

            XmlDocument XD = new XmlDocument();
            XD.Load(mapsDescriptor);
            XmlNode XN = XD.DocumentElement;
            XmlNodeList XNL = XN.SelectNodes("/maps/map");

            for (int i = 0; i < XNL.Count; i++) {
                Map map = new Map(
                    XNL.Item(i).Attributes["id"].InnerText,
                    XNL.Item(i).SelectSingleNode("name").InnerText,
                    System.IO.Path.GetDirectoryName(mapsDescriptor) + "\\" + XNL.Item(i).SelectSingleNode("original").InnerText,
					System.IO.Path.GetDirectoryName(mapsDescriptor) + "\\" + XNL.Item(i).SelectSingleNode("hd").InnerText
                    );
                maps.Add(map.id, map);
                sortedMaps.Add(map.name, map);
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
