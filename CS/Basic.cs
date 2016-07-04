using System.Collections.Generic;
using System.Collections;
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using CS;

namespace CS.Components
{
	class transform
	{
		public Vector2 position;
		public Vector2 velocity;
		public Vector2 acceleration;
	}
	class TransformSystem : ComponentSystem<transform>, ISysUpdateable
	{
		public TransformSystem(State state) : base(state)
		{

		}

		private uint updateindex;
		public uint UpdateIndex
		{
			get
			{
				return updateindex;
			}
		}

		void ISysUpdateable.Update(Global G)
		{
			foreach (transform t in components)
			{
				var dt = (float)G.gametime.ElapsedGameTime.TotalSeconds;
				t.velocity += t.acceleration * dt;
				t.position += t.velocity * dt;
			}
		}
	}

	class Texture2
	{
		Transform transform;
		Texture2D texture;
		float layerDepth;
		Vector2 offset;

		public Texture2(Global G, String textureString, Transform transform = null, float layer = 0.9f)
		{
			this.texture = G.getTexture(textureString);
			this.transform = transform;
			this.layerDepth = layer;
			this.offset = new Vector2(0, 0);
		}

		public void Render(SpriteBatch batch, Vector2 position)
		{
			position += offset;
			batch.Draw(texture, position: position, layerDepth: layerDepth);
		}
	}

	class TextureSystem : ComponentSystem<Texture2>, ISysRenderable
	{
		TransformSystem transform;
		public TextureSystem(State state, GraphicsDevice graphicsDevice, TransformSystem transform) : base(state)
		{
			_batch = new SpriteBatch(graphicsDevice);
			this.transform = transform;
		}

		SpriteBatch _batch;
		public SpriteBatch Batch
		{
			get
			{
				return _batch;
			}
		}

		uint _renderIndex;
		public uint RenderIndex
		{
			get
			{
				return _renderIndex;
			}
		}

		public void Render()
		{
			_batch.Begin();
			for(int i = 0; i < size; ++i)
			{
				var transfromIndex = _state.entitiesIndexes[entityIDs[i]][transform.systemIndex];
				var transformC = transform.getComponent(transfromIndex);
				var textureC = components[i];
				textureC.Render(_batch, transformC.position);
			}
			_batch.End();
		}
	}

	class Transform : UpdatableComponent
	{
		private Vector2 pos;
		private Vector2 vel;
		private Vector2 acc;

		public Transform(Entity e, float x, float y) : base(e)
		{
			this.GetType();
			this.pos = new Vector2(x, y);
			this.vel = new Vector2();
			this.acc = new Vector2();
			if (vel == Vector2.Zero && acc == Vector2.Zero)
			{
				setSleeping(true);
				return;
			}
		}

		override public void Update(ref Global G)
		{
			var dt = (float)G.gametime.ElapsedGameTime.TotalSeconds;

			vel.X += acc.X * dt;
			vel.Y += acc.Y * dt;

			pos.X += vel.X * dt;
			pos.Y += vel.Y * dt;
		}

		public Vector2 Position
		{
			get
			{
				return pos;
			}

			set
			{
				pos = value;
			}
		}

		public Vector2 Velocity
		{
			get
			{
				return vel;
			}

			set
			{
				
				vel = value;
				if (!(vel == Vector2.Zero && acc == Vector2.Zero))
					if(sleep)
						setSleeping(false);
				else
					setSleeping(false);


			}
		}

		public Vector2 Acceleration
		{
			get
			{
				return vel;
			}

			set
			{
				vel = value;
				if (!(vel == Vector2.Zero && acc == Vector2.Zero))
					if (sleep)
						setSleeping(false);
					else
						setSleeping(false);
			}
		}

	}

	class MouseFollower : UpdatableComponent
	{
		private Transform transform;

		public MouseFollower(Entity e, Transform transform) : base(e)
		{
			this.transform = transform;
		}

		public override void Update(ref Global G)
		{
			var pos = transform.Position;
			pos.X = G.mouseState.X;
			pos.Y = G.mouseState.Y;
			transform.Position = pos;
		}
	}

	class Texture2DC : RenderableComponent
	{
		Transform transform;
		Texture2D texture;
		float layerDepth;
		Vector2 offset;

		public Texture2DC(Entity entity, String textureString, Transform transform = null, float layer = 0.9f) : base(entity, typeof(Texture2DC), typeof(Transform))
		{
			this.texture = entity.State.G.getTexture(textureString);
			this.transform = transform;
			this.layerDepth = layer;
			this.offset = new Vector2(0, 0);
		}

		public override void Update(ref Global G)
		{

		}

		public override void Render(SpriteBatch batch)
		{
			Vector2 position;
			if (transform != null)
				position = transform.Position + offset;
			else
				position = offset;
			batch.Draw(texture, position: position, layerDepth: layerDepth);
		}
	}
}


namespace Entities
{
	using CS.Components;
	class Cursor
	{
		Entity entity;
		Transform pos;
		MouseFollower mf;
		Texture2DC text;
		public Cursor(ref State state, String textureString)
		{
			entity = state.CreateEntity();
			pos = new Transform(entity, 10, 10);
			mf = new MouseFollower(entity, pos);
			text = new Texture2DC(entity, textureString, pos, layer:1);
		}
	}

	class Image
	{
		Entity entity;
		Transform pos;
		Texture2DC text;
		public Image(State state, String textureString, float x, float y, float layer = 0)
		{
			entity = state.CreateEntity();
			pos = new Transform(entity, x, y);
			text = new Texture2DC(entity, textureString, pos, layer);
		}
	}

	class TestState : State
	{
		public TestState(Global G, int offset) : base(G)
		{
			 new Image(this, "landscape1", 32 + offset, 32, 0);
		}

	}
}
