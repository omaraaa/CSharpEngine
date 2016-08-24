using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using Lidgren.Network;
using CS;
using CS.Components;
using System;
using System.IO;
using Entities;

namespace CS.Components
{
	enum NetworkMessages : int
	{
		CONNECTED, INITIALIZE, SYNC, EXIT
	}

	class NetworkServerSystem : BaseSystem, ISysUpdateable
	{
		public int TickRate = 20;

		NetServer Server;
		NetPeerConfiguration Config;

		public NetworkServerSystem(State state) : base(state, "NetworkServerSystem")
		{
		}

		public override BaseSystem DeserializeConstructor(State state, string name)
		{
			return new NetworkServerSystem(state);
		}

		public void Start(string appname, int port = 0)
		{
			Config = new NetPeerConfiguration(appname)
			{
				Port = port
			};
			Server = new NetServer(Config);
			Server.Start();
		}

		public void Update(Global G)
		{
		}

		private void ManageData(NetIncomingMessage message)
		{

		}

		public override void SerializeSystem(BinaryWriter writer)
		{
		}

		public override void DeserializeSystem(BinaryReader reader)
		{
		}
	}

	class NetworkClientSystem : BaseSystem, ISysUpdateable
	{
		public int TickRate = 20;

		NetClient Client;
		NetPeerConfiguration Config;

		public NetworkClientSystem(State state) : base(state, "NetworkClientSystem")
		{
		}

		public override BaseSystem DeserializeConstructor(State state, string name)
		{
			return new NetworkServerSystem(state);
		}

		public void Connect(string appname, string host, int port)
		{
			Config = new NetPeerConfiguration(appname)
			{
				Port = port
			};
			Client = new NetClient(Config);
			Client.Connect(host, port);
		}

		public void Update(Global G)
		{
			
		}

		private void ManageData(NetIncomingMessage message)
		{

		}

		public override void SerializeSystem(BinaryWriter writer)
		{
		}

		public override void DeserializeSystem(BinaryReader reader)
		{
		}
	}

	class StateSyncSystem : BaseSystem, ISysUpdateable
	{
		public NetBuffer Buffer;

		public StateSyncSystem(State state) : base(state, "StateSyncSystem")
		{
		}

		public override BaseSystem DeserializeConstructor(State state, string name)
		{
			return new StateSyncSystem(state);
		}

		public override void DeserializeSystem(BinaryReader reader)
		{
		}

		public override void SerializeSystem(BinaryWriter writer)
		{
		}

		public void Update(Global G)
		{
			throw new NotImplementedException();
		}
	}
}