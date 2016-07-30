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
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

class Player
{
	public Body leg;
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

	public Player(World world, int b, int s, int l)
	{
		this.body = world.BodyList[b];
		this.side = world.BodyList[s];
		this.leg = world.BodyList[l];
		leg.IsSensor = true;
		leg.OnCollision += collision;
		leg.OnSeparation += seperation;

		leg.IgnoreCollisionWith(body);
		leg.SleepingAllowed = false;
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
	PhysicsSystem physics;
	public PlayerSystem(State state) : base(state, "Player")
	{
		physics = state.getSystem<PhysicsSystem>();
	}

	public override BaseSystem DeserializeConstructor(State state)
	{
		return new PlayerSystem(state);
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

	public override void Serialize(FileStream fs, BinaryFormatter formatter)
	{
		base.Serialize(fs, formatter);
		foreach(Player p in components)
		{
			formatter.Serialize(fs, p.body.BodyId);
			formatter.Serialize(fs, p.side.BodyId);
			formatter.Serialize(fs, p.leg.BodyId);
		}
	}

	public override void Deserialize(FileStream fs, BinaryFormatter formatter)
	{
		base.Deserialize(fs, formatter);
		components = new Player[size];
		for(int i = 0; i < size; ++i)
		{
			var body = (int) formatter.Deserialize(fs);
			var side = (int) formatter.Deserialize(fs);
			var leg = (int) formatter.Deserialize(fs);

			Player p = new Player(physics.world, body, side, leg);
			

			components[i] = p;
		}
	}
}