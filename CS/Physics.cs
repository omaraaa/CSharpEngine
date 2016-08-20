using System.Collections.Generic;
using System.Collections;
using System;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using Microsoft.Xna.Framework.Graphics;

using CS;
using CS.Util;

using FarseerPhysics.Dynamics;
using FarseerPhysics.Collision;
using FarseerPhysics.Collision.Shapes;
using FarseerPhysics.Common;
using FarseerPhysics.Factories;
using FarseerPhysics.Controllers;
using FarseerPhysics;
using FarseerPhysics.DebugView;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

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
			debugView.LoadContent(G.game.GraphicsDevice, G.game.Content);
			debugView.Enabled = true;
#endif
			batch = new SpriteBatch(G.game.GraphicsDevice);
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
			var proj = Matrix.CreateOrthographicOffCenter(ConvertUnits.ToSimUnits( 0 + _state.camera.position.X - _state.camera.center.X),
				ConvertUnits.ToSimUnits(G.game.GraphicsDevice.Viewport.Width + _state.camera.position.X - _state.camera.center.X),
				ConvertUnits.ToSimUnits(G.game.GraphicsDevice.Viewport.Height + _state.camera.position.Y - _state.camera.center.Y),
				ConvertUnits.ToSimUnits(0 + _state.camera.position.Y - _state.camera.center.Y),
				0, 100) * Matrix.CreateScale(new Vector3(_state.camera.scale, 0));
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
				if (entityIDs[i] == -1 || !components[i].Awake)
					continue;

				var index = _state.getComponentIndex(entityIDs[i], transformSys.systemIndex);
				if (index != -1)
				{
					var trans = transformSys.getComponent(index);
					trans.position = Vector2.LerpPrecise(ConvertUnits.ToDisplayUnits(components[i].Position), trans.deltaPos, G.dt);
				}

				}
		}

		public override BaseSystem DeserializeConstructor(State state, string name)
		{
			return new PhysicsSystem(state);
		}

		public override void Serialize(BinaryWriter writer)
		{
			base.Serialize(writer);

			WorldSerializer.Serialize(world, writer);
			for (int i = 0; i < world.BodyList.Count; ++i)
			{
				for(int j = 0; j < size; ++j)
				{
					if(components[j] == world.BodyList[i])
						writer.Write(i);

				}
			}
		}

		public override void Deserialize(BinaryReader reader)
		{
			base.Deserialize(reader);

			world = new World(Vector2.Zero);
			WorldSerializer.Deserialize(world, reader);
			for(int i = 0; i < size; ++i)
			{
				int id = reader.ReadInt32();
				components[i] = world.BodyList[id];
			}
#if DEBUG
			debugView = new DebugViewXNA(world);
			debugView.LoadContent(_state.G.game.GraphicsDevice, _state.G.game.Content);
			debugView.Enabled = true;
#endif
		}

		public override void RemoveEntity(int id)
		{
			var index = _state.getComponentIndex(id, systemIndex);
			world.RemoveBody(components[index]);
			base.RemoveEntity(id);
		}
	}

}