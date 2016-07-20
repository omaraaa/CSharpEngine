using System;
using CS;
using CS.Components;


class Player
{

}

class PlayerSystem : ComponentSystem<Player>, ISysUpdateable
{
	public PlayerSystem(State state) : base(state)
	{
	}

	public void Update(Global G)
	{
		throw new NotImplementedException();
	}
}