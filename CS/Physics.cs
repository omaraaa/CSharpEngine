using System.Collections.Generic;
using System.Collections;
using System;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using Microsoft.Xna.Framework.Graphics;

using CS;
using Util;

using FarseerPhysics.Dynamics;
using FarseerPhysics.Collision;
using FarseerPhysics.Collision.Shapes;
using FarseerPhysics.Common;
using FarseerPhysics.Factories;
using FarseerPhysics.Controllers;
using FarseerPhysics;
#if DEBUG
using FarseerPhysics.DebugView;
#endif
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using MG;

namespace CS.Components
{
	class PhysicsObject
	{

		static public Body CreateBody(PhysicsSystem sys, int width, int height, int bodytype = (int) BodyType.Dynamic)
		{
			var body = new Body(sys.world);
			body.BodyType = (BodyType) bodytype;
			//body.Awake = false;
			//body.SleepingAllowed = true;
			//body.FixedRotation = true;
			//if(bodytype == BodyType.Dynamic || bodytype == BodyType.Kinematic)
			//FixtureFactory.AttachCircle(ConvertUnits.ToSimUnits( Math.Min(width, height))/2, 1f, body);
			//else
			FixtureFactory.AttachRectangle(ConvertUnits.ToSimUnits(width), ConvertUnits.ToSimUnits(height), 10f, Vector2.Zero, body);
			//body.Restitution = 0f;
			body.Friction = 0.2f;

			return body;
		}


	}

	class PhysicsSystem : ComponentSystem<Body>, ISysUpdateable, ISysRenderable
	{
		public World world;
		private TransformSystem transformSys;
#if DEBUG
		DebugViewXNA debugView;
#endif
		SpriteBatch batch;

		bool isSerializingWhole = false;

		public PhysicsSystem(State state) : base(state, "FarseerPhysicsSystem")
		{
			var G = state.G;
			transformSys = state.getSystem<TransformSystem>();
			ConvertUnits.SetDisplayUnitToSimUnitRatio(64);
			world = new World(new Vector2(0, ConvertUnits.ToSimUnits(2000)));
			//Settings.ContinuousPhysics = false;
			Settings.VelocityIterations = 1;
			Settings.PositionIterations = 1;
			//Settings.DefaultFixtureIgnoreCCDWith = Category.All;
#if DEBUG
			debugView = new DebugViewXNA(world);
			debugView.LoadContent(G.getSystem<MonogameSystem>().Game.GraphicsDevice, G.getSystem<MonogameSystem>().Game.Content);
			debugView.Enabled = true;
#endif
			batch = new SpriteBatch(G.getSystem<MonogameSystem>().Game.GraphicsDevice);
		}

		public SpriteBatch Batch
		{
			get
			{
				return batch;
			}
		}

		public override int AddComponent(int id, Body component)
		{
			var r = base.AddComponent(id, component);
			world.ProcessChanges();
			return r;
		}

		public void Render(Global G)
		{
#if DEBUG
			var camera = _state.getSystem<Camera>();
			var proj = Matrix.CreateOrthographicOffCenter(ConvertUnits.ToSimUnits( 0 + camera.position.X - camera.center.X),
				ConvertUnits.ToSimUnits(G.getSystem<MonogameSystem>().Game.GraphicsDevice.Viewport.Width + camera.position.X - camera.center.X),
				ConvertUnits.ToSimUnits(G.getSystem<MonogameSystem>().Game.GraphicsDevice.Viewport.Height + camera.position.Y - camera.center.Y),
				ConvertUnits.ToSimUnits(0 + camera.position.Y - camera.center.Y),
				0, 100) * Matrix.CreateScale(new Vector3(camera.scale, 0));
			batch.Begin();
			debugView.RenderDebugData(ref proj);
			//debugView.DrawString(0, 0, "test");
			batch.End();
#endif
		}

		public void Update(Global G)
		{

			for (int i = 0; i < size; ++i)
			{
				if (entityIDs[i] == -1)
					continue;

				int index = 0;
				if (transformSys.ContainsEntity(entityIDs[i], ref index))
				{
					var trans = transformSys.getComponent(index);
					if (float.IsNaN(trans.position.X) && float.IsNaN(trans.position.Y))
						trans.position = Vector2.Zero;
					components[i].Position = ConvertUnits.ToSimUnits(trans.position);
				}
			}

			if(G.dt < 1/30f)
				world.Step(G.dt);
			else
				world.Step(1 / 30f);


			for (int i = 0; i < size; ++i)
			{
				//if (!components [i].Awake)
				//	components [i].IsStatic = true;
				if (entityIDs[i] == -1 || !components[i].Awake || components[i].IsStatic)
					continue;

				var index = _state.getComponentIndex(entityIDs[i], transformSys.systemIndex);
				if (index != -1)
				{
					var trans = transformSys.getComponent(index);
					trans.position = 
						ConvertUnits.ToDisplayUnits(components[i].Position);
				}

				}
		}

		public override BaseSystem DeserializeConstructor(State state, string name)
		{
			return new PhysicsSystem(state);
		}

		public override void RemoveEntity(int id)
		{
			var index = _state.getComponentIndex(id, systemIndex);
			world.RemoveBody(components[index]);
			base.RemoveEntity(id);
		}

		protected override void SerailizeComponent(ref Body component, BinaryWriter writer)
		{
			WorldBinarySerializer.SerializeBody(component, writer);
		}

		protected override Body DeserailizeComponent(BinaryReader reader)
		{
			Body b = WorldBinaryDeserializer.DeserializeBody(world, reader);
			return b;
		}

		public override void SerializeSystem(BinaryWriter writer)
		{
			WorldSerializer.Serialize(world, writer);
		}

		public override void DeserializeSystem(BinaryReader reader)
		{
			world = new World(Vector2.Zero);
			WorldSerializer.Deserialize(world, reader);


		}

		protected override void postDeserialization(BinaryReader reader)
		{
			world.ProcessChanges();
#if DEBUG
			debugView = new DebugViewXNA(world);
			debugView.LoadContent(_state.G.getSystem<MonogameSystem>().Game.GraphicsDevice, _state.G.getSystem<MonogameSystem>().Game.Content);
			debugView.Enabled = true;
#endif


			base.postDeserialization(reader);
		}
	}

}