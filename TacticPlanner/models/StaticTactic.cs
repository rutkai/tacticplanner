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

namespace TacticPlanner.models {
	class StaticTactic : AbstractTactic {
		class StaticMapEntry {
			public Drawing map;
			public int time;
			public bool isClone;
			public int cloneOriginalTime;

			public StaticMapEntry(int time, BitmapSource map) {
				this.map = new Drawing(map);
				this.time = time;
				this.isClone = false;
				this.cloneOriginalTime = 0;
			}
			public StaticMapEntry(int time, int cloneOriginalTime) {
				this.map = null;
				this.time = time;
				this.isClone = true;
				this.cloneOriginalTime = cloneOriginalTime;
			}
		}
		struct SampleStamp {
			public BitmapSource stamp;
			public int size;
			 public Point p;
		}

		private Dictionary<int, StaticMapEntry> staticTactics;

		private readonly BitmapSource clearTactic;

		private Pen pen;
		private Color penColor;
		private KeyValuePair<Point, Point> sample;
		private SampleStamp sampleStamp;
		private bool hasSampleLine = false, hasSampleArrow = false;

		public StaticTactic(Maps maps, Tanks tanks, Icons icons) : base(maps, tanks, icons) {
			staticTactics = new Dictionary<int, StaticMapEntry>();

			var source = new Uri(@"pack://application:,,,/Resources/clearTactics.png", UriKind.Absolute);
			clearTactic = new BitmapImage(source);  

			penColor = Color.FromRgb(255, 0, 0);
			pen = new Pen(new SolidColorBrush(penColor), 5);
			pen.StartLineCap = pen.EndLineCap = PenLineCap.Round;
			pen.Freeze();
		}

		public Map getMap() {	// Ideiglenes
			return map;
		}

		public override ImageSource getTacticAt(int time) {
			if (staticTactics.ContainsKey(time)) {
				if (staticTactics[time].isClone) {
					return getTacticAt(staticTactics[time].cloneOriginalTime);
				} else {
					if (hasSampleLine) {
						DrawingGroup drawing = new DrawingGroup();
						drawing.Children.Add(new ImageDrawing(staticTactics[time].map.getImage(), new Rect(0, 0, 1024, 1024)));
						drawing.Children.Add(new GeometryDrawing(pen.Brush, pen, new LineGeometry(sample.Key, sample.Value)));
						drawing.ClipGeometry = new RectangleGeometry(new Rect(0, 0, 1024, 1024));
						drawing.Freeze();
						return new DrawingImage(drawing);
					} else if (hasSampleArrow) {
						Pen pen = this.pen.Clone();
						pen.EndLineCap = PenLineCap.Triangle;
						DrawingGroup drawing = new DrawingGroup();
						drawing.Children.Add(new ImageDrawing(staticTactics[time].map.getImage(), new Rect(0, 0, 1024, 1024)));
						drawing.Children.Add(new GeometryDrawing(pen.Brush, pen, Drawing.makeArrowGeometry(sample.Key, sample.Value, pen.Thickness)));
						drawing.ClipGeometry = new RectangleGeometry(new Rect(0, 0, 1024, 1024));
						drawing.Freeze();
						return new DrawingImage(drawing);
					} else if (sampleStamp.size != 0) {
						int stampWidth, stampHeight;
						if (sampleStamp.stamp.PixelHeight > sampleStamp.stamp.PixelWidth) {
							stampHeight = sampleStamp.size;
							stampWidth = (int)((double)sampleStamp.size * ((double)sampleStamp.stamp.PixelWidth / (double)sampleStamp.stamp.PixelHeight));
						} else {
							stampHeight = (int)((double)sampleStamp.size * ((double)sampleStamp.stamp.PixelHeight / (double)sampleStamp.stamp.PixelWidth));
							stampWidth = sampleStamp.size;
						}
						DrawingGroup drawing = new DrawingGroup();
						drawing.Children.Add(new ImageDrawing(staticTactics[time].map.getImage(), new Rect(0, 0, 1024, 1024)));
						drawing.Children.Add(new ImageDrawing(sampleStamp.stamp, new Rect(sampleStamp.p.X - stampWidth / 2, sampleStamp.p.Y - stampHeight / 2, stampWidth, stampHeight)));
						drawing.ClipGeometry = new RectangleGeometry(new Rect(0, 0, 1024, 1024));
						drawing.Freeze();
						return new DrawingImage(drawing);
					} else {
						return staticTactics[time].map.getImage();
					}
				}
			} else {
				return clearTactic;
			}
		}

		public override ImageSource getPlayTacticAt(int time) {
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
			penColor = color;
			pen = new Pen(new SolidColorBrush(penColor), pen.Thickness);
			pen.Freeze();
		}
		public void setThickness(int thickness) {
			pen = new Pen(new SolidColorBrush(penColor), thickness);
			pen.Freeze();
		}
		public void setDashStyle(DashStyle style) {
			pen = new Pen(new SolidColorBrush(penColor), pen.Thickness);
			pen.DashStyle = style;
			pen.Freeze();
		}

		private void prepareDraw(int time) {
			if (staticTactics.ContainsKey(time)) {
				if (staticTactics[time].isClone) {
					StaticMapEntry entry = staticTactics[time];
					entry.isClone = false;
					int originalTime = entry.cloneOriginalTime;
					while (staticTactics[originalTime].isClone) {
						originalTime = staticTactics[originalTime].cloneOriginalTime;
					}
					entry.map = staticTactics[originalTime].map.Clone();
					staticTactics[time] = entry;
				}
			} else {
				staticTactics.Add(time, new StaticMapEntry(time, clearTactic.Clone()));
			}
		}

		private Point pointThicknessCorrection(Point p) {
			p.X -= (int)pen.Thickness / 2;
			p.Y -= (int)pen.Thickness / 2;
			return p;
		}

		public void drawLine(Point from, Point to, int time) {
			prepareDraw(time);
			removeSamples();

			staticTactics[time].map.drawLine(from, to, pen, penColor);
		}

		public void drawArrow(Point from, Point to, int time) {
			prepareDraw(time);
			removeSamples();

			staticTactics[time].map.drawArrow(from, to, pen, penColor);
		}

		public void drawSampleLine(Point from, Point to, int time) {
			prepareDraw(time);
			removeSamples();

			sample = new KeyValuePair<Point, Point>(from, to);
			hasSampleLine = true;
		}

		public void drawSampleArrow(Point from, Point to, int time) {
			prepareDraw(time);
			removeSamples();

			sample = new KeyValuePair<Point, Point>(from, to);
			hasSampleArrow = true;
		}

		public void removeSamples() {
			hasSampleLine = hasSampleArrow = false;
			sampleStamp = new SampleStamp();
		}

		public void drawPoint(Point p, int time) {
			prepareDraw(time);

			staticTactics[time].map.drawPoint(p, pen, penColor);
		}

		public void drawEraserPoint(Point p, int time) {
			prepareDraw(time);

			staticTactics[time].map.drawEraser(p, pen, penColor);
		}

		public void drawStamp(Point p, BitmapSource stamp, int size, int time) {
			prepareDraw(time);

			staticTactics[time].map.drawStamp(p, stamp, size);
		}

		public void drawSampleStamp(Point p, BitmapSource stamp, int size, int time) {
			prepareDraw(time);

			sampleStamp.stamp = stamp;
			sampleStamp.size = size;
			sampleStamp.p = p;
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
			foreach (StaticMapEntry item in staticTactics.Values) {
				if (item.isClone)
					continue;

				part = zip.CreatePart(new Uri("/static/map-0/" + item.time.ToString() + ".png", UriKind.Relative), "image/png", CompressionOption.Fast);
				PngBitmapEncoder encoder = new PngBitmapEncoder();
				encoder.Frames.Add(BitmapFrame.Create(item.map.getImage()));
				encoder.Save(part.GetStream());
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
					BitmapImage img = new BitmapImage();
					img.BeginInit();
					img.StreamSource = imageStream;
					img.EndInit();
					staticMap = new StaticMapEntry(
						time,
						img
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
					BitmapImage img = new BitmapImage();
					img.BeginInit();
					img.StreamSource = imageStream;
					img.EndInit();
					staticMap = new StaticMapEntry(
						time,
						img
					);
				}
				staticTactics.Add(staticMap.time, staticMap);
			}
		}
	}
}
