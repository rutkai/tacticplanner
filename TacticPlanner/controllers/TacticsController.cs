using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using TacticPlanner.models;
using TacticPlanner.types;

namespace TacticPlanner.controllers {
	class TacticsController {
		private Tactic tactic;
		private MainWindow mainwindow;

		private Maps maps;
		private Tanks tanks;
		private Icons icons;

		public TacticsController(MainWindow window, string basePath) {
			mainwindow = window;
			tanks = new Tanks(basePath + "\\stamps\\tanks\\tanks.xml");
			icons = new Icons(basePath + "\\stamps\\icons\\icons.xml");
			maps = new Maps(basePath + "\\maps\\maps.xml", icons);
		}

		public Tactic getTactic() {
			if (tactic == null) {
				tactic = new Tactic(maps, tanks, icons, "1");
			}
			return tactic;
		}

		public Map[] getMaps() {
			return maps.getSortedMaps();
		}

		public List<StaticIcon> getStaticIcons() {
			return icons.getStaticIconList();
		}

		public List<DynamicIcon> getDynamicIcons() {
			return icons.getDynamicIconList();
		}

		public void add(string map) {
			tactic = new Tactic(maps, tanks, icons, map);
			mainwindow.initFromTactic();
		}

		public void load(string path) {
			tactic = new Tactic(maps, tanks, icons, path);
			mainwindow.initFromTactic();
		}

		public void save(string path) {
			tactic.save(path);
		}

		public bool isLoaded() {
			return tactic != null;
		}

		public Map getMap() {
			return tactic.getMap();
		}

		public void setMapPack(MapPack pack) {
			maps.setMapPack(pack);
		}

		public void setBattleType(BattleType type, string variation) {
			if (isLoaded()) {
				tactic.setBattleType(type, variation);
				mainwindow.setBattletype(type, variation);
			}
		}

		public BattleType getBattleType() {
			return tactic.getBattleType();
		}

		public string getBattleVariation() {
			return tactic.getBattleVariation();
		}

		public void initFromTactics() {
			mainwindow.initFromTactic();
		}

		public Tanks getTanksObj() {
			return tanks;
		}

		public DynamicIcon getDynamicIcon(string id) {
			return icons.getDynamicIcon(id);
		}

		public void setDynamicPenColor(Color color) {
			tactic.setDynamicPenColor(color);
			mainwindow.refreshMap();
		}

		public void setDynamicIconSize(int size) {
			tactic.setDynamicIconSize(size);
			mainwindow.refreshMap();
			mainwindow.refreshStaticMap();
		}

		public void setShowTankName(bool show) {
			if (isLoaded()) {
				tactic.setShowTankName(show);
				mainwindow.refreshMap();
			}
		}

		public void setShowPlayerName(bool show) {
			if (isLoaded()) {
				tactic.setShowPlayerName(show);
				mainwindow.refreshMap();
			}
		}

		public void setTankIcon(DisplayTankIcon icon) {
			if (isLoaded()) {
				tactic.setTankIcon(icon);
			}
			mainwindow.refreshMap();
		}

		public DynamicTank[] getTanks() {
			return tactic.getTanks();
		}

		public bool hasStaticTimer() {
			return tactic.hasStaticTimer();
		}

		public bool hasDynamicTimer() {
			return tactic.hasDynamicTimer();
		}

		public void setStaticTimer(bool enabled) {
			tactic.setStaticTimer(enabled);
			mainwindow.refreshNoTimer();
		}

		public void setDynamicTimer(bool enabled) {
			tactic.setDynamicTimer(enabled);
			mainwindow.refreshNoTimer();
		}

		public bool hasStaticIcon(StaticIcon icon) {
			return tactic.hasStaticIcon(icon);
		}

		public void addStaticIcon(StaticIcon icon) {
			tactic.addStaticIcon(icon);
			mainwindow.refreshDynamicPanel();
			mainwindow.refreshMap();
		}

		public void removeStaticIcon(StaticIcon icon) {
			tactic.removeStaticIcon(icon);
			mainwindow.refreshDynamicPanel();
			mainwindow.refreshMap();
		}

		public void addTank(DynamicTank tank) {
			tactic.addTank(tank);
			mainwindow.refreshMap();
			mainwindow.refreshDynamicTactic();
		}

		public void editTank(DynamicTank tank) {
			mainwindow.refreshMap();
			mainwindow.refreshDynamicTactic();
		}

		public void removeTank(DynamicTank tank) {
			tactic.removeTank(tank);
			mainwindow.refreshMap();
			mainwindow.refreshDynamicTactic();
		}

		public void removePosition(DynamicTank tank, int time) {
			tactic.removePosition(tank, time);
			mainwindow.refreshMap();
		}

		public string getTankActionId(DynamicTank tank, int time) {
			return tactic.getTankActionId(tank, time);
		}

		public void setTankActionId(DynamicTank tank, int time, string action) {
			tactic.setTankActionId(tank, time, action);
			mainwindow.refreshMap();
		}

		public bool isAlive(DynamicTank tank, int time) {
			return tactic.isAlive(tank, time);
		}

		public void setKill(DynamicTank tank, int time) {
			tactic.setKill(tank, time);
			mainwindow.refreshDynamicPanel();
			mainwindow.refreshMap();
		}

		public bool selectIcon(Point p, bool multiselect, int time) {
			bool result = tactic.selectIcon(p, multiselect, time);
			mainwindow.refreshMap();
			return result;
		}

		public bool hasSelectedIcon() {
			return tactic.hasSelectedIcon();
		}

		public bool isSelectedCopyable() {
			return tactic.isSelectedCopyable();
		}

		public void copy() {
			tactic.copy();
		}

		public void paste() {
			tactic.paste();
			mainwindow.refreshMap();
			mainwindow.refreshDynamicTactic();
		}

		public void moveIcons(Point from, Point to, int time) {
			tactic.moveIcons(from, to, time);
			mainwindow.refreshMap();
		}

		public void deselectIcon(Point p, int time) {
			tactic.deselectIcon(p, time);
			mainwindow.refreshMap();
		}

		public void deselectIcon() {
			tactic.deselectIcon();
			mainwindow.refreshMap();
		}

		public void serializeDynamicTactic(System.IO.Stream stream) {
			tactic.serializeDynamicTactic(stream);
		}

		public void unserializeDynamicTactic(System.IO.Stream stream) {
			tactic.unserializeDynamicTactic(stream);
			mainwindow.refreshDynamicTactic();
		}

		public ImageSource getStaticImage(int time) {
			return tactic.getStaticImage(time);
		}

		public ImageSource getDynamicImage(int time) {
			return tactic.getDynamicImage(time);
		}

		public ImageSource getStaticPlayImage(int time) {
			return tactic.getStaticPlayImage(time);
		}

		public ImageSource getDynamicPlayImage(int time) {
			return tactic.getDynamicPlayImage(time);
		}

		public void removeDraw(int time) {
			tactic.removeDraw(time);
			mainwindow.refreshMap();
		}

		public void cloneTactic(int from, int to) {
			tactic.cloneTactic(from, to);
			mainwindow.refreshMap();
		}

		public void drawSampleLine(Point from, Point to, Color color, int thickness, DashStyle dash, int time) {
			tactic.drawSampleLine(from, to, color, thickness, dash, time);
			mainwindow.refreshMap();
		}

		public void drawSampleArrow(Point from, Point to, Color color, int thickness, DashStyle dash, int time) {
			tactic.drawSampleArrow(from, to, color, thickness, dash, time);
			mainwindow.refreshMap();
		}

		public void drawSampleStamp(Point p, BitmapImage img, int size, int time) {
			tactic.drawSampleStamp(p, img, size, time);
			mainwindow.refreshMap();
		}

		public void removeSamples() {
			tactic.removeSamples();
			mainwindow.refreshMap();
		}

		public void drawLine(Point from, Point to, Color color, int thickness, DashStyle dash, int time) {
			tactic.drawLine(from, to, color, thickness, dash, time);
			mainwindow.refreshMap();
		}

		public void drawArrow(Point from, Point to, Color color, int thickness, DashStyle dash, int time) {
			tactic.drawArrow(from, to, color, thickness, dash, time);
			mainwindow.refreshMap();
		}

		public void drawStamp(Point p, BitmapImage img, int size, int time) {
			tactic.drawStamp(p, img, size, time);
			mainwindow.refreshMap();
		}

		public void drawPoint(Point p, Color color, int thickness, int time) {
			tactic.drawPoint(p, color, thickness, time);
			mainwindow.refreshMap();
		}

		public void drawPoints(Point[] ps, Color color, int thickness, int time) {
			for (int i = 0; i < ps.Length; i++) {
				tactic.drawPoint(ps[i], color, thickness, time);
			}
			mainwindow.refreshMap();
		}

		public void drawEraserPoint(Point p, int thickness, int time) {
			tactic.drawEraserPoint(p, thickness, time);
			mainwindow.refreshMap();
		}

		public void drawEraserPoints(Point[] ps, int thickness, int time) {
			for (int i = 0; i < ps.Length; i++) {
				tactic.drawEraserPoint(ps[i], thickness, time);
			}
			mainwindow.refreshMap();
		}


	}
}
