using System;
using CS;
using CS.Components;

using FarseerPhysics.Dynamics;
using FarseerPhysics.Dynamics.Joints;
using FarseerPhysics.Dynamics.Contacts;
using FarseerPhysics;
using FarseerPhysics.Factories;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework;

class Player
{
	Body leg;
	public Body body;
	public int isTouching = 0;
	public Body side;

	public Player(World world, Body body)
	{
		
		this.body = body;
		this.body.FixedRotation = true;
		this.body.SleepingAllowed = false;
		this.body.Friction = 1f;
		this.body.Mass = 1;

		side = BodyFactory.CreateRectangle(world, ConvertUnits.ToSimUnits(100 + 8), ConvertUnits.ToSimUnits(10), 1);
		
		side.IsStatic = false;
		side.GravityScale = 0;
		side.Mass = 0;
		side.Friction = 0;
		//side.FixedRotation = true;
		//leftSide.IgnoreCollisionWith(body);
		side.SleepingAllowed = false;

		//world.Step(0);

		//Joint j = new Jo
		var j = JointFactory.CreateWeldJoint(world, this.body, side, this.body.Position, this.body.Position);
		leg = BodyFactory.CreateRectangle(world, ConvertUnits.ToSimUnits(100 - 4), ConvertUnits.ToSimUnits(1), 1f);
		leg.IsSensor = true;
		leg.OnCollision += collision;
		leg.OnSeparation += seperation;

		leg.IsStatic = false;
		leg.GravityScale = 0;
		leg.Mass = 0;
		leg.Friction = 0;
		leg.FixedRotation = true;
		leg.IgnoreCollisionWith(body);
		leg.SleepingAllowed = false;

		var j2 = JointFactory.CreateWeldJoint(world, side, leg, this.body.Position, this.body.Position - new Vector2(0, ConvertUnits.ToSimUnits(32 + 16 + 1)));
		world.Step(0);
	}
	bool collision(Fixture a, Fixture b, Contact contact)
	{
		isTouching++;
		return true;
	}
	void seperation(Fixture a, Fixture b)
	{
		isTouching--;
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
			bool moving = false;
			var speed = ConvertUnits.ToSimUnits(230);

			if (keyState.IsKeyDown(Keys.A) && vel.X > -speed)
			{
				vel.X += -speed;
				moving = true;
			}
			if (keyState.IsKeyDown(Keys.D) && vel.X < speed)
			{
				vel.X += speed;
				moving = true;
			}

			if(keyState.IsKeyDown(Keys.Space) && player.isTouching > 0)
			{
				vel.Y = -ConvertUnits.ToSimUnits(500);
				moving = true;

			}

			player.body.LinearVelocity = vel;
			//player.side.Position = player.body.Position;
		}
	}
}