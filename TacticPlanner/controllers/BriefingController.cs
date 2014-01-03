using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Packaging;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using TacticPlanner.models;
using TacticPlanner.models.network;
using TacticPlanner.Common;

namespace TacticPlanner.controllers {
	class BriefingController {
		[Serializable]
		struct PointsPacket {
			public Point[] ps;
			private byte a;
			private byte r;
			private byte g;
			private byte b;
			public int thickness;
			public int time;

			public PointsPacket(Point[] ps, Color color, int thickness, int time) {
				this.ps = ps;
				a = color.A;
				r = color.R;
				g = color.G;
				b = color.B;
				this.thickness = thickness;
				this.time = time;
			}

			public Color getColor() {
				return Color.FromArgb(a, r, g, b);
			}
		}
		[Serializable]
		struct LinesPacket {
			public Point from;
			public Point to;
			private byte a;
			private byte r;
			private byte g;
			private byte b;
			public int thickness;
			private byte dash;
			public int time;

			public LinesPacket(Point from, Point to, Color color, int thickness, DashStyle dash, int time) {
				this.from = from;
				this.to = to;
				a = color.A;
				r = color.R;
				g = color.G;
				b = color.B;
				this.thickness = thickness;
				if (dash == DashStyles.Solid) {
					this.dash = 1;
				} else if (dash == DashStyles.Dot) {
					this.dash = 2;
				} else {		// Dash
					this.dash = 3;
				}
				this.time = time;
			}

			public Color getColor() {
				return Color.FromArgb(a, r, g, b);
			}

			public DashStyle getDashStyle() {
				if (dash == 1) {
					return DashStyles.Solid;
				} else if (dash == 2) {
					return DashStyles.Dot;
				} else {	// dash == 3
					return DashStyles.Dash;
				}
			}
		}
		[Serializable]
		struct StampPackage {
			public Point p;
			private byte[] img;
			public int size;
			public int time;

			public StampPackage(Point p, BitmapImage bitmap, int size, int time) {
				this.p = p;
				this.size = size;
				this.time = time;

				using (MemoryStream ms = new MemoryStream()) {
					PngBitmapEncoder encoder = new PngBitmapEncoder();
					encoder.Frames.Add(BitmapFrame.Create(bitmap));
					encoder.Save(ms);
					img = ms.ToArray();
				}
			}

			public BitmapImage getImage() {
				BitmapImage bitmap = new BitmapImage();
				MemoryStream ms = new MemoryStream(img);
				ms.Seek(0, SeekOrigin.Begin);

				bitmap.BeginInit();
				bitmap.StreamSource = ms;
				bitmap.EndInit();

				return bitmap;
			}
		}

		private static BriefingController _instance = null;

		private Briefing adapter;

		private string nick;

		private BriefingController() {
			adapter = new Briefing();
			adapter.NetError += new NetCore.NetCoreErrorHandler(NetCoreErrorHandler);
			adapter.NetClientEvent += new NetCore.NetClientEventHandler(adapter_NetClientEvent);
			adapter.ReceiveObservers += new NetCore.NetPackageReceiveHandler(adapter_ReceiveObservers);
		}

		public static BriefingController Instance {
			get {
				return Lazy.Init(ref _instance, () => new BriefingController());
			}
		}

		void adapter_NetClientEvent(object sender, NetClientEvent e) {
			mainwindow.Dispatcher.Invoke((Action)(() => {
				if (e.ev == ClientEventType.connected) {
					MemoryStream stream = new MemoryStream();
					Tactics.Instance.Tactic.serialize(stream);
					NetPackage pack = new NetPackage(NetPackageTypes.Tactic, nick, stream.ToArray());
					adapter.sendTo(pack, e.username);

					mainwindow.addClient(e.username);

					switch (mainwindow.getActiveWindow()) {
						case MainWindow.activeWindow.staticPanel:
							adapter.sendTo(new NetPackage(NetPackageTypes.ShowStatic, nick, null), e.username);
							break;
						case MainWindow.activeWindow.dynamicPanel:
							adapter.sendTo(new NetPackage(NetPackageTypes.ShowDynamic, nick, null), e.username);
							break;
						case MainWindow.activeWindow.playPanelStatic:
							adapter.sendTo(new NetPackage(NetPackageTypes.ShowPlayStatic, nick, null), e.username);
							break;
					}
				} else if (e.ev == ClientEventType.disconnected) {
					mainwindow.removeClient(e.username);
				}
			}));
		}

		void adapter_ReceiveObservers(object sender, NetPackageReceived e) {
			mainwindow.Dispatcher.Invoke((Action)(() => {
				if (e.pack.contentType == NetPackageTypes.Tactic) {
					using (MemoryStream stream = new MemoryStream((byte[])e.pack.content)) {
						stream.Position = 0;
						Tactics.Instance.Tactic.unserialize(stream);
					}
					Tactics.Instance.initFromTactics();
				} else if (e.pack.contentType == NetPackageTypes.ClientList) {
					mainwindow.setClients((string[])e.pack.content);
				} else if (e.pack.contentType == NetPackageTypes.Settings) {
					mainwindow.setClientsPing(adapter.canPing());
					mainwindow.setClientsDraw(adapter.canDraw());
				} else if (e.pack.contentType == NetPackageTypes.Ping) {
					byte[] cell = (byte[])e.pack.content;
					mainwindow.pingCell(cell[0], cell[1]);
				} else if (e.pack.contentType == NetPackageTypes.SetTimer) {
					mainwindow.setTime((int)e.pack.content);
				} else if (e.pack.contentType == NetPackageTypes.SetBattletype) {
					object[] data = (object[])e.pack.content;
					Tactics.Instance.setBattleType((BattleType)data[0], (string)data[1]);
				} else if (e.pack.contentType == NetPackageTypes.ShowStatic) {
					mainwindow.showStatic();
				} else if (e.pack.contentType == NetPackageTypes.ShowDynamic) {
					mainwindow.showDynamic();
				} else if (e.pack.contentType == NetPackageTypes.ShowPlayStatic) {
					mainwindow.showPlayStatic();
				} else if (e.pack.contentType == NetPackageTypes.ShowPlayDynamic) {
					mainwindow.showPlayDynamic();
				} else if (e.pack.contentType == NetPackageTypes.DrawPoints) {
					PointsPacket pack = (PointsPacket)e.pack.content;
					Tactics.Instance.drawPoints(pack.ps, pack.getColor(), pack.thickness, pack.time);
				} else if (e.pack.contentType == NetPackageTypes.DrawEraserPoints) {
					PointsPacket pack = (PointsPacket)e.pack.content;
					Tactics.Instance.drawEraserPoints(pack.ps, pack.thickness, pack.time);
				} else if (e.pack.contentType == NetPackageTypes.DrawLine) {
					LinesPacket pack = (LinesPacket)e.pack.content;
					Tactics.Instance.drawLine(pack.from, pack.to, pack.getColor(), pack.thickness, pack.getDashStyle(), pack.time);
				} else if (e.pack.contentType == NetPackageTypes.DrawArrow) {
					LinesPacket pack = (LinesPacket)e.pack.content;
					Tactics.Instance.drawArrow(pack.from, pack.to, pack.getColor(), pack.thickness, pack.getDashStyle(), pack.time);
				} else if (e.pack.contentType == NetPackageTypes.DrawStamp) {
					StampPackage pack = (StampPackage)e.pack.content;
					Tactics.Instance.drawStamp(pack.p, pack.getImage(), pack.size, pack.time);
				} else if (e.pack.contentType == NetPackageTypes.ResetDrawAt) {
					Tactics.Instance.removeDraw((int)e.pack.content);
				} else if (e.pack.contentType == NetPackageTypes.CloneDrawAt) {
					Tactics.Instance.cloneTactic((int)e.pack.content + 30, (int)e.pack.content);
				} else if (e.pack.contentType == NetPackageTypes.ReloadDynamic) {
					Stream stream = unzip((byte[])e.pack.content);
					Tactics.Instance.unserializeDynamicTactic(stream);
				} else if (e.pack.contentType == NetPackageTypes.StaticTimer) {
					Tactics.Instance.setStaticTimer((bool)e.pack.content);
				} else if (e.pack.contentType == NetPackageTypes.DynamicTimer) {
					Tactics.Instance.setDynamicTimer((bool)e.pack.content);
				}
			}));
		}

		public void NetCoreErrorHandler(object sender, NetCoreError e) {
			dropError(e.error);
			if (!adapter.hasConnection()) {
				setDisconnected();
			}
		}

		public void open(string nick, int port = 27788) {
			this.nick = nick;
			adapter.openServer(nick, port);
		}

		public void connect(string nick, string host, int port, string password = "") {
			this.nick = nick;
			adapter.connect(host, port, nick, password);
		}

		public void disconnect() {
			adapter.disconnect();
		}

		public string getMyIp() {
			return adapter.getMyIp();
		}

		public bool hasConnection() {
			return adapter.hasConnection();
		}

		public bool isClient() {
			return adapter.isClient();
		}

		public bool isServer() {
			return adapter.isServer();
		}

		public void setPassword(string password) {
			adapter.setPassword(password);
		}

		public void enableClientsPing() {
			adapter.enableClientsPing();
		}
		public void disableClientsPing() {
			adapter.disableClientsPing();
		}

		public void enableClientsDraw() {
			adapter.enableClientsDraw();
		}
		public void disableClientsDraw() {
			adapter.disableClientsDraw();
		}

		public void sendTactic() {
			if (adapter.isServer() || adapter.isClient() && adapter.canDraw()) {
				using (MemoryStream stream = new MemoryStream()) {
					Tactics.Instance.Tactic.serialize(stream);
					NetPackage pack = new NetPackage(NetPackageTypes.Tactic, nick, stream.ToArray());
					adapter.send(pack);
				}
			}
		}

		public void ping(byte col, byte row) {
			if (adapter.isServer() || adapter.isClient() && adapter.canPing()) {
				byte[] data = new byte[2];
				data[0] = col;
				data[1] = row;
				NetPackage pack = new NetPackage(NetPackageTypes.Ping, nick, data);
				adapter.send(pack);
			}
		}

		public void setTimer(int time) {
			if (adapter.isServer() || adapter.isClient() && adapter.canPing()) {
				NetPackage pack = new NetPackage(NetPackageTypes.SetTimer, nick, time);
				adapter.send(pack);
			}
		}

		public void setBattleType(BattleType type, string variant) {
			object[] data = new object[2];
			data[0] = type;
			data[1] = variant;
			NetPackage pack = new NetPackage(NetPackageTypes.SetBattletype, nick, data);
			adapter.send(pack);
		}

		public void showStatic() {
			if (adapter.isServer() || adapter.isClient() && adapter.canPing()) {
				adapter.send(new NetPackage(NetPackageTypes.ShowStatic, nick, null));
			}
		}

		public void showDynamic() {
			if (adapter.isServer() || adapter.isClient() && adapter.canPing()) {
				adapter.send(new NetPackage(NetPackageTypes.ShowDynamic, nick, null));
			}
		}

		public void showPlayStatic() {
			if (adapter.isServer() || adapter.isClient() && adapter.canPing()) {
				adapter.send(new NetPackage(NetPackageTypes.ShowPlayStatic, nick, null));
			}
		}

		public void showPlayDynamic() {
			if (adapter.isServer() || adapter.isClient() && adapter.canPing()) {
				adapter.send(new NetPackage(NetPackageTypes.ShowPlayDynamic, nick, null));
			}
		}

		public void drawPoints(Point[] ps, Color color, int thickness, int time) {
			if (adapter.isServer() || adapter.isClient() && adapter.canDraw()) {
				PointsPacket data = new PointsPacket(ps, color, thickness, time);
				NetPackage pack = new NetPackage(NetPackageTypes.DrawPoints, nick, data);
				adapter.send(pack);
			}
		}

		public void drawEraserPoints(Point[] ps, int thickness, int time) {
			if (adapter.isServer() || adapter.isClient() && adapter.canDraw()) {
				PointsPacket data = new PointsPacket(ps, Colors.Red, thickness, time);
				NetPackage pack = new NetPackage(NetPackageTypes.DrawEraserPoints, nick, data);
				adapter.send(pack);
			}
		}

		public void drawLine(Point from, Point to, Color color, int thickness, DashStyle dash, int time) {
			if (adapter.isServer() || adapter.isClient() && adapter.canDraw()) {
				LinesPacket data = new LinesPacket(from, to, color, thickness, dash, time);
				NetPackage pack = new NetPackage(NetPackageTypes.DrawLine, nick, data);
				adapter.send(pack);
			}
		}

		public void drawArrow(Point from, Point to, Color color, int thickness, DashStyle dash, int time) {
			if (adapter.isServer() || adapter.isClient() && adapter.canDraw()) {
				LinesPacket data = new LinesPacket(from, to, color, thickness, dash, time);
				NetPackage pack = new NetPackage(NetPackageTypes.DrawArrow, nick, data);
				adapter.send(pack);
			}
		}

		public void drawStamp(Point p, BitmapImage bitmap, int size, int time) {
			if (adapter.isServer() || adapter.isClient() && adapter.canDraw()) {
				StampPackage data = new StampPackage(p, bitmap, size, time);
				NetPackage pack = new NetPackage(NetPackageTypes.DrawStamp, nick, data);
				adapter.send(pack);
			}
		}

		public void resetDrawAt(int time) {
			if (adapter.isServer() || adapter.isClient() && adapter.canDraw()) {
				adapter.send(new NetPackage(NetPackageTypes.ResetDrawAt, nick, time));
			}
		}

		public void cloneDrawAt(int time) {
			if (adapter.isServer() || adapter.isClient() && adapter.canDraw()) {
				adapter.send(new NetPackage(NetPackageTypes.CloneDrawAt, nick, time));
			}
		}

		public void reloadDynamic() {
			if (adapter.isServer() || adapter.isClient() && adapter.canDraw()) {
				MemoryStream stream = new MemoryStream();
				Tactics.Instance.serializeDynamicTactic(stream);
				NetPackage pack = new NetPackage(NetPackageTypes.ReloadDynamic, nick, zip(stream));
				adapter.send(pack);
			}
		}

		public void staticTimer(bool val) {
			if (adapter.isServer() || adapter.isClient() && adapter.canDraw()) {
				adapter.send(new NetPackage(NetPackageTypes.StaticTimer, nick, val));
			}
		}

		public void dynamicTimer(bool val) {
			if (adapter.isServer() || adapter.isClient() && adapter.canDraw()) {
				adapter.send(new NetPackage(NetPackageTypes.DynamicTimer, nick, val));
			}
		}

		private byte[] zip(Stream stream) {
			MemoryStream ms = new MemoryStream();
			Package zip = ZipPackage.Open(ms, FileMode.Create, FileAccess.ReadWrite);
			PackagePart part = zip.CreatePart(new Uri("/data.xml", UriKind.Relative), System.Net.Mime.MediaTypeNames.Text.Xml, CompressionOption.Maximum);
			stream.Position = 0;
			stream.CopyTo(part.GetStream());
			zip.Close();
			return ms.ToArray();
		}

		private Stream unzip(byte[] data) {
			MemoryStream ms = new MemoryStream(data);
			Package zip = ZipPackage.Open(ms, FileMode.Open, FileAccess.Read);
			PackagePart part = zip.GetPart(new Uri("/data.xml", UriKind.Relative));
			return part.GetStream(FileMode.Open, FileAccess.Read);
		}

		public void kick(string nick) {
			adapter.kick(nick);
		}

		public void dropError(string message) {
			mainwindow.dropError(message);
		}

		public void setDisconnected() {
			mainwindow.disconnected();
		}
	}
}
