using System.Collections.Generic;
using System.Collections;
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;

using CS;

namespace CS.Components
{
	class Transform
	{
		public Vector2 position;
		public Vector2 velocity;
		public Vector2 acceleration;
	}
	class TransformSystem : ComponentSystem<Transform>, ISysUpdateable
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
			foreach (Transform t in components)
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
			foreach(int entity in entityIDs)
			{
				if (entity == -1)
					continue;

				var transfromIndex = _state.entitiesIndexes[entity][transform.systemIndex];
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
		Vector2 scale;
		public Rectangle textureRect;

		public Texture2(Global G, String textureString, float layer = 0.9f)
		{
			this.texture = G.getTexture(textureString);
			this.layerDepth = layer;
			this.offset = new Vector2(0, 0);

			this.scale = new Vector2(1, 1);

			textureRect = new Rectangle(0, 0, texture.Width, texture.Height);
		}

		public void Render(SpriteBatch batch, Vector2 position)
		{
			position += offset;
			batch.Draw(texture, position: position.ToPoint().ToVector2(), scale: scale, layerDepth: layerDepth);
		}

		public void setScale(float x, float y)
		{
			scale = new Vector2(x, y);
			textureRect.Width = (int) ((float) texture.Width * x);
			textureRect.Height = (int) ((float) texture.Height * y);
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

			_batch.Begin(sortMode: SpriteSortMode.BackToFront,samplerState: SamplerState.PointWrap);
			for(int i = 0; i < size; ++i)
			{
				var transfromIndex = _state.entitiesIndexes[entityIDs[i]][transform.systemIndex];
				var transformC = transform.getComponent(transfromIndex);
				var textureC = components[i];
				textureC.Render(_batch, transformC.position);
			}
			_batch.End();
		}

		public Rectangle getRect(int id)
		{
			var index = _state.getComponentIndex(id, systemIndex);
			if (index == -1)
				return Rectangle.Empty;

			var textureC = components[index];
			Rectangle rect = textureC.textureRect;

			var transfromIndex = _state.entitiesIndexes[entityIDs[index]][transform.systemIndex];
			if (transfromIndex != -1)
			{
				var transformC = transform.getComponent(transfromIndex);
				rect.X = (int)transformC.position.X;
				rect.Y = (int)transformC.position.Y;
			}

			return rect;
		}
	}

	class DragAndDropSystem : BaseSystem, ISysUpdateable
	{
		bool mhold = false;
		int heldEntity = -1;
		Point offset;
		TextureSystem textureSys;
		TransformSystem transfromSys;

		public DragAndDropSystem(State state) : base(state)
		{
			offset = new Point(0, 0);
			textureSys = _state.getSystem<TextureSystem>();
			transfromSys = _state.getSystem<TransformSystem>();
		}

		public uint UpdateIndex
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public void Update(Global G)
		{
			var mx = G.mouseState.X;
			var my = G.mouseState.Y;
			var leftPressed = G.mouseState.LeftButton == ButtonState.Pressed;
			if (leftPressed && !mhold)
			{
				foreach (var e in entityIDs)
				{
					if (e == -1)
						continue;

					var tIndex = _state.getComponentIndex(e, systemIndex);
					if (tIndex != -1)
					{
						var rect = textureSys.getRect(e);
						if(rect.Contains(mx, my))
						{
							mhold = true;
							heldEntity = e;
							offset.X = mx - rect.X;
							offset.Y = my - rect.Y;
						}
					}
				}
			}
			else if(!leftPressed && mhold)
			{
				mhold = false;
				heldEntity = -1;
			}

			if(mhold)
			{
				int transIndex = -1;
				if(transfromSys.ContainsEntity(heldEntity, ref transIndex))
				{
					var trans = transfromSys.getComponent(transIndex);
					trans.position.X = mx - offset.X;
					trans.position.Y = my - offset.Y;
				}
			}

		}
	}


}


namespace Entities
{
	using CS.Components;
	class Image
	{
		public int id;
		uint textureIndex;
		uint transformIndex;

		public Image(State state, String image, Vector2 position, float layer = 0.9f)
		{
			var transformSys = state.getSystem<TransformSystem>();
			var textureSys = state.getSystem<TextureSystem>();

			id = state.CreateEntity();

			Transform transfom = new Transform();
			transfom.position = position;
			transformSys.AddComponent(id, transfom);

			Texture2 texture = new Texture2(state.G, image, layer);
			textureSys.AddComponent(id, texture);
		}
	}
	
}
