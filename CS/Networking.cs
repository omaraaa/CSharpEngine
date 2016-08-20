using Lidgren.Network;
using CS;
using CS.Components;
using System;

namespace CS.Components
{
	class NetworkSystem : BaseSystem, ISysUpdateable
	{
		public NetworkSystem(State state, string name) : base(state, name)
		{
		}

		public override BaseSystem DeserializeConstructor(State state, string name)
		{
			return new NetworkSystem(state, name);
		}

		public void Update(Global G)
		{
			throw new NotImplementedException();
		}
	}
}