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
	public Fixture leg;
	public Body body;
	public int isTouching = 0;
	public Fixture side;

	public Player(int id, PhysicsSystem physics, Rectangle bounds)
	{

		this.body = new Body(physics.world);
		this.body.FixedRotation = true;
		this.body.SleepingAllowed = false;
		this.body.IsStatic = false;
		this.body.Friction = 0.5f;
		this.body.Restitution = 0;
		physics.AddComponent(id, body);

		FixtureFactory.AttachCircle(ConvertUnits.ToSimUnits(bounds.Width / 3f), 1f, body, ConvertUnits.ToSimUnits(new Vector2(0, bounds.Height/2f - bounds.Width / 3f)));

		side = FixtureFactory.AttachRectangle(ConvertUnits.ToSimUnits(bounds.Width+ 2), ConvertUnits.ToSimUnits(bounds.Height/2), 1f,
			ConvertUnits.ToSimUnits(new Vector2(0, -(bounds.Height/2f)/2f)), body);
		side.Restitution = 0;
		side.Friction = 0f;


		leg = FixtureFactory.AttachRectangle(ConvertUnits.ToSimUnits(bounds.Width - 4), ConvertUnits.ToSimUnits(1), 0.001f, new Vector2(0, ConvertUnits.ToSimUnits(32 + 16 + 1)), body);
		leg.IsSensor = true;
		leg.OnCollision += collision;
		leg.OnSeparation += seperation;

	}

	public Player(World world, int b)
	{
		this.body = world.BodyList[b];
		this.side = body.FixtureList[1];
		this.leg = body.FixtureList[2];
		leg.IsSensor = true;
		leg.OnCollision += collision;
		leg.OnSeparation += seperation;

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

			if (keyState.IsKeyDown(Keys.A))
			{
				vel.X = -speed;
				moving = true;
			}
			if (keyState.IsKeyDown(Keys.D) )
			{
				vel.X = speed;
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
		for (int i = 0; i < physics.world.BodyList.Count; ++i)
		{
			var b = physics.world.BodyList[i];
			foreach (Player p in components)
			{
				if (b.BodyId == p.body.BodyId)
					formatter.Serialize(fs, i);
			}
		}
	}

	public override void Deserialize(FileStream fs, BinaryFormatter formatter)
	{
		base.Deserialize(fs, formatter);
		components = new Player[size];
		for(int i = 0; i < size; ++i)
		{
			var body = (int) formatter.Deserialize(fs);

			Player p = new Player(physics.world, body);
			

			components[i] = p;
		}
	}
}