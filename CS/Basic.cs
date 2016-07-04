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
				t.velocity += t.acceleration * G.dt;
				t.position += t.velocity * G.dt;
			}
		}
	}

	class MouseFollowSystem : BaseSystem, ISysUpdateable
	{
		private TransformSystem transform;
		public MouseFollowSystem(State state, TransformSystem transSys) : base(state)
		{
			transform = transSys;
		}

		private uint updateindex;
		public uint UpdateIndex
		{
			get
			{
				return updateindex;
			}
		}

		public void Update(Global G)
		{
			foreach(uint entity in entityIDs)
			{
				var transfromIndex = _state.entitiesIndexes[entityIDs[entity]][transform.systemIndex];
				var transformC = transform.getComponent(transfromIndex);
				var pos = transformC.position;
				pos.X = G.mouseState.X;
				pos.Y = G.mouseState.Y;
				transformC.position = pos;
			}
		}
	}

	class Texture2
	{
		Texture2D texture;
		float layerDepth;
		Vector2 offset;

		public Texture2(Global G, String textureString, float layer = 0.9f)
		{
			this.texture = G.getTexture(textureString);
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

	


}


namespace Entities
{
	using CS.Components;
	
}
