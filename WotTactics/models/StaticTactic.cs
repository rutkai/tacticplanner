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
	class StaticTactic : AbstractTactic {
		class StaticMapEntry {
			public Bitmap map;
			public int time;
			public bool isClone;
			public int cloneOriginalTime;

			private Graphics graph;
			public Graphics getGraphics() {
				if (graph == null) {
					graph = Graphics.FromImage(map);
					graph.CompositingMode = CompositingMode.SourceCopy;
					graph.CompositingQuality = CompositingQuality.HighSpeed;
					graph.PixelOffsetMode = PixelOffsetMode.None;
					graph.SmoothingMode = SmoothingMode.None;
					graph.InterpolationMode = InterpolationMode.Default;
				}
				return graph;
			}
			public void removeGraphics() {
				graph.Dispose();
				graph = null;
			}

			public StaticMapEntry(int time, Bitmap map) {
				this.map = map;
				this.time = time;
				this.isClone = false;
				this.cloneOriginalTime = 0;
				this.graph = null;
			}
			public StaticMapEntry(int time, int cloneOriginalTime) {
				this.map = null;
				this.time = time;
				this.isClone = true;
				this.cloneOriginalTime = cloneOriginalTime;
				this.graph = null;
			}
		}

		private Dictionary<int, StaticMapEntry> staticTactics;

		private readonly Bitmap clearTactic;

		private Pen pen;
		private SolidBrush brush;
		private KeyValuePair<Point, Point> sampleLine;
		private KeyValuePair<Point, Point> sampleArrow;
		private bool hasSampleLine;
		private bool hasSampleArrow;

		public StaticTactic(Maps maps, Tanks tanks, Icons icons) : base(maps, tanks, icons) {
			staticTactics = new Dictionary<int, StaticMapEntry>();

			Bitmap clear = (Bitmap)TacticPlanner.Properties.Resources.clearTactics.Clone();
			((Bitmap)clear).MakeTransparent(((Bitmap)clear).GetPixel(1, 1));
			clearTactic = clear;

			brush = new SolidBrush(Color.Red);
			pen = new Pen(Color.Red);
			pen.StartCap = pen.EndCap = LineCap.Round;
			hasSampleLine = hasSampleArrow = false;
		}

		public Map getMap() {	// Ideiglenes
			return map;
		}

		public override Bitmap getTacticAt(int time) {
			if (staticTactics.ContainsKey(time)) {
				if (staticTactics[time].isClone) {
					return getTacticAt(staticTactics[time].cloneOriginalTime);
				} else {
					if (hasSampleLine) {
						Bitmap temp = (Bitmap)staticTactics[time].map.Clone();
						using (Graphics gr = Graphics.FromImage(temp)) {
							gr.DrawLine(pen, sampleLine.Key, sampleLine.Value);
							gr.Dispose();
						}
						return temp;
					} else if (hasSampleArrow) {
						Bitmap temp = (Bitmap)staticTactics[time].map.Clone();
						using (Graphics gr = Graphics.FromImage(temp)) {
							AdjustableArrowCap bigArrow = new AdjustableArrowCap(5, 5);
							pen.CustomEndCap = bigArrow;
							gr.DrawLine(pen, sampleArrow.Key, sampleArrow.Value);
							pen.EndCap = LineCap.Round;
							gr.Dispose();
						}
						return temp;
					} else {
						return staticTactics[time].map;
					}
				}
			} else {
				return clearTactic;
			}
		}

		public override Bitmap getPlayTacticAt(int time) {
			if (time == (time / 30) * 30) {
				return getTacticAt(time);
			} else {
				return null;
			}
		}

		public void removeTactic(int time) {
			staticTactics.Remove(time);
		}

		public void cloneTactic(int from, int to) {
			if (staticTactics.ContainsKey(to)) {
				staticTactics.Remove(to);
			}

			staticTactics.Add(to, new StaticMapEntry(to, from));
		}

		public void setPenColor(Color color) {
			brush.Color = color;
			pen.Color = color;
		}
		public void setThickness(int thickness) {
			pen.Width = thickness;
		}
		public void setDashStyle(DashStyle style) {
			pen.DashStyle = style;
		}

		private void prepareDraw(int time) {
			if (staticTactics.ContainsKey(time)) {
				if (staticTactics[time].isClone) {
					StaticMapEntry entry = staticTactics[time];
					entry.isClone = false;
					entry.map = (Bitmap)getTacticAt(entry.cloneOriginalTime).Clone();
					staticTactics[time] = entry;
				}
			} else {
				staticTactics.Add(time, new StaticMapEntry(time, (Bitmap)clearTactic.Clone()));
			}
		}

		private Point pointThicknessCorrection(Point p) {
			p.X -= (int)pen.Width / 2;
			p.Y -= (int)pen.Width / 2;
			return p;
		}

		public void drawLine(Point from, Point to, int time) {
			prepareDraw(time);
			removeSamples();

			staticTactics[time].getGraphics().DrawLine(pen, from, to);
		}

		public void drawArrow(Point from, Point to, int time) {
			prepareDraw(time);
			removeSamples();

			AdjustableArrowCap bigArrow = new AdjustableArrowCap(5, 5);
			pen.CustomEndCap = bigArrow;
			staticTactics[time].getGraphics().DrawLine(pen, from, to);
			pen.EndCap = LineCap.Round;
		}

		public void drawSampleLine(Point from, Point to, int time) {
			prepareDraw(time);
			removeSamples();

			sampleLine = new KeyValuePair<Point, Point>(from, to);
			hasSampleLine = true;
		}

		public void drawSampleArrow(Point from, Point to, int time) {
			prepareDraw(time);
			removeSamples();

			sampleArrow = new KeyValuePair<Point, Point>(from, to);
			hasSampleArrow = true;
		}

		public void removeSamples() {
			hasSampleLine = hasSampleArrow = false;
		}

		public void drawPoint(Point p, int time) {
			prepareDraw(time);
			p = pointThicknessCorrection(p);

			staticTactics[time].getGraphics().FillEllipse(brush, p.X, p.Y, pen.Width, pen.Width);
		}

		public void drawEraserPoint(Point p, int time) {
			prepareDraw(time);
			p = pointThicknessCorrection(p);

			Brush eraser = new SolidBrush(Color.FromArgb(255, 1, 255));
			staticTactics[time].getGraphics().FillEllipse(eraser, p.X, p.Y, (int)pen.Width, (int)pen.Width);
			staticTactics[time].removeGraphics();
			staticTactics[time].map.MakeTransparent(Color.FromArgb(255, 1, 255));
		}

		public void drawStamp(Point p, Bitmap stamp, int size, int time) {
			prepareDraw(time);
			p = pointThicknessCorrection(p);

			staticTactics[time].getGraphics().DrawImage(stamp, new Rectangle(p.X - size / 2, p.Y - (size * stamp.Height) / stamp.Width / 2, size, (size * stamp.Height) / stamp.Width));
		}

		public override void save(Package zip) {
			UTF8Encoding encoding = new UTF8Encoding();

			MemoryStream xmlString = new MemoryStream();
			XmlTextWriter xmlWriter = new XmlTextWriter(xmlString, Encoding.UTF8);
			xmlWriter.WriteStartDocument();
			xmlWriter.WriteComment("Tactic planner save file. DO NOT EDIT THIS FILE MANUALLY!");
			xmlWriter.WriteStartElement("static");

			xmlWriter.WriteAttributeString("version", "1.1.0");

			xmlWriter.WriteEndElement();
			xmlWriter.WriteEndDocument();
			xmlWriter.Close();

			PackagePart part = zip.CreatePart(new Uri("/static/static.xml", UriKind.Relative), System.Net.Mime.MediaTypeNames.Text.Xml, CompressionOption.Fast);
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
			xmlWriter.WriteStartElement("maps");
			foreach (StaticMapEntry item in staticTactics.Values) {
				xmlWriter.WriteStartElement("map");
				xmlWriter.WriteStartElement("time");
				xmlWriter.WriteValue(item.time);
				xmlWriter.WriteEndElement();
				xmlWriter.WriteStartElement("isClone");
				xmlWriter.WriteValue(Convert.ToString(item.isClone));
				xmlWriter.WriteEndElement();
				if (item.isClone) {
					xmlWriter.WriteStartElement("cloneOriginalTime");
					xmlWriter.WriteValue(item.cloneOriginalTime);
					xmlWriter.WriteEndElement();
				}
				xmlWriter.WriteEndElement();
			}
			xmlWriter.WriteEndElement();
			xmlWriter.WriteEndElement();

			xmlWriter.WriteEndElement();
			xmlWriter.WriteEndDocument();
			xmlWriter.Close();

			part = zip.CreatePart(new Uri("/static/maps.xml", UriKind.Relative), System.Net.Mime.MediaTypeNames.Text.Xml, CompressionOption.Fast);
			part.GetStream().Write(xmlString.ToArray(), 0, xmlString.ToArray().Length);
			ImageConverter converter = new ImageConverter();
			foreach (StaticMapEntry item in staticTactics.Values) {
				if (item.isClone)
					continue;

				part = zip.CreatePart(new Uri("/static/map-0/" + item.time.ToString() + ".png", UriKind.Relative), "image/png", CompressionOption.Fast);
				byte[] imgBuffer = (byte[])converter.ConvertTo(item.map, typeof(byte[]));
				part.GetStream().Write(imgBuffer, 0, imgBuffer.Length);
			}
		}

		public override void load(Package zip) {
			try {
				PackagePart part = zip.GetPart(new Uri("/static/static.xml", UriKind.Relative));
				loadVersion110(zip);
			} catch (InvalidOperationException) {
				loadVersion100(zip);
			}
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

			XmlNode XN = XNDocument.SelectSingleNode("/tactic/map");
			setMap(XN.Attributes["id"].InnerText);

			XN = XNDocument.SelectSingleNode("/tactic/staticMaps/settings/timer");
			timer = Convert.ToBoolean(XN.InnerText);

			XmlNodeList XNL = XNDocument.SelectNodes("/tactic/staticMaps/maps/map");
			for (int i = 0; i < XNL.Count; i++) {
				bool isClone = Convert.ToBoolean(XNL.Item(i).SelectSingleNode("isClone").InnerText);
				int time = Convert.ToInt32(XNL.Item(i).SelectSingleNode("time").InnerText);
				StaticMapEntry staticMap;
				if (isClone) {
					staticMap = new StaticMapEntry(
						time,
						Convert.ToInt32(XNL.Item(i).SelectSingleNode("cloneOriginalTime").InnerText)
					);
				} else {
					part = zip.GetPart(new Uri("/staticMaps/" + time.ToString() + ".png", UriKind.Relative));
					zipStream = part.GetStream(FileMode.Open, FileAccess.Read);
					MemoryStream imageStream = new MemoryStream();
					read = zipStream.Read(buffer, 0, buffer.Length);
					while (read > 0) {
						imageStream.Write(buffer, 0, buffer.Length);
						read = zipStream.Read(buffer, 0, buffer.Length);
					}
					staticMap = new StaticMapEntry(
						time,
						(Bitmap)Bitmap.FromStream(imageStream)
					);
				}
				staticTactics.Add(staticMap.time, staticMap);
			}
		}
		private void loadVersion110(Package zip) {
			PackagePart part = zip.GetPart(new Uri("/static/maps.xml", UriKind.Relative));
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

			XmlNodeList XNL = XNDocument.SelectNodes("/mapData/map/maps/map");
			for (int i = 0; i < XNL.Count; i++) {
				bool isClone = Convert.ToBoolean(XNL.Item(i).SelectSingleNode("isClone").InnerText);
				int time = Convert.ToInt32(XNL.Item(i).SelectSingleNode("time").InnerText);
				StaticMapEntry staticMap;
				if (isClone) {
					staticMap = new StaticMapEntry(
						time,
						Convert.ToInt32(XNL.Item(i).SelectSingleNode("cloneOriginalTime").InnerText)
					);
				} else {
					part = zip.GetPart(new Uri("/static/map-0/" + time.ToString() + ".png", UriKind.Relative));
					zipStream = part.GetStream(FileMode.Open, FileAccess.Read);
					MemoryStream imageStream = new MemoryStream();
					read = zipStream.Read(buffer, 0, buffer.Length);
					while (read > 0) {
						imageStream.Write(buffer, 0, buffer.Length);
						read = zipStream.Read(buffer, 0, buffer.Length);
					}
					staticMap = new StaticMapEntry(
						time,
						(Bitmap)Bitmap.FromStream(imageStream)
					);
				}
				staticTactics.Add(staticMap.time, staticMap);
			}
		}
	}
}
