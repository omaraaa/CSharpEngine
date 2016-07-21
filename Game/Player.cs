using System;
using CS;
using CS.Components;

using FarseerPhysics.Dynamics;
using FarseerPhysics.Dynamics.Contacts;
using FarseerPhysics;
using FarseerPhysics.Factories;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework;

class Player
{
	public Fixture leg;
	public Body body;
	public int isTouching = 0;

	public Player(Body body)
	{
		leg = FixtureFactory.AttachCircle(ConvertUnits.ToSimUnits(32), 0f, body, new Vector2(0, ConvertUnits.ToSimUnits(32)), false);
		leg.IsSensor = true;
		leg.OnCollision = collision;
		leg.OnSeparation = seperation;
		this.body = body;
		body.FixedRotation = true;
	}
	bool collision(Fixture a, Fixture b, Contact contact)
	{
		isTouching++;
		a.UserData = true;
		return true;
	}
	void seperation(Fixture a, Fixture b)
	{
		isTouching--;
		a.UserData = false;
	}
}

class PlayerSystem : ComponentSystem<Player>, ISysUpdateable
{
	public PlayerSystem(State state) : base(state)
	{
	}

	public void Update(Global G)
	{
		for(int i = 0; i < size; ++i)
		{
			if (entityIDs[i] == -1)
				continue;

			var player = components[i];
			var keyState = G.keyboardState;
			var vel = player.body.LinearVelocity;


			if (keyState.IsKeyDown(Keys.A))
			{
				player.body.ApplyForce(new Vector2(-10000, 0));
			}
			if (keyState.IsKeyDown(Keys.D))
			{
				player.body.ApplyForce(new Vector2(10000, 0));
			}
			var isTouching = player.leg.UserData as bool?;
			if(keyState.IsKeyDown(Keys.Space) && player.isTouching > 0)
			{
				vel.Y = -50;

			}
			player.body.LinearVelocity = vel;
		}
	}
}