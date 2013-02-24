using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TacticPlanner.models.network {
	class Briefing : NetCore {
		[Serializable]
		struct BriefingSettings {
			public bool clientsPing;
			public bool clientsDraw;

			public BriefingSettings(bool ping = true, bool draw = false) {
				clientsPing = ping;
				clientsDraw = draw;
			}
		}

		private bool clientsPing = true, clientsDraw = false;

		public Briefing() : base() {
			this.NetClientEvent += new NetClientEventHandler(Briefing_NetClientEvent);
			this.ReceiveObservers += new NetPackageReceiveHandler(Briefing_ReceiveObservers);
		}

		void Briefing_NetClientEvent(object sender, NetClientEvent e) {
			if (e.ev == ClientEventType.connected) {
				string[] clients = getClientNames(true);
				NetPackage pack = new NetPackage(NetPackageTypes.ClientList, username, clients);
				send(pack);

				sendSettings(e.username);
			} else if (e.ev == ClientEventType.disconnected) {
				string[] clients = getClientNames(true);
				NetPackage pack = new NetPackage(NetPackageTypes.ClientList, username, clients);
				send(pack);
			}
		}

		void Briefing_ReceiveObservers(object sender, NetPackageReceived e) {
			if (e.pack.contentType == NetPackageTypes.Settings) {
				BriefingSettings sets = (BriefingSettings)e.pack.content;
				clientsPing = sets.clientsPing;
				clientsDraw = sets.clientsDraw;
			} else if (isServer() && (
				e.pack.contentType == NetPackageTypes.Tactic ||
				e.pack.contentType == NetPackageTypes.Ping ||
				e.pack.contentType == NetPackageTypes.DrawPoints ||
				e.pack.contentType == NetPackageTypes.DrawEraserPoints ||
				e.pack.contentType == NetPackageTypes.DrawLine ||
				e.pack.contentType == NetPackageTypes.DrawArrow ||
				e.pack.contentType == NetPackageTypes.DrawStamp ||
				e.pack.contentType == NetPackageTypes.SetTimer ||
				e.pack.contentType == NetPackageTypes.ShowStatic ||
				e.pack.contentType == NetPackageTypes.ShowDynamic ||
				e.pack.contentType == NetPackageTypes.ShowPlayStatic ||
                e.pack.contentType == NetPackageTypes.ShowPlayDynamic ||
				e.pack.contentType == NetPackageTypes.ResetDrawAt ||
				e.pack.contentType == NetPackageTypes.CloneDrawAt ||
				e.pack.contentType == NetPackageTypes.ReloadDynamic ||
				e.pack.contentType == NetPackageTypes.StaticTimer ||
				e.pack.contentType == NetPackageTypes.DynamicTimer
				)) {
					sendExclude(e.pack, e.pack.sender);
			}
		}

		public void openServer(string nick, int port = 27788) {
			base.openServer(nick, port);
		}

		public void connect(string host, int port, string nick, string password = "") {
			base.connect(host, port, nick, password);
		}

		public override void disconnect() {
			base.disconnect();
		}

		protected override bool preProcessData(object package, System.Net.Sockets.Socket client) {
			return base.preProcessData(package, client);
		}

		public void enableClientsPing() {
			clientsPing = true;
			sendSettings();
		}
		public void disableClientsPing() {
			clientsPing = false;
			sendSettings();
		}

		public void enableClientsDraw() {
			clientsDraw = true;
			sendSettings();
		}
		public void disableClientsDraw() {
			clientsDraw = false;
			sendSettings();
		}

		public bool canPing() {
			return clientsPing;
		}

		public bool canDraw() {
			return clientsDraw;
		}

		private void sendSettings(string to = null) {
			BriefingSettings sets = new BriefingSettings(clientsPing, clientsDraw);
			NetPackage pack = new NetPackage(NetPackageTypes.Settings, username, sets);
			if (to != null) {
				sendTo(pack, to);
			} else {
				send(pack);
			}
		}

		public void send(NetPackage data) {
			base.send(data);
		}

		public void sendTo(NetPackage data, string nick) {
			base.sendTo(data, nick);
		}

		public void sendExclude(NetPackage data, string nick) {
			base.sendExclude(data, nick);
		}

	}
}
