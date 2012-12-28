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
    class Tactic {
        struct staticMapEntry {
            public Bitmap map;
            public int time;
            public bool isClone;
            public int cloneOriginalTime;
        }

        public Map map { get; set; }
        public bool noStaticTimer { get; set; }
        public bool noDynamicTimer { get; set; }

        private Dictionary<int, staticMapEntry> staticTactics;
        private readonly Bitmap clearTactic;
        private readonly string exePath;

        private Dictionary<string, DynamicTank> dynamicTanks;
        private List<StaticIcon> dynamicStaticIcons;

        private Maps maps;
        private Tanks tanks;
        private Icons icons;
        private Quality quality;

        public Tactic(Maps maps, Tanks tanks, Icons icons, Quality quality = Quality.High) {
            this.maps = maps;
            this.tanks = tanks;
            this.icons = icons;
            this.quality = quality;

            staticTactics = new Dictionary<int, staticMapEntry>();

            Bitmap clear = (Bitmap)TacticPlanner.Properties.Resources.clearTactics.Clone();
            ((Bitmap)clear).MakeTransparent(((Bitmap)clear).GetPixel(1, 1));
            clearTactic = clear;
            exePath = System.Windows.Forms.Application.StartupPath + "\\";

            dynamicTanks = new Dictionary<string, DynamicTank>();
            dynamicStaticIcons = new List<StaticIcon>();

            noStaticTimer = noStaticTimer = false;
        }

        public Bitmap getStaticTacticAt(int time) {
            if (staticTactics.ContainsKey(time)) {
                if (staticTactics[time].isClone) {
                    return getStaticTacticAt(staticTactics[time].cloneOriginalTime);
                } else {
                    return staticTactics[time].map;
                }

                throw new Exception("Internal error");
            } else {
                return getClearImage();
            }
        }

        public Bitmap getDynamicTacticAt(int time, Color textColor, int iconSize = 50, bool showPlayername = true, bool showTankname = true) {
            Bitmap img = (Bitmap)clearTactic.Clone();
            
            using (Graphics gr = Graphics.FromImage(img)) {
                switch (quality) {
                    case Quality.High:
                        gr.SmoothingMode = SmoothingMode.HighQuality;
                        gr.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        gr.PixelOffsetMode = PixelOffsetMode.HighQuality;
                        break;
                    case Quality.Medium:
                        gr.SmoothingMode = SmoothingMode.HighSpeed;
                        gr.InterpolationMode = InterpolationMode.HighQualityBilinear;
                        gr.PixelOffsetMode = PixelOffsetMode.HighSpeed;
                        break;
                    case Quality.Low:
                        gr.SmoothingMode = SmoothingMode.None;
                        gr.InterpolationMode = InterpolationMode.Low;
                        gr.PixelOffsetMode = PixelOffsetMode.None;
                        break;
                }
                foreach (StaticIcon staticon in dynamicStaticIcons) {
                    Bitmap icon = (Bitmap)Bitmap.FromFile(exePath + "stamps\\icons\\" + staticon.filename);
                    gr.DrawImage(icon, new Rectangle(staticon.position.X - iconSize / 2, staticon.position.Y - iconSize / 2, iconSize, (iconSize * icon.Height) / icon.Width));
                }

                Font font = new Font("Arial", iconSize / 2, FontStyle.Bold, GraphicsUnit.Pixel);
                Brush brush = new SolidBrush(textColor);
                foreach (DynamicTank tank in dynamicTanks.Values) {
                    Bitmap icon;
                    if (tank.killTime < time) {
                        icon = (Bitmap)Bitmap.FromFile(exePath + "stamps\\icons\\" + icons.getTankIcon(tank.tank.type, tank.isAlly).aliveFilename);
                    } else {
                        icon = (Bitmap)Bitmap.FromFile(exePath + "stamps\\icons\\" + icons.getTankIcon(tank.tank.type, tank.isAlly).deadFilename);
                    }

                    int roundTime = (int)(Math.Ceiling((float)time / 30.0) * 30.0);
                    Bitmap actionIcon = null;
                    if (tank.actions.ContainsKey(roundTime)) {
                        actionIcon = (Bitmap)Bitmap.FromFile(exePath + "stamps\\icons\\" + icons.getDynamicIcon(tank.actions[roundTime]).filename);
                    }
                    
                    while (!tank.positions.ContainsKey(roundTime)) {
                        roundTime += 30;
                    }

                    Point from = tank.positions[roundTime];
                    int fromTime = roundTime;

                    roundTime = (time / 30) * 30;
                    while (!tank.positions.ContainsKey(roundTime) && (roundTime >= 0)) {
                        roundTime -= 30;
                    }
                    Point to;
                    int toTime;
                    if (roundTime < 0) {
                        to = from;
                        toTime = fromTime;
                    } else {
                        to = tank.positions[roundTime];
                        toTime = roundTime;
                    }

                    Point center;
                    if (from == to) {
                        center = from;
                    } else {
                        center = new Point(
                            from.X - (int)((float)(from.X - to.X) * Math.Abs((float)(fromTime - time) / (float)(fromTime - toTime))),
                            from.Y - (int)((float)(from.Y - to.Y) * Math.Abs((float)(fromTime - time) / (float)(fromTime - toTime)))
                        );
                    }

                    if (icon.Height < icon.Width) {
                        gr.DrawImage(icon, new Rectangle(center.X - iconSize / 2, center.Y - iconSize / 2, iconSize, (iconSize * icon.Height) / icon.Width));
                    } else {
                        gr.DrawImage(icon, new Rectangle(center.X - iconSize / 2, center.Y - iconSize / 2, (iconSize * icon.Width) / icon.Height, iconSize));
                    }
                    if (actionIcon != null) {
                        gr.DrawImage(actionIcon, new Rectangle(center.X - iconSize - 5, center.Y - iconSize + 3, iconSize * 2, (iconSize * actionIcon.Height * 2) / actionIcon.Width));
                    }
                    if (showPlayername || showTankname) {
                        string text;
                        if (showPlayername && showTankname) {
                            text = tank.name + " (" + tank.tank.name + ")";
                        } else if (showPlayername) {
                            text = tank.name;
                        } else {
                            text = tank.tank.name;
                        }
                        gr.DrawString(text, font, brush, center.X - (text.Length * (font.Size + 3)) / 4 - 10, center.Y + iconSize / 2);
                    }
                }
                font.Dispose();
                brush.Dispose();
            }

            GC.Collect();

            return img;
        }

        public Bitmap getPlayStaticTacticAt(int time) {
            if (time == (time / 30) * 30) {
                return getStaticTacticAt(time);
            } else {
                return null;
            }
        }

        public Bitmap getPlayDynamicTacticAt(int time, Color textColor, int iconSize = 50, bool showPlayername = true, bool showTankname = true) {
            return getDynamicTacticAt(time, textColor, iconSize, showPlayername, showTankname);
        }

        public void makeStaticEmpty(int time) {
            staticTactics.Remove(time);
        }

        public void makeStaticClone(int from, int to) {
            if (staticTactics.ContainsKey(to)) {
                staticTactics.Remove(to);
            }

            staticMapEntry entry = new staticMapEntry();
            entry.isClone = true;
            entry.cloneOriginalTime = from;
            entry.time = to;
            staticTactics.Add(to, entry);
        }

        public void drawStaticLine(Point from, Point to, int thickness, Color color, DashStyle dash, int time) {
            prepareDraw(time);
            staticMapEntry entry = staticTactics[time];

            Pen pen = new Pen(color, thickness);
            pen.DashStyle = dash;
            using (Graphics g = Graphics.FromImage(entry.map)) {
                g.DrawLine(pen, from, to);
                pen.Dispose();
                g.Dispose();
            }

            staticTactics[time] = entry;
        }

        public void drawStaticArrow(Point from, Point to, int thickness, Color color, DashStyle dash, int time) {
            prepareDraw(time);
            staticMapEntry entry = staticTactics[time];

            Pen pen = new Pen(color, thickness);
            pen.DashStyle = dash;
            using (Graphics g = Graphics.FromImage(entry.map)) {
                AdjustableArrowCap bigArrow = new AdjustableArrowCap(5, 5);
                pen.StartCap = LineCap.Round;
                pen.CustomEndCap = bigArrow;
                g.DrawLine(pen, from, to);
                pen.Dispose();
                g.Dispose();
            }

            staticTactics[time] = entry;
        }

        public void drawStaticPoints(List<Point> p, int thickness, Color color, int time) {
            prepareDraw(time);
            staticMapEntry entry = staticTactics[time];

            Brush brush = new SolidBrush(color);
            using (Graphics g = Graphics.FromImage(entry.map)) {
                for (int i = 0; i < p.Count; i++) {
                    g.FillEllipse(brush, p[i].X - thickness / 2, p[i].Y - thickness / 2, thickness, thickness);
                }

                brush.Dispose();
                g.Dispose();
            }

            staticTactics[time] = entry;
        }

        public void drawStaticEraserPoints(List<Point> p, int thickness, int time) {
            prepareDraw(time);
            staticMapEntry entry = staticTactics[time];

            Brush brush = new SolidBrush(Color.FromArgb(255, 1, 255));
            using (Graphics g = Graphics.FromImage(entry.map)) {
                for (int i = 0; i < p.Count; i++) {
                    g.FillEllipse(brush, p[i].X - thickness / 2, p[i].Y - thickness / 2, thickness, thickness);
                    
                }

                brush.Dispose();
                g.Dispose();
            }

            entry.map.MakeTransparent(Color.FromArgb(255, 1, 255));
            staticTactics[time] = entry;
        }

        public void drawStaticStamp(Point p, Bitmap stamp, int size, int time) {
            prepareDraw(time);
            staticMapEntry entry = staticTactics[time];

            using (Graphics gr = Graphics.FromImage(entry.map)) {
                switch (quality) {
                    case Quality.High:
                        gr.SmoothingMode = SmoothingMode.HighQuality;
                        gr.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        gr.PixelOffsetMode = PixelOffsetMode.HighQuality;
                        break;
                    case Quality.Medium:
                        gr.SmoothingMode = SmoothingMode.HighSpeed;
                        gr.InterpolationMode = InterpolationMode.HighQualityBilinear;
                        gr.PixelOffsetMode = PixelOffsetMode.HighSpeed;
                        break;
                    case Quality.Low:
                        gr.SmoothingMode = SmoothingMode.None;
                        gr.InterpolationMode = InterpolationMode.Low;
                        gr.PixelOffsetMode = PixelOffsetMode.None;
                        break;
                }
                gr.DrawImage(stamp, new Rectangle(p.X - size / 2, p.Y - (size * stamp.Height) / stamp.Width / 2, size, (size * stamp.Height) / stamp.Width));
            }

            staticTactics[time] = entry;
        }

        private void prepareDraw(int time) {
            if (staticTactics.ContainsKey(time)) {
                if (staticTactics[time].isClone) {
                    staticMapEntry entry = staticTactics[time];
                    entry.isClone = false;
                    entry.map = (Bitmap)getStaticTacticAt(entry.cloneOriginalTime).Clone();
                    staticTactics[time] = entry;
                }
            } else {
                staticMapEntry entry = new staticMapEntry();
                entry.isClone = false;
                entry.time = time;
                entry.map = (Bitmap)clearTactic.Clone();
                staticTactics.Add(time, entry);
            }
        }

        public Bitmap getClearImage() {
            return clearTactic;
        }

        public Bitmap getMap() {
            if (map == null) {
                return null;
            } else {
                return (Bitmap)Bitmap.FromFile(exePath + "maps\\" + map.filename);
            }
        }

        public Size imageDimensions() {
            return new Size(
                clearTactic.Width,
                clearTactic.Height
            );
        }

        public bool addStaticElement(StaticIcon icon) {
            if (dynamicStaticIcons.Contains(icon)) {
                return false;
            } else {
                dynamicStaticIcons.Add(icon);
                return true;
            }
        }

        public bool removeStaticElement(string id) {
            bool result = false;

            for (int i = 0; i < dynamicStaticIcons.Count; i++) {
                if (dynamicStaticIcons[i].id == id) {
                    dynamicStaticIcons.RemoveAt(i);
                    result = true;
                    break;
                }
            }

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

        public void moveDynamicItem(Point from, Point to,  int time) {
            int bestDistance = int.MaxValue;
            StaticIcon staticIcon = null;
            DynamicTank dynamicTank = null;

            int distance;
            foreach (StaticIcon item in dynamicStaticIcons) {
                distance = (int)Math.Sqrt(Math.Pow(item.position.X - from.X, 2) + Math.Pow(item.position.Y - from.Y, 2));
                if (bestDistance > distance) {
                    bestDistance = distance;
                    staticIcon = item;
                }
            }

            int timeSearch;
            foreach (DynamicTank item in dynamicTanks.Values) {
                timeSearch = time;
                while (!item.positions.ContainsKey(timeSearch)) {
                    timeSearch += 30;
                }

                distance = (int)Math.Sqrt(Math.Pow(item.positions[timeSearch].X - from.X, 2) + Math.Pow(item.positions[timeSearch].Y - from.Y, 2));
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
                staticIcon.position = to;
            } else if (dynamicTank != null) {
                if (dynamicTank.positions.ContainsKey(time)) {
                    dynamicTank.positions[time] = to;
                } else {
                    dynamicTank.positions.Add(time, to);
                }
            }
        }

        public void setMap(string id) {
            map = maps.getMap(id);
        }

        public void setQuality(Quality quality) {
            this.quality = quality;
        }

        public bool save(string path) {
            if (File.Exists(path)) {
                try {
                    File.Delete(path);
                } catch (Exception) {
                    return false;
                }
            }

            MemoryStream xmlString = new MemoryStream();
            XmlTextWriter xmlWriter = new XmlTextWriter(xmlString, Encoding.UTF8);
            xmlWriter.WriteStartDocument();
            xmlWriter.WriteComment("Tactic planner save file. DO NOT EDIT THIS FILE MANUALLY!");
            xmlWriter.WriteStartElement("tactic");

            xmlWriter.WriteAttributeString("version", "1.0.0");
            xmlWriter.WriteAttributeString("game", "World of Tanks");

            xmlWriter.WriteStartElement("map");
            xmlWriter.WriteAttributeString("id", map.id);
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("staticMaps");
            xmlWriter.WriteStartElement("settings");
            xmlWriter.WriteStartElement("timer");
            xmlWriter.WriteValue(Convert.ToString(!noStaticTimer));
            xmlWriter.WriteEndElement();
            xmlWriter.WriteEndElement();
            xmlWriter.WriteStartElement("maps");
            foreach (staticMapEntry item in staticTactics.Values) {
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

            xmlWriter.WriteStartElement("dynamicMaps");
            xmlWriter.WriteStartElement("settings");
            xmlWriter.WriteStartElement("timer");
            xmlWriter.WriteValue(Convert.ToString(!noDynamicTimer));
            xmlWriter.WriteEndElement();
            xmlWriter.WriteEndElement();
            xmlWriter.WriteStartElement("staticIcons");
            foreach (StaticIcon item in dynamicStaticIcons) {
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

            Package zip = ZipPackage.Open(path, FileMode.Create, FileAccess.ReadWrite);
            PackagePart part = zip.CreatePart(new Uri("/tactic.xml", UriKind.Relative), System.Net.Mime.MediaTypeNames.Text.Xml, CompressionOption.Fast);
            UTF8Encoding encoding = new UTF8Encoding();
            part.GetStream().Write(xmlString.ToArray(), 0, xmlString.ToArray().Length);
            ImageConverter converter = new ImageConverter();
            foreach (staticMapEntry item in staticTactics.Values) {
                if (item.isClone)
                    continue;

                part = zip.CreatePart(new Uri("/staticMaps/" + item.time.ToString() + ".png", UriKind.Relative), "image/png", CompressionOption.Fast);
                byte[] imgBuffer = (byte[])converter.ConvertTo(item.map, typeof(byte[]));
                part.GetStream().Write(imgBuffer, 0, imgBuffer.Length);
            }
            zip.Close();

            return true;
        }

        public bool load(string path) {
            if (!File.Exists(path)) {
                return false;
            }

            Package zip = ZipPackage.Open(path, FileMode.Open, FileAccess.Read);
            PackagePart part = zip.GetPart(new Uri("/tactic.xml", UriKind.Relative));
            Stream zipStream = part.GetStream(FileMode.Open, FileAccess.Read);
            MemoryStream xmlString = new MemoryStream();
            byte[] buffer = new byte[10000];
            int read = zipStream.Read(buffer, 0, buffer.Length);
            while (read > 0) {
                xmlString.Write(buffer, 0, buffer.Length);
                read = zipStream.Read(buffer, 0, buffer.Length);
            }

            ImageConverter converter = new ImageConverter();

            xmlString.Position = 0;
            StreamReader sr = new StreamReader(xmlString);
            XmlDocument XD = new XmlDocument();
            XD.LoadXml(sr.ReadToEnd());
            XmlNode XNDocument = XD.DocumentElement;
            XmlNode XN = XNDocument.SelectSingleNode("/tactic");
            if (XN.Attributes["version"].InnerText != "1.0.0") {
                return false;
            }
            if (XN.Attributes["game"].InnerText != "World of Tanks") {
                return false;
            }

            XN = XNDocument.SelectSingleNode("/tactic/map");
            setMap(XN.Attributes["id"].InnerText);

            XN = XNDocument.SelectSingleNode("/tactic/staticMaps/settings/timer");
            noStaticTimer = !Convert.ToBoolean(XN.InnerText);

            XmlNodeList XNL = XNDocument.SelectNodes("/tactic/staticMaps/maps/map");
            for (int i = 0; i < XNL.Count; i++) {
                staticMapEntry staticMap = new staticMapEntry();
                staticMap.time = Convert.ToInt32(XNL.Item(i).SelectSingleNode("time").InnerText);
                staticMap.isClone = Convert.ToBoolean(XNL.Item(i).SelectSingleNode("isClone").InnerText);
                if (staticMap.isClone) {
                    staticMap.cloneOriginalTime = Convert.ToInt32(XNL.Item(i).SelectSingleNode("cloneOriginalTime").InnerText);
                } else {
                    part = zip.GetPart(new Uri("/staticMaps/" + staticMap.time.ToString() + ".png", UriKind.Relative));
                    zipStream = part.GetStream(FileMode.Open, FileAccess.Read);
                    MemoryStream imageStream = new MemoryStream();
                    read = zipStream.Read(buffer, 0, buffer.Length);
                    while (read > 0) {
                        imageStream.Write(buffer, 0, buffer.Length);
                        read = zipStream.Read(buffer, 0, buffer.Length);
                    }
                    staticMap.map = (Bitmap)Bitmap.FromStream(imageStream);
                }
                staticTactics.Add(staticMap.time, staticMap);
            }

            XN = XNDocument.SelectSingleNode("/tactic/dynamicMaps/settings/timer");
            noDynamicTimer = !Convert.ToBoolean(XN.InnerText);

            XNL = XNDocument.SelectNodes("/tactic/dynamicMaps/staticIcons/icon");
            for (int i = 0; i < XNL.Count; i++) {
                StaticIcon staticIcon = icons.getStaticIcon(XNL.Item(i).SelectSingleNode("id").InnerText);
                XN = XNL.Item(i).SelectSingleNode("position");
                staticIcon.position = new Point(
                    Convert.ToInt32(XN.Attributes["X"].InnerText),
                    Convert.ToInt32(XN.Attributes["Y"].InnerText)
                );
                dynamicStaticIcons.Add(staticIcon);
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
            zip.Close();

            return true;
        }
    }
}
