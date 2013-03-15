using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using System.IO.Packaging;
using System.Xml;

using TacticPlanner.models;
using TacticPlanner.types;

namespace TacticPlanner.models {
	class Tactic {
		private Maps maps;
		private Tanks tanks;
		private Icons icons;

		private Map map;

		private StaticTactic staticTactic;
		private DynamicTactic dynamicTactic;

		public Tactic(Maps maps, Tanks tanks, Icons icons, string map_or_path) {
			this.maps = maps;
			this.tanks = tanks;
			this.icons = icons;

			int mapId;
			if (int.TryParse(map_or_path, out mapId)) {
				staticTactic = new StaticTactic(maps, tanks, icons);
				dynamicTactic = new DynamicTactic(maps, tanks, icons);
				staticTactic.setMap(map_or_path);
				dynamicTactic.setMap(map_or_path);

				this.map = maps.getMap(map_or_path);
			} else {
				load(map_or_path);
			}
		}

		public Map getMap() {
			return map;
		}

		public void setMapPack(MapPack pack) {
			map.mapPack = pack;
		}

		public void setBattleType(BattleType type, string variation) {
			map.Battletype = type;
			map.Variation = variation;
		}

		public BattleType getBattleType() {
			return map.Battletype;
		}

		public string getBattleVariation() {
			return map.Variation;
		}

		public void setDynamicPenColor(Color color) {
			dynamicTactic.setPenColor(color);
		}

		public void setDynamicIconSize(int size) {
			dynamicTactic.setIconsSize(size);
			map.iconsSize = size;
		}

		public void setShowTankName(bool show) {
			dynamicTactic.ShowTankName = show;
		}

		public void setShowPlayerName(bool show) {
			dynamicTactic.ShowPlayerName = show;
		}

		public DynamicTank[] getTanks() {
			return dynamicTactic.getDynamicTanks();
		}

		public bool hasStaticTimer() {
			return staticTactic.timer;
		}

		public bool hasDynamicTimer() {
			return dynamicTactic.timer;
		}

		public void setStaticTimer(bool enabled) {
			staticTactic.timer = enabled;
		}

		public void setDynamicTimer(bool enabled) {
			dynamicTactic.timer = enabled;
		}

		public bool hasStaticIcon(StaticIcon icon) {
			return dynamicTactic.hasStaticElement(icon);
		}

		public void addStaticIcon(StaticIcon icon) {
			dynamicTactic.addStaticElement(icon);
		}

		public void removeStaticIcon(StaticIcon icon) {
			dynamicTactic.removeStaticElement(icon);
		}

		public void addTank(DynamicTank tank) {
			dynamicTactic.addDynamicTank(tank);
		}

		public void removeTank(DynamicTank tank) {
			dynamicTactic.removeDynamicTank(tank);
		}

		public void removePosition(DynamicTank tank, int time) {
			dynamicTactic.removeDynamicPosition(tank, time);
		}

		public string getTankActionId(DynamicTank tank, int time) {
			return dynamicTactic.getDynamicTankActionId(tank, time);
		}

		public void setTankActionId(DynamicTank tank, int time, string action) {
			dynamicTactic.setDynamicTankAction(tank, time, action);
		}

		public bool isAlive(DynamicTank tank, int time) {
			return dynamicTactic.isAlive(tank, time);
		}

		public void setKill(DynamicTank tank, int time) {
			dynamicTactic.setKill(tank, time);
		}

		public bool selectIcon(Point p, bool multiselect, int time) {
			return dynamicTactic.selectItem(p, multiselect, time);
		}

		public bool hasSelectedIcon() {
			return dynamicTactic.hasSelectedItem();
		}

		public bool isSelectedCopyable() {
			return dynamicTactic.isSelectedCopyable();
		}

		public void copy() {
			dynamicTactic.copySelected();
		}

		public void paste() {
			dynamicTactic.paste();
		}

		public void moveIcons(Point from, Point to, int time) {
			dynamicTactic.moveItem(from, to, time);
		}

		public void deselectIcon(Point p, int time) {
			dynamicTactic.deselectItem(p, time);
		}

		public void deselectIcon() {
			dynamicTactic.deselectItem();
		}

		public void serializeDynamicTactic(System.IO.Stream stream) {
			dynamicTactic.serialize(stream);
		}

		public void unserializeDynamicTactic(System.IO.Stream stream) {
			dynamicTactic.unserialize(stream);
		}

		public ImageSource getStaticImage(int time) {
			return staticTactic.getTacticAt(time);
		}

		public ImageSource getDynamicImage(int time) {
			return dynamicTactic.getTacticAt(time);
		}

		public ImageSource getStaticPlayImage(int time) {
			return staticTactic.getPlayTacticAt(time);
		}

		public ImageSource getDynamicPlayImage(int time) {
			return dynamicTactic.getPlayTacticAt(time);
		}

		public void removeDraw(int time) {
			staticTactic.removeDraw(time);
		}

		public void cloneTactic(int from, int to) {
			staticTactic.cloneTactic(from, to);
		}

		public void drawSampleLine(Point from, Point to, Color color, int thickness, DashStyle dash, int time) {
			staticTactic.drawSampleLine(from, to, color, thickness, dash, time);
		}

		public void drawSampleArrow(Point from, Point to, Color color, int thickness, DashStyle dash, int time) {
			staticTactic.drawSampleArrow(from, to, color, thickness, dash, time);
		}

		public void drawSampleStamp(Point p, BitmapImage img, int size, int time) {
			staticTactic.drawSampleStamp(p, img, size, time);
		}

		public void removeSamples() {
			staticTactic.removeSamples();
		}

		public void drawLine(Point from, Point to, Color color, int thickness, DashStyle dash, int time) {
			staticTactic.drawLine(from, to, color, thickness, dash, time);
		}

		public void drawArrow(Point from, Point to, Color color, int thickness, DashStyle dash, int time) {
			staticTactic.drawArrow(from, to, color, thickness, dash, time);
		}

		public void drawStamp(Point p, BitmapImage img, int size, int time) {
			staticTactic.drawStamp(p, img, size, time);
		}

		public void drawPoint(Point p, Color color, int thickness, int time) {
			staticTactic.drawPoint(p, color, thickness, time);
		}

		public void drawEraserPoint(Point p, int thickness, int time) {
			staticTactic.drawEraserPoint(p, thickness, time);
		}

		public void save(string path) {
			if (File.Exists(path)) {
				File.Delete(path);
			}

			FileStream fs = new FileStream(path, FileMode.Create);
			serialize(fs);
			fs.Close();
		}

		public void serialize(Stream stream) {
			Package zip = ZipPackage.Open(stream, FileMode.Create, FileAccess.ReadWrite);

			MemoryStream xmlString = new MemoryStream();
			XmlTextWriter xmlWriter = new XmlTextWriter(xmlString, Encoding.UTF8);
			xmlWriter.WriteStartDocument();
			xmlWriter.WriteComment("Tactic planner save file. DO NOT EDIT THIS FILE MANUALLY!");
			xmlWriter.WriteStartElement("tactic");
			xmlWriter.WriteAttributeString("version", "1.1.0");
			xmlWriter.WriteAttributeString("game", "World of Tanks");
			xmlWriter.WriteEndElement();
			xmlWriter.WriteEndDocument();
			xmlWriter.Close();

			PackagePart part = zip.CreatePart(new Uri("/tactic.xml", UriKind.Relative), System.Net.Mime.MediaTypeNames.Text.Xml, CompressionOption.Fast);
			part.GetStream().Write(xmlString.ToArray(), 0, xmlString.ToArray().Length);

			staticTactic.save(zip);
			dynamicTactic.save(zip);
			zip.Close();
		}

		protected void load(string path) {
			if (!File.Exists(path)) {
				throw new FileNotFoundException();
			}

			FileStream fs = new FileStream(path, FileMode.Open);
			unserialize(fs);
			fs.Close();
		}

		public void unserialize(Stream stream) {
			staticTactic = new StaticTactic(maps, tanks, icons);
			dynamicTactic = new DynamicTactic(maps, tanks, icons);

			Package zip = ZipPackage.Open(stream, FileMode.Open, FileAccess.Read);
			staticTactic.load(zip);
			dynamicTactic.load(zip);
			zip.Close();

			this.map = staticTactic.getMap();
		}
	}
}
