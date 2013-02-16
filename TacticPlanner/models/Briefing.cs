using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using TacticPlanner.models.network;

namespace TacticPlanner.models {
	class Briefing : NetCore {
		private bool clientsPing = true, cliensDraw = false;
		private string nick;

		private Tactic tactic;

		public Briefing(Tactic tactic = null) : base() {
			this.tactic = tactic;
		}

		public void openServer(string nick, int port = 27788) {
			if (tactic == null) {
				throw new Exception("No tactic!");
			}

			base.openServer(port);

			this.nick = nick;
		}

		public void connect(string nick, string host, int port, string password = "") {
			if (tactic == null) {
				throw new Exception("No tactic!");
			}

			base.connect(host, port, password);

			this.nick = nick;
		}

		protected override void clientInitializer(System.Net.Sockets.TcpClient client, System.Net.Sockets.NetworkStream stream) {
			
		}

		public void enableClientsPing() {

		}
		public void disableClientsPing() {

		}

		public void enableClientsDraw() {

		}
		public void disableClientsDraw() {

		}

		public void setTactic(Tactic tactic) {
			this.tactic = tactic;
		}

		public void send(NetPackage data) {
			base.send(data);
		}

		public NetPackage receive() {
			NetPackage package = (NetPackage)base.receive();
			
			return package;
		}

	}
}
