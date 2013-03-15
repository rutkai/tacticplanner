using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using System.IO.Packaging;
using System.Globalization;
using System.Xml;

namespace TacticPlanner.models {
	class DynamicTactic : AbstractTactic {
		private List<DynamicTank> dynamicTanks;
		private List<StaticIcon> staticIcons;

		private readonly BitmapSource clearTactic;

		private Brush brush;
		private int iconsSize;

		public bool ShowPlayerName { get; set; }
		public bool ShowTankName { get; set; }

		private List<StaticIcon> selectedStaticIcon;
		private List<DynamicTank> selectedDynamicTank, copyDynamicTank;

		public DynamicTactic(Maps maps, Tanks tanks, Icons icons) : base(maps, tanks, icons) {
			dynamicTanks = new List<DynamicTank>();
			staticIcons = new List<StaticIcon>();

			brush = new SolidColorBrush(Color.FromRgb(255, 0, 0));
			brush.Freeze();
			iconsSize = 50;

			selectedStaticIcon = new List<StaticIcon>();
			selectedDynamicTank = new List<DynamicTank>();
			copyDynamicTank = new List<DynamicTank>();

			var source = new Uri(@"pack://application:,,,/Resources/clearTactics.png", UriKind.Absolute);
			clearTactic = new BitmapImage(source);  
		}

		public override ImageSource getTacticAt(int time) {
			DrawingGroup resultDraw = new DrawingGroup();
			DrawingContext dc = resultDraw.Open();
			resultDraw.ClipGeometry = new RectangleGeometry(new Rect(0, 0, clearTactic.PixelWidth, clearTactic.PixelHeight));
			dc.DrawImage(clearTactic, new Rect(0, 0, clearTactic.PixelWidth, clearTactic.PixelHeight));

			Pen pen = new Pen(brush, iconsSize / 2);
			Pen linePen = new Pen(brush, iconsSize / 25);

			foreach (StaticIcon staticon in staticIcons) {
				BitmapImage icon = staticon.getImage();
				dc.DrawImage(icon, new Rect(staticon.position.X - iconsSize / 2, staticon.position.Y - iconsSize / 2, iconsSize, (iconsSize * icon.Height) / icon.Width));
				if (selectedStaticIcon.Contains(staticon)) {
					dc.DrawGeometry(brush, linePen, makeRectangleGeometry(new Rect(staticon.position.X - iconsSize / 2 - 2, staticon.position.Y - iconsSize / 2 - 2, iconsSize + 2, (iconsSize * icon.Height) / icon.Width + 2)));
				}
			}

			foreach (DynamicTank tank in dynamicTanks) {
				BitmapSource icon;
				if (tank.killTime < time) {
					icon = icons.getTankIcon(tank.tank.type, tank.isAlly).getAliveImage();
				} else {
					icon = icons.getTankIcon(tank.tank.type, tank.isAlly).getDeadImage();
				}

				int roundTime = (int)(Math.Ceiling((float)time / 30.0) * 30.0);

				BitmapSource actionIcon = null;
				if (tank.actions.ContainsKey(roundTime)) {
					actionIcon = icons.getDynamicIcon(tank.actions[roundTime]).getImage();
				}

				Point pos = getTankPosition(tank, time);
				if (icon.Height < icon.Width) {
					dc.DrawImage(icon, new Rect(pos.X - iconsSize / 2, pos.Y - iconsSize / 2, iconsSize, (iconsSize * icon.Height) / icon.Width));
					if (selectedDynamicTank.Contains(tank)) {
						dc.DrawGeometry(brush, linePen, makeRectangleGeometry(new Rect(pos.X - iconsSize / 2 - 2, pos.Y - iconsSize / 2 - 2, iconsSize + 2, (iconsSize * icon.Height) / icon.Width + 2)));
					}
				} else {
					dc.DrawImage(icon, new Rect(pos.X - iconsSize / 2, pos.Y - iconsSize / 2, (iconsSize * icon.Width) / icon.Height, iconsSize));
					if (selectedDynamicTank.Contains(tank)) {
						dc.DrawGeometry(brush, linePen, makeRectangleGeometry(new Rect(pos.X - iconsSize / 2 - 2, pos.Y - iconsSize / 2 - 2, (iconsSize * icon.Width) / icon.Height + 2, iconsSize + 2)));
					}
				}

				if (actionIcon != null) {
					dc.DrawImage(actionIcon, new Rect(pos.X - iconsSize, pos.Y - iconsSize, iconsSize * 2, (iconsSize * actionIcon.Height * 2) / actionIcon.Width));
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
					FormattedText ftxt = new FormattedText(text, System.Globalization.CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface(new FontFamily("Tahoma"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal), iconsSize / 2, brush);
					dc.DrawText(ftxt, new Point(pos.X - ftxt.WidthIncludingTrailingWhitespace / 2 - 5, pos.Y + iconsSize / 2));
				}
			}
			dc.Close();

			resultDraw.Freeze();
			return new DrawingImage(resultDraw);
		}

		private Geometry makeRectangleGeometry(Rect rect) {
			GeometryGroup rg = new GeometryGroup();
			rg.Children.Add(new LineGeometry(rect.TopLeft, rect.TopRight));
			rg.Children.Add(new LineGeometry(rect.TopRight, rect.BottomRight));
			rg.Children.Add(new LineGeometry(rect.BottomRight, rect.BottomLeft));
			rg.Children.Add(new LineGeometry(rect.BottomLeft, rect.TopLeft));
			return rg;
		}

		public override ImageSource getPlayTacticAt(int time) {
			deselectItem();
			return getTacticAt(time);
		}

		public void setPenColor(Color color) {
			brush = new SolidColorBrush(color);
			brush.Freeze();
		}

		public void setIconsSize(int size) {
			iconsSize = size;
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

		public void addStaticElement(StaticIcon icon) {
			if (!staticIcons.Contains(icon)) {
				staticIcons.Add(icon);
			}
		}

		public void removeStaticElement(StaticIcon icon) {
			staticIcons.Remove(icon);
		}

		public bool hasStaticElement(StaticIcon icon) {
			return staticIcons.Contains(icon);
		}

		public void addDynamicTank(DynamicTank tank) {
			dynamicTanks.Add(tank);
		}

		public void removeDynamicTank(DynamicTank tank) {
			dynamicTanks.Remove(tank);
		}

		public DynamicTank[] getDynamicTanks() {
			return dynamicTanks.ToArray();
		}

		public void setDynamicTankAction(DynamicTank tank, int time, string actionId) {
			tank.actions.Remove(time);
			if (actionId != "") {
				tank.actions.Add(time, actionId);
			}
		}

		public string getDynamicTankActionId(DynamicTank tank, int time) {
			if (tank.actions.ContainsKey(time)) {
				return tank.actions[time];
			} else {
				return "";
			}
		}

		public void removeDynamicPosition(DynamicTank tank, int time) {
			if (time != 900) {
				tank.positions.Remove(time);
			}
		}

		public bool isAlive(DynamicTank tank, int time) {
			if (tank.killTime < time) {
				return true;
			} else {
				return false;
			}
		}

		public void setKill(DynamicTank tank, int time) {
			tank.killTime = time;
		}

		public bool selectItem(Point from, bool enableMultiselect, int time) {
			int bestDistance = int.MaxValue;
			StaticIcon staticIcon = null;
			DynamicTank dynamicTank = null;
			bool newSelection = false;

			int distance;
			foreach (StaticIcon item in staticIcons) {
				distance = (int)Math.Sqrt(Math.Pow(item.position.X - from.X, 2) + Math.Pow(item.position.Y - from.Y, 2));
				if (bestDistance > distance) {
					bestDistance = distance;
					staticIcon = item;
				}
			}

			foreach (DynamicTank item in dynamicTanks) {
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

			if (!enableMultiselect) {
				deselectItem();
			}
			if (staticIcon != null) {
				if (!selectedStaticIcon.Contains(staticIcon)) {
					selectedStaticIcon.Add(staticIcon);
					newSelection = true;
				}
			} else if (dynamicTank != null) {
				if (!selectedDynamicTank.Contains(dynamicTank)) {
					selectedDynamicTank.Add(dynamicTank);
					newSelection = true;
				}
			}
			return newSelection;
		}

		public void selectItem(StaticIcon icon) {
			selectedStaticIcon.Add(icon);
		}

		public void selectItem(DynamicTank tank) {
			selectedDynamicTank.Add(tank);
		}

		public void deselectItem(Point from, int time) {
			int bestDistance = int.MaxValue;
			StaticIcon staticIcon = null;
			DynamicTank dynamicTank = null;

			int distance;
			foreach (StaticIcon item in staticIcons) {
				distance = (int)Math.Sqrt(Math.Pow(item.position.X - from.X, 2) + Math.Pow(item.position.Y - from.Y, 2));
				if (bestDistance > distance) {
					bestDistance = distance;
					staticIcon = item;
				}
			}

			foreach (DynamicTank item in dynamicTanks) {
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
				if (selectedStaticIcon.Contains(staticIcon)) {
					selectedStaticIcon.Remove(staticIcon);
				}
			} else if (dynamicTank != null) {
				if (selectedDynamicTank.Contains(dynamicTank)) {
					selectedDynamicTank.Remove(dynamicTank);
				}
			}
		}

		public void deselectItem() {
			selectedStaticIcon = new List<StaticIcon>();
			selectedDynamicTank = new List<DynamicTank>();
		}

		public bool hasSelectedItem() {
			return selectedStaticIcon.Count != 0 || selectedDynamicTank.Count != 0;
		}

		public bool isSelectedCopyable() {
			return selectedDynamicTank.Count != 0;
		}

		public void moveItem(Point from, Point to, int time) {
			foreach (StaticIcon icon in selectedStaticIcon) {
				Point pos = icon.position;
				pos.X -= from.X - to.X;
				pos.Y -= from.Y - to.Y;
				icon.position = pos;
			}

			foreach (DynamicTank tank in selectedDynamicTank) {
				if (!tank.positions.ContainsKey(time)) {
					Point current = getTankPosition(tank, time);
					tank.positions.Add(time, current);
				}

				Point pos = tank.positions[time];
				pos.X -= from.X - to.X;
				pos.Y -= from.Y - to.Y;
				tank.positions[time] = pos;
			}
		}

		public void copySelected() {
			copyDynamicTank = selectedDynamicTank;
		}

		public void paste() {
			if (copyDynamicTank.Count != 0) {
				List<DynamicTank> newTanks = new List<DynamicTank>();
				foreach (DynamicTank tank in copyDynamicTank) {
					DynamicTank newTank = (DynamicTank)tank.Clone();
					foreach (int time in newTank.positions.Keys.ToList()) {
						Point pos = newTank.positions[time];
						pos.X += pos.X < 1010 ? 10 : 0;
						pos.Y += pos.Y < 1010 ? 10 : 0;
						newTank.positions[time] = pos;
					}
					addDynamicTank(newTank);
					newTanks.Add(newTank);
				}
				copyDynamicTank = selectedDynamicTank = newTanks;
			}
		}

		public override void save(Package zip) {
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
			serialize(xmlString);

			part = zip.CreatePart(new Uri("/dynamic/maps.xml", UriKind.Relative), System.Net.Mime.MediaTypeNames.Text.Xml, CompressionOption.Fast);
			xmlString.Position = 0;
			xmlString.CopyTo(part.GetStream());
		}

		public void serialize(Stream stream) {
			XmlTextWriter xmlWriter = new XmlTextWriter(stream, Encoding.UTF8);
			xmlWriter.WriteStartDocument();
			xmlWriter.WriteComment("Tactic planner save file. DO NOT EDIT THIS FILE MANUALLY!");
			xmlWriter.WriteStartElement("mapData");

			xmlWriter.WriteAttributeString("version", "1.1.0");

			xmlWriter.WriteStartElement("map");
			xmlWriter.WriteAttributeString("id", map.id);
			xmlWriter.WriteAttributeString("BattleType", map.Battletype.ToString());
			xmlWriter.WriteAttributeString("Variation", map.Variation);
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
			foreach (DynamicTank item in dynamicTanks) {
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
			xmlWriter.Flush();
		}

		public override void load(Package zip) {
			reset();
			try {
				PackagePart part = zip.GetPart(new Uri("/dynamic/dynamic.xml", UriKind.Relative));
				loadVersion110(zip);
			} catch (InvalidOperationException) {
				loadVersion100(zip);
			}
		}
		private void loadVersion100(Package zip) {
			PackagePart part = zip.GetPart(new Uri("/tactic.xml", UriKind.Relative));
			Stream zipStream = part.GetStream(FileMode.Open, FileAccess.Read);

			StreamReader sr = new StreamReader(zipStream);
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
					Convert.ToDouble(XN.Attributes["X"].InnerText.Replace(',', '.'), CultureInfo.InvariantCulture),
					Convert.ToDouble(XN.Attributes["Y"].InnerText.Replace(',', '.'), CultureInfo.InvariantCulture)
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
							Convert.ToDouble(XNLtemp.Item(j).Attributes["X"].InnerText.Replace(',', '.'), CultureInfo.InvariantCulture),
							Convert.ToDouble(XNLtemp.Item(j).Attributes["Y"].InnerText.Replace(',', '.'), CultureInfo.InvariantCulture)
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
				dynamicTanks.Add(tank);
			}
		}
		private void loadVersion110(Package zip) {
			PackagePart part = zip.GetPart(new Uri("/dynamic/maps.xml", UriKind.Relative));
			Stream zipStream = part.GetStream(FileMode.Open, FileAccess.Read);

			StreamReader sr = new StreamReader(zipStream);
			XmlDocument XD = new XmlDocument();
			XD.LoadXml(sr.ReadToEnd());
			XmlNode XNDocument = XD.DocumentElement;
			XmlNode XN = XNDocument.SelectSingleNode("/mapData/map");
			if (XN.Attributes["BattleType"].InnerText != "") {
				setMap(
					XN.Attributes["id"].InnerText,
					(BattleType)Enum.Parse(typeof(BattleType), XN.Attributes["BattleType"].InnerText),
					XN.Attributes["Variation"].InnerText
					);
			} else {
				setMap(XN.Attributes["id"].InnerText);
			}
			timer = Convert.ToBoolean(XN.Attributes["timer"].InnerText);

			XmlNodeList XNL = XNDocument.SelectNodes("/mapData/map/staticIcons/icon");
			for (int i = 0; i < XNL.Count; i++) {
				StaticIcon staticIcon = icons.getStaticIcon(XNL.Item(i).SelectSingleNode("id").InnerText);
				XN = XNL.Item(i).SelectSingleNode("position");
				staticIcon.position = new Point(
					Convert.ToDouble(XN.Attributes["X"].InnerText.Replace(',', '.'), CultureInfo.InvariantCulture),
					Convert.ToDouble(XN.Attributes["Y"].InnerText.Replace(',', '.'), CultureInfo.InvariantCulture)
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
							Convert.ToDouble(XNLtemp.Item(j).Attributes["X"].InnerText.Replace(',', '.'), CultureInfo.InvariantCulture),
							Convert.ToDouble(XNLtemp.Item(j).Attributes["Y"].InnerText.Replace(',', '.'), CultureInfo.InvariantCulture)
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
				dynamicTanks.Add(tank);
			}
		}

		public void unserialize(Stream stream) {
			reset();

			StreamReader sr = new StreamReader(stream);
			XmlDocument XD = new XmlDocument();
			XD.LoadXml(sr.ReadToEnd());
			XmlNode XNDocument = XD.DocumentElement;
			XmlNode XN = XNDocument.SelectSingleNode("/mapData/map");
			if (XN.Attributes["BattleType"].InnerText != "") {
				setMap(
					XN.Attributes["id"].InnerText,
					(BattleType)Enum.Parse(typeof(BattleType), XN.Attributes["BattleType"].InnerText),
					XN.Attributes["Variation"].InnerText
					);
			} else {
				setMap(XN.Attributes["id"].InnerText);
			}
			timer = Convert.ToBoolean(XN.Attributes["timer"].InnerText);

			XmlNodeList XNL = XNDocument.SelectNodes("/mapData/map/staticIcons/icon");
			for (int i = 0; i < XNL.Count; i++) {
				StaticIcon staticIcon = icons.getStaticIcon(XNL.Item(i).SelectSingleNode("id").InnerText);
				XN = XNL.Item(i).SelectSingleNode("position");
				staticIcon.position = new Point(
					Convert.ToDouble(XN.Attributes["X"].InnerText.Replace(',', '.'), CultureInfo.InvariantCulture),
					Convert.ToDouble(XN.Attributes["Y"].InnerText.Replace(',', '.'), CultureInfo.InvariantCulture)
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
							Convert.ToDouble(XNLtemp.Item(j).Attributes["X"].InnerText.Replace(',', '.'), CultureInfo.InvariantCulture),
							Convert.ToDouble(XNLtemp.Item(j).Attributes["Y"].InnerText.Replace(',', '.'), CultureInfo.InvariantCulture)
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
				dynamicTanks.Add(tank);
			}
		}

		private void reset() {
			dynamicTanks.Clear();
			staticIcons.Clear();
			deselectItem();
			copyDynamicTank.Clear();
		}
	}
}
