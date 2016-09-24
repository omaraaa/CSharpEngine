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
using System.Diagnostics;
using System.Runtime.Serialization.Formatters.Binary;

class Player
{
	public Fixture leg;
	public Body body;
	public int isTouching = 0;
	public Fixture side;
	public bool isControllable;
	public int id;
	public Vector2 spawnPos;
	State state;
	public int jumps = 0;
	public int maxJumps = 1;
	public bool infinite = false;

	public Player(State state, Vector2 pos, float renderlayer, bool control = true)
	{
		var transSys = state.getSystem<TransformSystem>();
		var physics = state.getSystem<PhysicsSystem>();
		var textureSys = state.getSystem<RenderSystem>();
		var spriteSys = state.G.getSystem<SpriteSystem>();
		var playerSys = state.getSystem<PlayerSystem>();
		var cameraFollow = state.getSystem<CameraFollowSystem>();

		spawnPos = pos;
		this.state = state;

		var id = state.CreateEntity();

		Transform trans = new Transform();
		trans.position = pos;
		transSys.AddComponent(id, trans);

		Texture2 texture = new Texture2(state.G, "player", renderlayer);

		Sprite spr = new Sprite(spriteSys,texture);
		spr.Play("idle", 16, true);
		textureSys.AddComponent(id, spr);

		Rectangle bounds = texture.textureRect;

		this.body = new Body(physics.world);
		this.body.FixedRotation = true;
		this.body.SleepingAllowed = false;
		this.body.IsStatic = false;
		this.body.Friction = 0f;
		this.body.Restitution = 0;
		physics.AddComponent(id, body);

		var c = FixtureFactory.AttachCircle(ConvertUnits.ToSimUnits(bounds.Width / 3f),
			1f, body, ConvertUnits.ToSimUnits(new Vector2(0, bounds.Height / 2f - bounds.Width / 3f)));
		side = FixtureFactory.AttachRectangle(ConvertUnits.ToSimUnits(((bounds.Width / 3f) * 2)), ConvertUnits.ToSimUnits(bounds.Height / 2), 1f,
			ConvertUnits.ToSimUnits(new Vector2(0, -(bounds.Height / 2f) / 2f + 14)), body);
		side.Restitution = 0;
		side.Friction = 0f;


		leg = FixtureFactory.AttachRectangle(ConvertUnits.ToSimUnits(bounds.Width / 2f), ConvertUnits.ToSimUnits(1), 0.001f, new Vector2(0, ConvertUnits.ToSimUnits(bounds.Height / 2f)), body);
		leg.IsSensor = true;
		leg.OnCollision += collision;
		leg.OnSeparation += seperation;

		isControllable = control;
		this.id = id;

		playerSys.AddComponent(id, this);
		cameraFollow.SetEntity(id);
		body.UserData = this;
	}

	public Player(int id, PhysicsSystem physics, Rectangle bounds, bool control = true)
	{

		this.body = new Body(physics.world);
		this.body.FixedRotation = true;
		this.body.SleepingAllowed = false;
		this.body.IsStatic = false;
		this.body.Friction = 0f;
		this.body.Restitution = 0;
		physics.AddComponent(id, body);

		var c = FixtureFactory.AttachCircle(ConvertUnits.ToSimUnits(bounds.Width / 3f),
			1f, body, ConvertUnits.ToSimUnits(new Vector2(0, bounds.Height / 2f - bounds.Width / 3f)));
		side = FixtureFactory.AttachRectangle(ConvertUnits.ToSimUnits(bounds.Width / 2f), ConvertUnits.ToSimUnits(bounds.Height / 2), 1f,
			ConvertUnits.ToSimUnits(new Vector2(0, -(bounds.Height / 2f) / 2f)), body);
		side.Restitution = 0;
		side.Friction = 0f;


		leg = FixtureFactory.AttachRectangle(ConvertUnits.ToSimUnits(bounds.Width / 2f), ConvertUnits.ToSimUnits(1), 0.001f, new Vector2(0, ConvertUnits.ToSimUnits(bounds.Height / 2f)), body);
		leg.IsSensor = true;
		leg.OnCollision += collision;
		leg.OnSeparation += seperation;

		isControllable = control;
	}

	public Player(World world, int b)
	{
		this.body = world.BodyList[b];
		this.side = body.FixtureList[1];
		this.leg = body.FixtureList[2];
		leg.IsSensor = true;
		leg.OnCollision += collision;
		leg.OnSeparation += seperation;
		isControllable = true;
	}

	public void setPos(Vector2 pos)
	{
		var transSys = state.getSystem<TransformSystem>();

		var t = transSys.getComponentByID(id);
		t.position = pos;

		body.Position = FarseerPhysics.ConvertUnits.ToSimUnits(pos);


	}
	bool collision(Fixture a, Fixture b, Contact contact)
	{
		if (!b.IsSensor)
		{
			if (isTouching == 0)
			{
				LD36.LD36Game.PlaySound("landsound", 0.8f, -0.3f);
			}
			isTouching++;
		}
		return true;
	}
	void seperation(Fixture a, Fixture b)
	{
		if (!b.IsSensor)
			isTouching--;
	}
}

class PlayerSystem : ComponentSystem<Player>, ISysUpdateable
{
	PhysicsSystem physics;
	RenderSystem renderSys;
	public PlayerSystem(State state) : base(state, "Player")
	{
		physics = state.getSystem<PhysicsSystem>();
		renderSys = state.getSystem<RenderSystem>();
	}

	public override BaseSystem DeserializeConstructor(State state, string name)
	{
		return new PlayerSystem(state);
	}

	public void Update(Global G)
	{
		for (int i = 0; i < size; ++i)
		{
			if (entityIDs[i] == -1)
				continue;

			var player = components[i];
			player.body.Mass = 1;
			var keyState = G.getSystem<MG.MonogameSystem>().keyboardState;
			var gamepad = G.getSystem<MG.MonogameSystem>().gamepadState;
			var vel = player.body.LinearVelocity;
			bool moving = false;
			var speed = ConvertUnits.ToSimUnits(3030);
			var maxVel = ConvertUnits.ToSimUnits(100);
			var spriteIndex = _state.getComponentIndex(entityIDs[i], renderSys.systemIndex);
			if (player.isControllable)
			{
				if (keyState.IsKeyDown(Keys.A) || gamepad.IsButtonDown(Buttons.DPadLeft))
				{
					if (spriteIndex != -1)
					{
						var spr = (Sprite) renderSys.getComponent(spriteIndex);
						spr.flipH = true;
					}
					player.body.ApplyForce(new Vector2(-speed, 0));
					moving = true;
				}
				if (keyState.IsKeyDown(Keys.D) || gamepad.IsButtonDown(Buttons.DPadRight))
				{
					if (spriteIndex != -1)
					{
						var spr = (Sprite)renderSys.getComponent(spriteIndex);
						spr.flipH = false;
					}
					player.body.ApplyForce(new Vector2(speed, 0));

					moving = true;
				}
				if (player.isTouching > 0)
				{
					player.jumps = 0;

				}
				var jumpkey = (keyState.IsKeyDown(Keys.Space) || gamepad.IsButtonDown(Buttons.A));
				if (jumpkey  && player.isTouching > 0)
				{
					vel.Y = -ConvertUnits.ToSimUnits(420);
					moving = true;
					player.jumps++;
					LD36.LD36Game.PlaySound("jumpsound");
				}

				if (jumpkey && vel.Y > 1f && (player.jumps < player.maxJumps || player.infinite))
				{
					vel.Y = -ConvertUnits.ToSimUnits(420);
					moving = true;
					player.jumps++;
					if (player.infinite)
						LD36.LD36Game.PlaySound("jumpsound", 1, 0);
					else
						LD36.LD36Game.PlaySound("jumpsound", 1, ((float)player.jumps) / (player.maxJumps + 1));

				}
			}
			if (spriteIndex != -1)
			{
				var spr = (Sprite)renderSys.getComponent(spriteIndex);
				if (player.isTouching == 0)
				{
					if (vel.Y < 0f)
						spr.Play("jumping", 30, false);
					if (vel.Y > 1f)
						spr.Play("falling", 30, false);
				}
				else
				{
					if (vel.X > ConvertUnits.ToSimUnits(2f))
					{
						spr.Play("move", 8*Math.Abs((int)ConvertUnits.ToDisplayUnits(vel.X)/50), true);
						spr.flipH = false;
					}
					else if (vel.X < -ConvertUnits.ToSimUnits(2f))
					{
						spr.Play("move", 8 * Math.Abs((int)ConvertUnits.ToDisplayUnits(vel.X)/50), true);
						spr.flipH = true;
					}
					else
					{
						//spriteSys.Play("jumping", entityIDs[i], 30, false);
						spr.Play("idle", 8, true);
					}
				}

				renderSys.SetComponent(entityIDs[i], spr);
			}

			if (Math.Abs(vel.X) > maxVel)
				vel.X = maxVel * (vel.X > 0 ? 1 : -1);

			player.body.LinearVelocity = vel;
			//player.side.Position = player.body.Position;
		}
	}

	public override void RemoveEntity(int id)
	{
		var index = _state.getComponentIndex(id, systemIndex);
		physics.world.RemoveBody(components[index].body);

		base.RemoveEntity(id);
	}

	public void setControl(int id)
	{
		var index = _state.getComponentIndex(id, systemIndex);
		if (index != -1)
			components[index].isControllable = true;

		for (int i = 0; i < size; ++i)
		{
			if (entityIDs[i] == -1 || i == index)
				continue;

			components[i].isControllable = false;
		}
	}

	protected override void SerailizeComponent(ref Player component, BinaryWriter writer)
	{
		for (int i = 0; i < physics.world.BodyList.Count; ++i)
		{
			var b = physics.world.BodyList[i];
			if (b.BodyId == component.body.BodyId)
				writer.Write(i);
		}
	}

	protected override Player DeserailizeComponent(BinaryReader reader)
	{
		int body = reader.ReadInt32();
		Player p = new Player(physics.world, body);
		return p;
	}

	public override void SerializeSystem(BinaryWriter writer)
	{
	}

	public override void DeserializeSystem(BinaryReader reader)
	{
	}
}