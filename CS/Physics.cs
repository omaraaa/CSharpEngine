using System.Collections.Generic;
using System.Collections;
using System;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using Microsoft.Xna.Framework.Graphics;

using CS;

using FarseerPhysics.Dynamics;
using FarseerPhysics.Collision;
using FarseerPhysics.Collision.Shapes;
using FarseerPhysics.Common;
using FarseerPhysics.Factories;
using FarseerPhysics.Controllers;
using FarseerPhysics;
using FarseerPhysics.DebugView;

namespace CS.Components
{
	class PhysicsObject
	{

		static public Body CreateBody(PhysicsSystem sys, int width, int height, BodyType bodytype = BodyType.Static)
		{
			var body = new Body(sys.world);
			body.BodyType = bodytype;
			//body.Awake = false;
			body.SleepingAllowed = true;
			//body.FixedRotation = true;
			if(bodytype == BodyType.Dynamic || bodytype == BodyType.Kinematic)
			FixtureFactory.AttachCircle(ConvertUnits.ToSimUnits( Math.Min(width, height))/2, 1f, body);
			else
			FixtureFactory.AttachRectangle(ConvertUnits.ToSimUnits(width), ConvertUnits.ToSimUnits(height), 0.1f, Vector2.Zero, body);
			//body.Restitution = 1f;
			body.Friction = 1f;

			return body;
		}


	}

	class PhysicsSystem : ComponentSystem<Body>, ISysUpdateable, ISysRenderable
	{
		public World world;
		private TransformSystem transformSys;
		DebugViewXNA debugView;
		SpriteBatch batch;

		public PhysicsSystem(State state) : base(state)
		{
			transformSys = state.getSystem<TransformSystem>();
			world = new World(new Vector2(0,10));
			ConvertUnits.SetDisplayUnitToSimUnitRatio(64);
			Settings.ContinuousPhysics = false;
			Settings.VelocityIterations = 6;
			Settings.PositionIterations = 2;
			Settings.EnableDiagnostics.Equals(false);
			Settings.DefaultFixtureIgnoreCCDWith = Category.All;
			debugView = new DebugViewXNA(world);
			debugView.LoadContent(state.G.game.GraphicsDevice, state.G.game.Content);
			debugView.Enabled = true;
			batch = new SpriteBatch(state.G.game.GraphicsDevice);
		}

		public SpriteBatch Batch
		{
			get
			{
				return batch;
			}
		}

		public void Render(Global G)
		{
			#if DEBUG
			var proj = Matrix.CreateOrthographicOffCenter(ConvertUnits.ToSimUnits(0), ConvertUnits.ToSimUnits(G.game.GraphicsDevice.Viewport.Width)
				, ConvertUnits.ToSimUnits(G.game.GraphicsDevice.Viewport.Height), ConvertUnits.ToSimUnits(0), 0, 100);
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
					components[i].Position = ConvertUnits.ToSimUnits(trans.position);
				}
			}

			if(G.dt < 1/30f)
				world.Step(G.dt);
			else
				world.Step(1 / 30f);

			for (int i = 0; i < size; ++i)
			{
				if (!components [i].Awake)
					components [i].IsStatic = true;
				if (entityIDs[i] == -1 || !components[i].Awake)
					continue;

				var index = _state.getComponentIndex(entityIDs[i], transformSys.systemIndex);
				if (index != -1)
				{
					var trans = transformSys.getComponent(index);
					trans.position = ConvertUnits.ToDisplayUnits(components[i].Position);
				}

				}
		}
	}
}