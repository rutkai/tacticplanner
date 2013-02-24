using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace TacticPlanner.models.network {
	abstract class NetCore {
		[Serializable]
		private class PasswordPackage {
			public bool? accepted;
			public string message;
			public string username;
			public string password;

			public PasswordPackage(string uname, string pass = "") {
				accepted = null;
				message = "";
				username = uname;
				password = pass;
			}
		}
		private enum InternalCommand {
			Kick,
			ServerDisconnecting
		}

		public delegate void NetPackageReceiveHandler(object sender, NetPackageReceived e);
		public delegate void NetCoreErrorHandler(object sender, NetCoreError e);
		public delegate void NetClientEventHandler(object sender, NetClientEvent e);
		public event NetPackageReceiveHandler ReceiveObservers;
		public event NetCoreErrorHandler NetError;
		public event NetClientEventHandler NetClientEvent;

		private SocketPermission permission;
		private Socket listener, client;
		private List<Socket> clients;
		private Dictionary<string, Socket> clientnames;
		private Thread clientDisposerThread;

		protected string username;
		private string password;
		private string host;
		private int port;

		private bool cancelReconnect = false;

		public NetCore() {
			password = "";
		}

		public bool hasConnection() {
			return client != null || listener != null;
		}

		public bool isServer() {
			return listener != null;
		}

		public bool isClient() {
			return client != null;
		}

		public void setPassword(string pass) {
			password = pass;
		}

		public string getMyIp() {
			IPHostEntry host;
			string localIP = "?";
			host = Dns.GetHostEntry(Dns.GetHostName());
			foreach (IPAddress ip in host.AddressList) {
				if (ip.AddressFamily == AddressFamily.InterNetwork) {
					localIP = ip.ToString();
				}
			}
			return localIP;
		}

		public string[] getClientNames(bool addMyself = false) {
			List<string> result = clientnames.Keys.ToList();
			if (addMyself) {
				result.Add(username);
			}
			result.Sort();
			return result.ToArray();
		}

		protected virtual void openServer(string uname = "", int port = 27788) {
			if (hasConnection()) {
				disconnect();
			}

			username = uname;

			try {
				permission = new SocketPermission(
					NetworkAccess.Accept,     // Allowed to accept connections 
					TransportType.Tcp,        // Defines transport types 
					"",                       // The IP addresses of local host 
					port                      // Specifies all ports 
				);
				permission.Demand();

				IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, port);

				listener = new Socket(
					AddressFamily.InterNetwork,
					SocketType.Stream,
					ProtocolType.Tcp
				);
				listener.Bind(endPoint);
                listener.NoDelay = false;
				listener.Listen(10000);

				clients = new List<Socket>();
				clientnames = new Dictionary<string, Socket>();
				clientDisposerThread = new Thread(new ThreadStart(clientDisposer));
				clientDisposerThread.Start();

				listener.BeginAccept(new AsyncCallback(acceptConnection), listener);
			} catch (Exception ex) {
				listener = null;
				dispatchErrorEvent(new NetCoreError("Listening error: " + ex.Message));
			}
		}

		private void acceptConnection(IAsyncResult ar) {
			Socket handler = null, listener = null;

			try {
				listener = (Socket)ar.AsyncState;
				handler = listener.EndAccept(ar);
				clients.Add(handler);

				byte[] headerbuffer = new byte[4];
				object[] obj = new object[2];
				obj[0] = headerbuffer;
				obj[1] = handler;
				handler.BeginReceive(headerbuffer, 0, headerbuffer.Length, SocketFlags.None, new AsyncCallback(receiveHeader), obj);
				listener.BeginAccept(new AsyncCallback(acceptConnection), listener);
			} catch (Exception) {
				// client connection error
			}
		}

		protected virtual void connect(string host, int port, string uname, string password = "", bool isReconnect = false) {
			if (hasConnection()) {
				disconnect();
			}

			username = uname;
			this.password = password;
			this.host = host;
			this.port = port;
			cancelReconnect = false;

			try {
				permission = new SocketPermission(
					NetworkAccess.Accept,     // Allowed to accept connections 
					TransportType.Tcp,        // Defines transport types 
					"",                       // The IP addresses of local host 
					port                      // Specifies all ports 
				);
				permission.Demand();

				IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(host), port);

				client = new Socket(
					AddressFamily.InterNetwork,
					SocketType.Stream,
					ProtocolType.Tcp
				);
                client.NoDelay = false;
				client.Connect(endPoint);

				byte[] headerbuffer = new byte[4];
				object[] obj = new object[2];
				obj[0] = headerbuffer;
				obj[1] = client;
				client.BeginReceive(headerbuffer, 0, headerbuffer.Length, SocketFlags.None, new AsyncCallback(receiveHeader), obj);

				send(new PasswordPackage(username, password));
			} catch (Exception ex) {
				client = null;
				if (isReconnect) {
					throw new Exception("Reconnect error!", ex);
				} else {
					dispatchErrorEvent(new NetCoreError("Connection error: " + ex.Message));
				}
			}
		}

		private void reconnect(string onFailMessage = "") {
			if (isServer()) {
				return;
			}

			if (hasConnection()) {
				disconnect();
			}

			if (cancelReconnect) {
				cancelReconnect = false;
				return;
			}
			Thread.Sleep(1000);	// reconnect waiting
			if (cancelReconnect) {
				cancelReconnect = false;
				return;
			}

			try {
				connect(host, port, username, password, true);
			} catch (Exception) {
				dispatchErrorEvent(new NetCoreError("Connection error: " + onFailMessage + "\nReconnecting failed!"));
			}
		}

		private void receiveHeader(IAsyncResult ar) {
			object[] obj = new object[2];
			obj = (object[])ar.AsyncState;

			byte[] headerbuffer = (byte[])obj[0];
			Socket handler = (Socket)obj[1];

			try {
				handler.EndReceive(ar);
				int packetsize = BitConverter.ToInt32(headerbuffer, 0);
                if (packetsize == 0) {
                    headerbuffer = new byte[4];
                    obj = new object[2];
                    obj[0] = headerbuffer;
                    obj[1] = handler;
                    handler.BeginReceive(headerbuffer, 0, headerbuffer.Length, SocketFlags.None, new AsyncCallback(receiveHeader), obj);
                } else {
                    byte[] buffer = new byte[packetsize < 8192 ? packetsize : 8192];
                    obj = new object[4];
                    obj[0] = buffer;
                    obj[1] = handler;
                    obj[2] = packetsize;
                    obj[3] = new byte[packetsize];
                    handler.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(receiveData), obj);
                }
			} catch (Exception) {
				if (hasConnection()) {
					if (isClient()) {
						reconnect("Header receive failed! Disconnecting...");
					} else {
						disconnect(handler);
					}
				}
			}
		}

		private void receiveData(IAsyncResult ar) {
			object[] obj = new object[2];
			obj = (object[])ar.AsyncState;

			byte[] buffer = (byte[])obj[0];
			Socket handler = (Socket)obj[1];
            int remaining = (int)obj[2];
            byte[] data = (byte[])obj[3];

			try {
				int received = handler.EndReceive(ar);

                Array.ConstrainedCopy(buffer, 0, data, data.Length - remaining, received);
                remaining -= received;

                if (remaining != 0) {
                    buffer = new byte[remaining < 8192 ? remaining : 8192];
                    obj = new object[4];
                    obj[0] = buffer;
                    obj[1] = handler;
                    obj[2] = remaining;
                    obj[3] = data;
                    handler.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(receiveData), obj);
                } else {
                    MemoryStream stream = new MemoryStream(data);
                    stream.Position = 0;
                    BinaryFormatter formatter = new BinaryFormatter();
                    object package = formatter.Deserialize(stream);

                    byte[] headerbuffer = new byte[4];
                    obj = new object[2];
                    obj[0] = headerbuffer;
                    obj[1] = handler;
                    handler.BeginReceive(headerbuffer, 0, headerbuffer.Length, SocketFlags.None, new AsyncCallback(receiveHeader), obj);

                    if (preProcessData(package, handler)) {
                        dispatchPackageEvent(new NetPackageReceived((NetPackage)package));
                    }
                }
			} catch (Exception) {
				if (hasConnection()) {
					if (isClient()) {
						reconnect("Receive failed! Disconnecting...");
					} else {
						disconnect(handler);
					}
				}
			}
		}

		protected virtual bool preProcessData(object package, Socket client) {
			if (package is PasswordPackage) {
				PasswordPackage ppack = (PasswordPackage)package;
				if (isServer()) {
					if (ppack.password != password) {
						ppack.accepted = false;
						ppack.message = "Wrong password!";
						sendTo((object)ppack, client);
						disconnect(client);
					} else if (clientnames.ContainsKey(ppack.username) || username == ppack.username) {
						ppack.accepted = false;
						ppack.message = "Your username has been taken already!";
						sendTo((object)ppack, client);
						disconnect(client);
					} else {
						ppack.accepted = true;
						clientnames.Add(ppack.username, client);
						dispatchClientEvent(new network.NetClientEvent(ClientEventType.connected, ppack.username));
						sendTo((object)ppack, client);
					}
				} else {
					if (!(bool)ppack.accepted) {
						cancelReconnect = true;
						disconnect();
						dispatchErrorEvent(new NetCoreError(ppack.message));
					}
				}
				return false;
			} else if (package is InternalCommand) {
				switch ((InternalCommand)package) {
					case InternalCommand.Kick:
						cancelReconnect = true;
						disconnect();
						dispatchErrorEvent(new NetCoreError("You have been kicked!"));
						break;
					case InternalCommand.ServerDisconnecting:
						cancelReconnect = true;
						disconnect();
						dispatchErrorEvent(new NetCoreError("Server closed connection!"));
						break;
				}
				return false;
			}

			return true;
		}

		protected virtual void send(object package) {
			if (!hasConnection()) {
				return;
			}

			byte[] data = prepareData(package);

			if (isClient()) {
				try {
					client.BeginSend(data, 0, data.Length, SocketFlags.None, new AsyncCallback(sendCallback), client);
				} catch (Exception) {
					if (hasConnection()) {
						reconnect("Send failed! Disconnecting...");
					}
				}
			} else {
				foreach (Socket sock in clients.ToArray()) {
					try {
						sock.BeginSend(data, 0, data.Length, SocketFlags.None, new AsyncCallback(sendCallback), sock);
					} catch (Exception) {
						disconnect(sock);
					}
				}
			}
		}

		protected virtual void sendTo(object package, string user) {
			if (!hasConnection() || isClient() || !clientnames.ContainsKey(user)) {
				return;
			}

			byte[] data = prepareData(package);
			Socket sock = clientnames[user];

			try {
				sock.BeginSend(data, 0, data.Length, SocketFlags.None, new AsyncCallback(sendCallback), sock);
			} catch (Exception) {
				disconnect(sock);
			}
		}

		private void sendTo(object package, Socket sock) {
			if (!hasConnection() || isClient()) {
				return;
			}

			byte[] data = prepareData(package);

			try {
				sock.BeginSend(data, 0, data.Length, SocketFlags.None, new AsyncCallback(sendCallback), sock);
			} catch (Exception) {
				disconnect(sock);
			}
		}

		protected virtual void sendExclude(object package, string user) {
			if (!hasConnection() || isClient() || !clientnames.ContainsKey(user)) {
				return;
			}

			byte[] data = prepareData(package);
			Socket excl = clientnames[user];

			foreach (Socket sock in clients.ToArray()) {
				try {
					if (sock != excl) {
						sock.BeginSend(data, 0, data.Length, SocketFlags.None, new AsyncCallback(sendCallback), sock);
					}
				} catch (Exception) {
					disconnect(sock);
				}
			}
		}

		private byte[] prepareData(object obj) {
			BinaryFormatter formatter = new BinaryFormatter();
			MemoryStream stream = new MemoryStream();

			formatter.Serialize(stream, obj);
			byte[] data = stream.ToArray();
			byte[] header = BitConverter.GetBytes(data.Length);

			byte[] tosend = new byte[data.Length + header.Length];
			header.CopyTo(tosend, 0);
			data.CopyTo(tosend, header.Length);

			return tosend;
		}

		private void sendCallback(IAsyncResult ar) {
			Socket handler = (Socket)ar.AsyncState;
			try {
				handler.EndSend(ar);
			} catch (Exception) {
				if (hasConnection()) {
					if (isClient()) {
						reconnect("Send failed! Disconnecting...");
					} else {
						disconnect(handler);
					}
				}
			}
		}

		public virtual void disconnect() {
			if (!hasConnection()) {
				return;
			}

			if (isServer()) {
				try {
					clientDisposerThread.Abort();
				} catch (Exception) { }
				//listener.Shutdown(SocketShutdown.Both);
				listener.Close();
				foreach (Socket client in clients) {
					if (client.Connected) {
						sendTo(InternalCommand.ServerDisconnecting, client);
						try {
							client.Shutdown(SocketShutdown.Both);
						} catch (Exception) { }
						client.Close();
					}
				}
				listener = null;
			} else if (isClient()) {
				if (client.Connected) {
					try {
						client.Shutdown(SocketShutdown.Both);
					} catch (Exception) { }
					client.Close();
				}
				client = null;
			}
		}

		protected virtual void disconnect(Socket client) {
			if (!hasConnection()) {
				return;
			}

			if (isClient()) {
				disconnect();
			} else {
				try {
					client.Shutdown(SocketShutdown.Both);
				} catch (Exception) { }
				client.Close();
			}
		}

		private void clientDisposer() {
			while ((true)) {
				Thread.Sleep(1000);
				for (int i = 0; i < clients.Count; i++) {
					if (!clients[i].Connected) {
						removeClient(clients[i]);
					}
				}
			}
		}

		private void removeClient(Socket client) {
			string user = null;
			foreach (string item in clientnames.Keys.ToArray()) {
				if (clientnames[item] == client) {
					user = item;
				}
			}
			if (user != null) {
				clientnames.Remove(user);
				dispatchClientEvent(new NetClientEvent(ClientEventType.disconnected, user));
			}
			clients.Remove(client);
		}

		public virtual bool kick(string username) {
			if (clientnames.ContainsKey(username)) {
				sendTo(InternalCommand.Kick, username);
				disconnect(clientnames[username]);
				return true;
			}
			return false;
		}

		private void dispatchPackageEvent(NetPackageReceived e) {
			if (ReceiveObservers != null) {
				ReceiveObservers(this, e);
			}
		}

		private void dispatchErrorEvent(NetCoreError e) {
			if (NetError != null) {
				NetError(this, e);
			}
		}

		private void dispatchClientEvent(NetClientEvent e) {
			if (NetClientEvent != null) {
				NetClientEvent(this, e);
			}
		}
	}

	class NetPackageReceived : EventArgs {
		public NetPackage pack;

		public NetPackageReceived(NetPackage pack) {
			this.pack = pack;
		}
	}

	class NetCoreError : EventArgs {
		public string error;
		public int code;

		public NetCoreError(string error, int code = -1) {
			this.error = error;
			this.code = code;
		}
	}

	enum ClientEventType {
		connected, disconnected
	}
	class NetClientEvent : EventArgs {
		public ClientEventType ev;
		public string username;

		public NetClientEvent(ClientEventType ev, string username) {
			this.ev = ev;
			this.username = username;
		}
	}
}
