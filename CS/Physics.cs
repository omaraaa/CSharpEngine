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

namespace CS.Components
{
	class PhysicsObject
	{
		public Body body;
		public Vector2 origin;

		public PhysicsObject(PhysicsSystem sys, int width, int height, BodyType bodytype = BodyType.Static)
		{
			body = new Body(sys.world);
			body.BodyType = bodytype;
			//body.Awake = false;
			body.SleepingAllowed = true;
			//body.FixedRotation = true;
			origin = new Vector2(ConvertUnits.ToSimUnits( width), ConvertUnits.ToSimUnits(height));
			if(bodytype == BodyType.Dynamic || bodytype == BodyType.Kinematic)
			FixtureFactory.AttachCircle(ConvertUnits.ToSimUnits( Math.Min(width, height))/2, 1f, body);
			else
			FixtureFactory.AttachRectangle(ConvertUnits.ToSimUnits(width), ConvertUnits.ToSimUnits(height), 1f, Vector2.Zero, body);
			//body.Restitution = 0.1f;
			body.Friction = 1f;
		}
	}

	class PhysicsSystem : ComponentSystem<PhysicsObject>, ISysUpdateable
	{
		public World world;
		private TransformSystem transformSys;

		public PhysicsSystem(State state) : base(state)
		{
			transformSys = state.getSystem<TransformSystem>();
			world = new World(new Vector2(0,100));
			ConvertUnits.SetDisplayUnitToSimUnitRatio(8);
			Settings.ContinuousPhysics = false;
			Settings.VelocityIterations = 1;
			Settings.PositionIterations = 1;
			Settings.EnableDiagnostics.Equals(false);
			Settings.DefaultFixtureIgnoreCCDWith = Category.All;
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
					components[i].body.Position = ConvertUnits.ToSimUnits(trans.position);
				}
			}

			if(G.dt < 1/60f)
				world.Step(G.dt);
			else
				world.Step(1 / 60f);

			for (int i = 0; i < size; ++i)
			{
				if (entityIDs[i] == -1 || !components[i].body.Awake)
					continue;

				var index = _state.getComponentIndex(entityIDs[i], transformSys.systemIndex);
				if (index != -1)
				{
					var trans = transformSys.getComponent(index);
					trans.position = ConvertUnits.ToDisplayUnits(components[i].body.Position);
				}

				
			}
		}
	}
}