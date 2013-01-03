using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.IO.Packaging;
using System.Xml;

namespace TacticPlanner.models {
	class DynamicTactic : AbstractTactic {
		private Dictionary<string, DynamicTank> dynamicTanks;
		private List<StaticIcon> staticIcons;

		private Bitmap staticMap;

		private SolidBrush brush;
		private Font font;
		private int iconsSize;

		public bool ShowPlayerName { get; set; }
		public bool ShowTankName { get; set; }

		private StaticIcon selectedStaticIcon;
		private DynamicTank selectedDynamicTank;
		private bool hasSelectedStaticIcon;
		private bool hasSelectedDynamicTank;

		public DynamicTactic(Maps maps, Tanks tanks, Icons icons) : base(maps, tanks, icons) {
			dynamicTanks = new Dictionary<string, DynamicTank>();
			staticIcons = new List<StaticIcon>();

			brush = new SolidBrush(Color.Red);
			font = new Font("Arial", 20, FontStyle.Bold, GraphicsUnit.Pixel);
			iconsSize = 50;

			hasSelectedStaticIcon = hasSelectedDynamicTank = false;
		}

		public override void setMap(string id) {
			this.map = maps.getMap(id);
			redrawStaticItems();
		}

		public override Bitmap getTacticAt(int time) {
			Bitmap img = (Bitmap)staticMap.Clone();

			using (Graphics gr = Graphics.FromImage(img)) {
				gr.CompositingQuality = CompositingQuality.HighSpeed;
				gr.PixelOffsetMode = PixelOffsetMode.None;
				gr.SmoothingMode = SmoothingMode.None;
				gr.InterpolationMode = InterpolationMode.Default;

				foreach (DynamicTank tank in dynamicTanks.Values) {
					Bitmap icon;
					if (tank.killTime < time) {
						icon = icons.getTankIcon(tank.tank.type, tank.isAlly).getAliveImage();
					} else {
						icon = icons.getTankIcon(tank.tank.type, tank.isAlly).getDeadImage();
					}

					int roundTime = (int)(Math.Ceiling((float)time / 30.0) * 30.0);
					Bitmap actionIcon = null;
					if (tank.actions.ContainsKey(roundTime)) {
						actionIcon = icons.getDynamicIcon(tank.actions[roundTime]).getImage();
					}

					Point pos = getTankPosition(tank, time);

					if (icon.Height < icon.Width) {
						gr.DrawImage(icon, new Rectangle(pos.X - iconsSize / 2, pos.Y - iconsSize / 2, iconsSize, (iconsSize * icon.Height) / icon.Width));
					} else {
						gr.DrawImage(icon, new Rectangle(pos.X - iconsSize / 2, pos.Y - iconsSize / 2, (iconsSize * icon.Width) / icon.Height, iconsSize));
					}
					if (actionIcon != null) {
						gr.DrawImage(actionIcon, new Rectangle(pos.X - iconsSize - 5, pos.Y - iconsSize + 3, iconsSize * 2, (iconsSize * actionIcon.Height * 2) / actionIcon.Width));
					}
					if ((ShowPlayerName && tank.name != "") || ShowTankName) {
						string text = "";
						if ((ShowPlayerName && tank.name != "") && ShowTankName) {
							text = tank.name + " (" + tank.tank.name + ")";
						} else if (ShowPlayerName && tank.name != "") {
							text = tank.name;
						} else if (ShowTankName) {
							text = tank.tank.name;
						}
						gr.DrawString(text, font, brush, pos.X - (text.Length * (font.Size + 3)) / 4 - 10, pos.Y + iconsSize / 2);
					}
				}
			}

			return img;
		}

		public override Bitmap getPlayTacticAt(int time) {
			return getTacticAt(time);
		}

		public void setPenColor(Color color) {
			brush.Color = color;
		}

		public void setIconsSize(int size) {
			iconsSize = size;
			font = new Font("Arial", size / 2, FontStyle.Bold, GraphicsUnit.Pixel);

			redrawStaticItems();
		}

		public void redrawStaticItems() {
			staticMap = map.getMapImage();

			using (Graphics gr = Graphics.FromImage(staticMap)) {
				gr.CompositingQuality = CompositingQuality.HighSpeed;
				gr.PixelOffsetMode = PixelOffsetMode.None;
				gr.SmoothingMode = SmoothingMode.None;
				gr.InterpolationMode = InterpolationMode.Default;
				foreach (StaticIcon staticon in staticIcons) {
					Bitmap icon = staticon.getImage();
					gr.DrawImage(icon, new Rectangle(staticon.position.X - iconsSize / 2, staticon.position.Y - iconsSize / 2, iconsSize, (iconsSize * icon.Height) / icon.Width));
				}
				gr.Dispose();
			}
		}

		public Point getTankPosition(DynamicTank tank, int time) {
			int roundTime = (int)(Math.Ceiling((float)time / 30.0) * 30.0);

			while (!tank.positions.ContainsKey(roundTime)) {
				roundTime += 30;
			}

			Point from = tank.positions[roundTime];
			int fromTime = roundTime;

			roundTime = (time / 30) * 30;
			while (!tank.positions.ContainsKey(roundTime) && (roundTime >= 0)) {
				roundTime -= 30;
			}

			if (roundTime < 0 || fromTime == roundTime) {
				return from;
			} else {
				Point to = tank.positions[roundTime];
				int toTime = roundTime;

				return new Point(
					from.X - (int)((float)(from.X - to.X) * Math.Abs((float)(fromTime - time) / (float)(fromTime - toTime))),
					from.Y - (int)((float)(from.Y - to.Y) * Math.Abs((float)(fromTime - time) / (float)(fromTime - toTime)))
				);
			}
		}

		public bool addStaticElement(StaticIcon icon) {
			if (staticIcons.Contains(icon)) {
				return false;
			} else {
				staticIcons.Add(icon);
				redrawStaticItems();
				return true;
			}
		}

		public bool removeStaticElement(string id) {
			bool result = false;

			for (int i = 0; i < staticIcons.Count; i++) {
				if (staticIcons[i].id == id) {
					staticIcons.RemoveAt(i);
					result = true;
					break;
				}
			}

			redrawStaticItems();

			return result;
		}

		public bool addDynamicTank(DynamicTank tank) {
			if (dynamicTanks.ContainsKey(tank.name)) {
				return false;
			}

			dynamicTanks.Add(tank.name, tank);
			return true;
		}

		public bool editDynamicTank(DynamicTank oldTank, DynamicTank newTank) {
			if (oldTank.name != newTank.name && dynamicTanks.ContainsKey(newTank.name)) {
				return false;
			}

			if (oldTank.name == newTank.name) {
				dynamicTanks[newTank.name] = newTank;
			} else {
				dynamicTanks.Remove(oldTank.name);
				dynamicTanks.Add(newTank.name, newTank);
			}
			return true;
		}

		public void removeDynamicTank(DynamicTank tank) {
			dynamicTanks.Remove(tank.name);
		}

		public DynamicTank[] getDynamicTanks() {
			return dynamicTanks.Values.ToArray();
		}

		public void setDynamicTankAction(string name, int time, string actionId) {
			dynamicTanks[name].actions.Remove(time);
			if (actionId != "") {
				dynamicTanks[name].actions.Add(time, actionId);
			}
		}

		public string getDynamicTankActionId(string name, int time) {
			if (dynamicTanks[name].actions.ContainsKey(time)) {
				return dynamicTanks[name].actions[time];
			} else {
				return "";
			}
		}

		public void removeDynamicPosition(string name, int time) {
			if (time != 900) {
				dynamicTanks[name].positions.Remove(time);
			}
		}

		public bool isAlive(string name, int time) {
			if (dynamicTanks[name].killTime < time) {
				return true;
			} else {
				return false;
			}
		}

		public void setKill(string name, int time) {
			dynamicTanks[name].killTime = time;
		}

		public void selectItem(Point from, int time) {
			int bestDistance = int.MaxValue;
			StaticIcon staticIcon = null;
			DynamicTank dynamicTank = null;

			deselectItem();

			int distance;
			foreach (StaticIcon item in staticIcons) {
				distance = (int)Math.Sqrt(Math.Pow(item.position.X - from.X, 2) + Math.Pow(item.position.Y - from.Y, 2));
				if (bestDistance > distance) {
					bestDistance = distance;
					staticIcon = item;
				}
			}

			foreach (DynamicTank item in dynamicTanks.Values) {
				Point pos = getTankPosition(item, time);

				distance = (int)Math.Sqrt(Math.Pow(pos.X - from.X, 2) + Math.Pow(pos.Y - from.Y, 2));
				if (bestDistance > distance) {
					bestDistance = distance;
					staticIcon = null;
					dynamicTank = item;
				}
			}

			if (bestDistance > 30) {
				staticIcon = null;
				dynamicTank = null;
			}

			if (staticIcon != null) {
				hasSelectedStaticIcon = true;
				selectedStaticIcon = staticIcon;
			} else if (dynamicTank != null) {
				hasSelectedDynamicTank = true;
				selectedDynamicTank = dynamicTank;
			}
		}

		public void deselectItem() {
			hasSelectedDynamicTank = hasSelectedStaticIcon = false;
		}

		public void moveItem(Point to, int time) {
			if (hasSelectedStaticIcon) {
				selectedStaticIcon.position = to;
				redrawStaticItems();
			} else if (hasSelectedDynamicTank) {
				if (selectedDynamicTank.positions.ContainsKey(time)) {
					selectedDynamicTank.positions[time] = to;
				} else {
					selectedDynamicTank.positions.Add(time, to);
				}
			}
		}

		public override void save(Package zip) {
			UTF8Encoding encoding = new UTF8Encoding();

			MemoryStream xmlString = new MemoryStream();
			XmlTextWriter xmlWriter = new XmlTextWriter(xmlString, Encoding.UTF8);
			xmlWriter.WriteStartDocument();
			xmlWriter.WriteComment("Tactic planner save file. DO NOT EDIT THIS FILE MANUALLY!");
			xmlWriter.WriteStartElement("dynamic");

			xmlWriter.WriteAttributeString("version", "1.1.0");

			xmlWriter.WriteEndElement();
			xmlWriter.WriteEndDocument();
			xmlWriter.Close();

			PackagePart part = zip.CreatePart(new Uri("/dynamic/dynamic.xml", UriKind.Relative), System.Net.Mime.MediaTypeNames.Text.Xml, CompressionOption.Fast);
			part.GetStream().Write(xmlString.ToArray(), 0, xmlString.ToArray().Length);

			xmlString = new MemoryStream();
			xmlWriter = new XmlTextWriter(xmlString, Encoding.UTF8);
			xmlWriter.WriteStartDocument();
			xmlWriter.WriteComment("Tactic planner save file. DO NOT EDIT THIS FILE MANUALLY!");
			xmlWriter.WriteStartElement("mapData");

			xmlWriter.WriteAttributeString("version", "1.1.0");

			xmlWriter.WriteStartElement("map");
			xmlWriter.WriteAttributeString("id", map.id);
			xmlWriter.WriteAttributeString("timer", Convert.ToString(timer));
			xmlWriter.WriteStartElement("staticIcons");
			foreach (StaticIcon item in staticIcons) {
				xmlWriter.WriteStartElement("icon");
				xmlWriter.WriteStartElement("id");
				xmlWriter.WriteValue(item.id);
				xmlWriter.WriteEndElement();
				xmlWriter.WriteStartElement("position");
				xmlWriter.WriteAttributeString("X", item.position.X.ToString());
				xmlWriter.WriteAttributeString("Y", item.position.Y.ToString());
				xmlWriter.WriteEndElement();
				xmlWriter.WriteEndElement();
			}
			xmlWriter.WriteEndElement();
			xmlWriter.WriteStartElement("tanks");
			foreach (DynamicTank item in dynamicTanks.Values) {
				xmlWriter.WriteStartElement("tank");
				xmlWriter.WriteStartElement("name");
				xmlWriter.WriteValue(item.name);
				xmlWriter.WriteEndElement();
				xmlWriter.WriteStartElement("isAlly");
				xmlWriter.WriteValue(Convert.ToString(item.isAlly));
				xmlWriter.WriteEndElement();
				xmlWriter.WriteStartElement("tankId");
				xmlWriter.WriteValue(item.tank.id);
				xmlWriter.WriteEndElement();
				xmlWriter.WriteStartElement("positions");
				foreach (KeyValuePair<int, Point> pair in item.positions) {
					xmlWriter.WriteStartElement("position");
					xmlWriter.WriteAttributeString("id", pair.Key.ToString());
					xmlWriter.WriteAttributeString("X", pair.Value.X.ToString());
					xmlWriter.WriteAttributeString("Y", pair.Value.Y.ToString());
					xmlWriter.WriteEndElement();
				}
				xmlWriter.WriteEndElement();
				xmlWriter.WriteStartElement("actions");
				foreach (KeyValuePair<int, string> pair in item.actions) {
					xmlWriter.WriteStartElement("action");
					xmlWriter.WriteAttributeString("id", pair.Key.ToString());
					xmlWriter.WriteAttributeString("actionId", pair.Value);
					xmlWriter.WriteEndElement();
				}
				xmlWriter.WriteEndElement();
				xmlWriter.WriteStartElement("killTime");
				xmlWriter.WriteValue(item.killTime);
				xmlWriter.WriteEndElement();
				xmlWriter.WriteEndElement();
			}
			xmlWriter.WriteEndElement();
			xmlWriter.WriteEndElement();

			xmlWriter.WriteEndElement();
			xmlWriter.WriteEndDocument();
			xmlWriter.Close();

			part = zip.CreatePart(new Uri("/dynamic/maps.xml", UriKind.Relative), System.Net.Mime.MediaTypeNames.Text.Xml, CompressionOption.Fast);
			part.GetStream().Write(xmlString.ToArray(), 0, xmlString.ToArray().Length);
		}

		public override void load(Package zip) {
			try {
				PackagePart part = zip.GetPart(new Uri("/dynamic/dynamic.xml", UriKind.Relative));
				loadVersion110(zip);
			} catch (InvalidOperationException) {
				loadVersion100(zip);
			}
			redrawStaticItems();
		}
		private void loadVersion100(Package zip) {
			PackagePart part = zip.GetPart(new Uri("/tactic.xml", UriKind.Relative));
			Stream zipStream = part.GetStream(FileMode.Open, FileAccess.Read);
			MemoryStream xmlString = new MemoryStream();
			byte[] buffer = new byte[10000];
			int read = zipStream.Read(buffer, 0, buffer.Length);
			while (read > 0) {
				xmlString.Write(buffer, 0, buffer.Length);
				read = zipStream.Read(buffer, 0, buffer.Length);
			}

			xmlString.Position = 0;
			StreamReader sr = new StreamReader(xmlString);
			XmlDocument XD = new XmlDocument();
			XD.LoadXml(sr.ReadToEnd());
			XmlNode XNDocument = XD.DocumentElement;
			XmlNode XN = XNDocument.SelectSingleNode("/tactic");

			XN = XNDocument.SelectSingleNode("/tactic/map");
			setMap(XN.Attributes["id"].InnerText);

			XN = XNDocument.SelectSingleNode("/tactic/dynamicMaps/settings/timer");
			timer = Convert.ToBoolean(XN.InnerText);

			XmlNodeList XNL = XNDocument.SelectNodes("/tactic/dynamicMaps/staticIcons/icon");
			for (int i = 0; i < XNL.Count; i++) {
				StaticIcon staticIcon = icons.getStaticIcon(XNL.Item(i).SelectSingleNode("id").InnerText);
				XN = XNL.Item(i).SelectSingleNode("position");
				staticIcon.position = new Point(
					Convert.ToInt32(XN.Attributes["X"].InnerText),
					Convert.ToInt32(XN.Attributes["Y"].InnerText)
				);
				staticIcons.Add(staticIcon);
			}

			XNL = XNDocument.SelectNodes("/tactic/dynamicMaps/tanks/tank");
			XmlNodeList XNLtemp;
			for (int i = 0; i < XNL.Count; i++) {
				DynamicTank tank = new DynamicTank();
				tank.name = XNL.Item(i).SelectSingleNode("name").InnerText;
				tank.isAlly = Convert.ToBoolean(XNL.Item(i).SelectSingleNode("isAlly").InnerText);
				tank.tank = tanks.getTank(XNL.Item(i).SelectSingleNode("tankId").InnerText);
				XNLtemp = XNL.Item(i).SelectNodes("positions/position");
				for (int j = 0; j < XNLtemp.Count; j++) {
					tank.positions.Add(
						Convert.ToInt32(XNLtemp.Item(j).Attributes["id"].InnerText),
						new Point(
							Convert.ToInt32(XNLtemp.Item(j).Attributes["X"].InnerText),
							Convert.ToInt32(XNLtemp.Item(j).Attributes["Y"].InnerText)
						)
					);
				}
				XNLtemp = XNL.Item(i).SelectNodes("actions/action");
				for (int j = 0; j < XNLtemp.Count; j++) {
					tank.actions.Add(
						Convert.ToInt32(XNLtemp.Item(j).Attributes["id"].InnerText),
						XNLtemp.Item(j).Attributes["actionId"].InnerText
					);
				}
				tank.killTime = Convert.ToInt32(XNL.Item(i).SelectSingleNode("killTime").InnerText);
				dynamicTanks.Add(tank.name, tank);
			}
		}
		private void loadVersion110(Package zip) {
			PackagePart part = zip.GetPart(new Uri("/dynamic/maps.xml", UriKind.Relative));
			Stream zipStream = part.GetStream(FileMode.Open, FileAccess.Read);
			MemoryStream xmlString = new MemoryStream();
			byte[] buffer = new byte[10000];
			int read = zipStream.Read(buffer, 0, buffer.Length);
			while (read > 0) {
				xmlString.Write(buffer, 0, buffer.Length);
				read = zipStream.Read(buffer, 0, buffer.Length);
			}

			xmlString.Position = 0;
			StreamReader sr = new StreamReader(xmlString);
			XmlDocument XD = new XmlDocument();
			XD.LoadXml(sr.ReadToEnd());
			XmlNode XNDocument = XD.DocumentElement;
			XmlNode XN = XNDocument.SelectSingleNode("/mapData/map");
			setMap(XN.Attributes["id"].InnerText);
			timer = Convert.ToBoolean(XN.Attributes["timer"].InnerText);

			XmlNodeList XNL = XNDocument.SelectNodes("/mapData/map/staticIcons/icon");
			for (int i = 0; i < XNL.Count; i++) {
				StaticIcon staticIcon = icons.getStaticIcon(XNL.Item(i).SelectSingleNode("id").InnerText);
				XN = XNL.Item(i).SelectSingleNode("position");
				staticIcon.position = new Point(
					Convert.ToInt32(XN.Attributes["X"].InnerText),
					Convert.ToInt32(XN.Attributes["Y"].InnerText)
				);
				staticIcons.Add(staticIcon);
			}

			XNL = XNDocument.SelectNodes("/mapData/map/tanks/tank");
			XmlNodeList XNLtemp;
			for (int i = 0; i < XNL.Count; i++) {
				DynamicTank tank = new DynamicTank();
				tank.name = XNL.Item(i).SelectSingleNode("name").InnerText;
				tank.isAlly = Convert.ToBoolean(XNL.Item(i).SelectSingleNode("isAlly").InnerText);
				tank.tank = tanks.getTank(XNL.Item(i).SelectSingleNode("tankId").InnerText);
				XNLtemp = XNL.Item(i).SelectNodes("positions/position");
				for (int j = 0; j < XNLtemp.Count; j++) {
					tank.positions.Add(
						Convert.ToInt32(XNLtemp.Item(j).Attributes["id"].InnerText),
						new Point(
							Convert.ToInt32(XNLtemp.Item(j).Attributes["X"].InnerText),
							Convert.ToInt32(XNLtemp.Item(j).Attributes["Y"].InnerText)
						)
					);
				}
				XNLtemp = XNL.Item(i).SelectNodes("actions/action");
				for (int j = 0; j < XNLtemp.Count; j++) {
					tank.actions.Add(
						Convert.ToInt32(XNLtemp.Item(j).Attributes["id"].InnerText),
						XNLtemp.Item(j).Attributes["actionId"].InnerText
					);
				}
				tank.killTime = Convert.ToInt32(XNL.Item(i).SelectSingleNode("killTime").InnerText);
				dynamicTanks.Add(tank.name, tank);
			}
		}
	}
}
