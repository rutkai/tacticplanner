using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Runtime.Serialization.Formatters.Binary;

namespace TacticPlanner.models {
	abstract class NetCore {
		protected TcpClient client;
		protected NetworkStream clientStream;

		protected List<Thread> clientThreads;
		protected List<TcpClient> clients;
		protected List<NetworkStream> clientStreams;
		protected TcpListener listener;
		protected Thread listenerThread;

		private string password;

		private Queue<object> toSend;
		private Queue<object> toReceive;

		public NetCore() {
			
		}

		public bool hasConnection() {
			return client != null || listener != null;
		}

		public bool isServer() {
			return listener != null;
		}

		public bool isClient() {
			return listener == null && client != null;
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

		public virtual void openServer(int port = 27788) {
			if (hasConnection()) {
				disconnect();
			}

			toSend = new Queue<object>();
			toReceive = new Queue<object>();

			try {
				listener = new TcpListener(IPAddress.Any, port);
				clientThreads = new List<Thread>();
				clients = new List<TcpClient>();
				clientStreams = new List<NetworkStream>();
				listenerThread = new Thread(new ThreadStart(listen));
				listenerThread.Start();
			} catch (Exception ex) {
				listener = null;
				throw ex;
			}
		}

		private void listen() {
			listener.Start();

			while ((true)) {
				TcpClient client = listener.AcceptTcpClient();
				clients.Add(client);
				Thread clientThread = new Thread(clientHandler_server);
				clientThread.Start(client);
			}
		}

		private void clientHandler_server(object client) {
			TcpClient tcpClient = (TcpClient)client;

			NetworkStream networkStream = tcpClient.GetStream();

			BinaryFormatter formatter = new BinaryFormatter();
			string pass = (string)formatter.Deserialize(networkStream);
			if (pass != password) {
				formatter.Serialize(networkStream, false);
				clientStreams.Remove(networkStream);
				int index = clients.IndexOf(tcpClient);
				clients.Remove(tcpClient);
				return;
			} else {
				formatter.Serialize(networkStream, true);
			}

			clientInitializer(tcpClient, networkStream);

			try {
				transfer(networkStream);
			} catch (Exception) {
				clientStreams.Remove(networkStream);
				int index = clients.IndexOf(tcpClient);
				clients.Remove(tcpClient);
			}
		}

		protected abstract void clientInitializer(TcpClient client, NetworkStream stream);

		public virtual void connect(string host, int port, string password = "") {
			if (hasConnection()) {
				disconnect();
			}

			toSend = new Queue<object>();
			toReceive = new Queue<object>();

			try {
				client = new TcpClient(host, port);
				if (client.Connected) {
					Thread clientThread = new Thread(clientHandler_server);
					clientThread.Start(client);
				}
			} catch (Exception ex) {
				client = null;
				throw ex;
			}
		}

		private void clientHandler_client(object client) {
			TcpClient tcpClient = (TcpClient)client;

			clientStream = tcpClient.GetStream();

			BinaryFormatter formatter = new BinaryFormatter();
			formatter.Serialize(clientStream, password);
			bool allow = (bool)formatter.Deserialize(clientStream);
			if (!allow) {
				disconnect();
				return;
			}

			try {
				transfer(clientStream);
			} catch (Exception) {
				disconnect();
			}
		}

		private void transfer(NetworkStream stream) {
			BinaryFormatter formatter = new BinaryFormatter();
			while ((true)) {
				while (toSend.Count != 0) {
					object data = toSend.Dequeue();
					if (isServer()) {
						for (int i = 0; i < clientStreams.Count; i++) {
							formatter.Serialize(clientStreams[i], data);
						}
					} else {
						formatter.Serialize(stream, data);
					}
				}
				while (stream.DataAvailable) {
					object data = formatter.Deserialize(stream);
					toReceive.Enqueue(data);
				}
				Thread.Sleep(30);
			}
		}

		public virtual void disconnect() {
			if (!hasConnection()) {
				return;
			}

			if (isServer()) {
				for (int i = 0; i < clientStreams.Count; i++) {
					clientStreams[i].Close();
				}
				for (int i = 0; i < clients.Count; i++) {
					clients[i].Close();
				}
				for (int i = 0; i < clientThreads.Count; i++) {
					clientThreads[i].Abort();
				}
				listenerThread.Abort();
				listener.Stop();
				listener = null;
			} else if (isClient()) {
				client.Close();
				client = null;
			}
		}

		public virtual void send(object data) {
			toSend.Enqueue(data);
		}

		public virtual object receive() {
			if (toReceive.Count != 0) {
				return toReceive.Dequeue();
			} else {
				return null;
			}
		}
	}
}
