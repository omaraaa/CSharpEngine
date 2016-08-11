using System.Collections.Generic;
using System.Collections;
using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
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
	class Transform
	{
		public Vector2 position;
		public Vector2 deltaPos;

		public Transform()
		{
			position = new Vector2(0,0);
			deltaPos = new Vector2(0,0);
		}
	}
	class TransformSystem : ComponentSystem<Transform>, ISysUpdateable
	{
		public TransformSystem(State state) : base(state, "Transform")
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
				t.deltaPos = t.position;
			}
		}

		public override BaseSystem DeserializeConstructor(State state)
		{
			return new TransformSystem(state);
		}

		public override void Serialize(BinaryWriter writer)
		{
			base.Serialize(writer);
			foreach (Transform t in components)
			{
				writer.Write(t.position.X);
				writer.Write(t.position.Y);
				writer.Write(t.deltaPos.Y);
				writer.Write(t.deltaPos.Y);
			}
		}

		public override void Deserialize(BinaryReader reader)
		{
			base.Deserialize(reader);
			components = new Transform[size];
			for(int i = 0; i < size; ++i)
			{
				Transform t = new Transform();
				t.position.X = reader.ReadSingle();
				t.position.Y = reader.ReadSingle();
				t.deltaPos.X = reader.ReadSingle();
				t.deltaPos.Y = reader.ReadSingle();
				components[i] = t;
			}
		}
	}

	class MouseFollowSystem : EntitySystem, ISysUpdateable
	{
		private TransformSystem transform;
		public MouseFollowSystem(State state) : base(state, "MouseFollow")
		{
			transform = state.getSystem<TransformSystem>();
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

				float tx = 0;
				float ty = 0;

				foreach (TouchLocation tl in G.touchCollection)
				{
					tx = tl.Position.X;
					ty = tl.Position.Y;
				}

				float mx = tx + G.mouseState.X;
				float my = ty + G.mouseState.Y;

				pos.X = mx;
				pos.Y = my;
				transformC.position = pos;
			}
		}

		public override BaseSystem DeserializeConstructor(State state)
		{
			return new MouseFollowSystem(state);
		}
	}

	class Texture2
	{
		Texture2D texture;
		public String textureName;
		public float layerDepth;
		public Vector2 offset;
		public Vector2 scale;
		public Vector2 origin;
		public Rectangle textureRect;
		public Rectangle srcRect;
		public SpriteEffects effect;
		public bool useCamera = true;

		public Texture2(Global G, String textureString, float layer = 0.9f)
		{
			this.texture = G.getTexture(textureString);
			textureName = textureString;
			this.layerDepth = layer;
			this.offset = new Vector2(0, 0);

			this.scale = new Vector2(1, 1);

			origin = new Vector2(texture.Width / 2f, texture.Height / 2f);
			textureRect = new Rectangle(0, 0, texture.Width, texture.Height);
			srcRect = new Rectangle(0, 0, texture.Width, texture.Height);
			effect = SpriteEffects.None;

		}

		public void Render(SpriteBatch batch, Vector2 position, float rotation = 0)
		{
			position += offset;
			resetRect();
			batch.Draw(texture, position: position, sourceRectangle:srcRect, scale: scale, layerDepth: layerDepth, rotation: rotation, origin: origin, effects:effect);
		}

		public void setScale(float x, float y)
		{
			scale = new Vector2(x, y);
			textureRect.Width = (int) ((float) srcRect.Width * x);
			textureRect.Height = (int) ((float) srcRect.Height * y);
		}

		public void setRect(int w, int h)
		{
			scale.X = ((float) w) / textureRect.Width;
			scale.Y = ((float) h) / textureRect.Height;
		}

		public int Width
		{
			get
			{
				return texture.Width;
			}
		}

		public int Height
		{
			get
			{
				return texture.Height;
			}
		}

		public void resetRect()
		{
			origin = new Vector2(srcRect.Width / 2f, srcRect.Height / 2f);
			textureRect.Width = (int)((float)srcRect.Width * scale.X);
			textureRect.Height = (int)((float)srcRect.Height * scale.Y);
		}
	}

	

	class TextureSystem : ComponentSystem<Texture2>, ISysRenderable
	{
		TransformSystem transform;
		PhysicsSystem physics;
		
		public TextureSystem(State state) : base(state, "Texture")
		{
			_batch = new SpriteBatch(state.G.game.GraphicsDevice);
			this.transform = state.getSystem<TransformSystem>();
			this.physics = state.getSystem<PhysicsSystem>();
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

		public void Render(Global G)
		{
			_batch.Begin(sortMode: SpriteSortMode.BackToFront,samplerState: SamplerState.PointWrap, transformMatrix: _state.camera.matrix);
			for(int i = 0; i < size; ++i)
			{
				if (entityIDs[i] == -1)
					continue;

				var transfromIndex = _state.entitiesIndexes[entityIDs[i]][transform.systemIndex];
				var transformC = transform.getComponent(transfromIndex);
				var textureC = components[i];

				int pindex = -1;
				if(physics != null)
					pindex = _state.getComponentIndex(entityIDs[i], physics.systemIndex);
				if(pindex != -1)
				{
					var p = physics.getComponent(pindex);
					textureC.Render(_batch, ConvertUnits.ToDisplayUnits( p.Position),  p.Rotation);
				} else
				{
					textureC.Render(_batch, transformC.position);
				}

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

			int transfromIndex = -1;
			if (transform.ContainsEntity(id, ref transfromIndex))
			{
				var transformC = transform.getComponent(transfromIndex);
				rect.X = (int)(transformC.position.X - rect.Width/2f + 8);
				rect.Y = (int)(transformC.position.Y - rect.Height/2f + 8);
			}

			return rect;
		}

		public override BaseSystem DeserializeConstructor(State state)
		{
			return new TextureSystem(state);
		}

		override public void Serialize(BinaryWriter writer)
		{
			base.Serialize(writer);
			foreach(Texture2 t in components)
			{
				writer.Write(t.textureName);
				writer.Write(t.layerDepth);
				writer.Write(t.offset.X);
				writer.Write(t.offset.Y);
				writer.Write(t.scale.X);
				writer.Write(t.scale.Y);
				writer.Write(t.origin.X);
				writer.Write(t.origin.Y);
				writer.Write(t.textureRect.X);
				writer.Write(t.textureRect.Y);
				writer.Write(t.textureRect.Width);
				writer.Write(t.textureRect.Height);
				writer.Write(t.srcRect.X);
				writer.Write(t.srcRect.Y);
				writer.Write(t.srcRect.Width);
				writer.Write(t.srcRect.Height);
			}
		}

		override public void Deserialize(BinaryReader reader)
		{
			base.Deserialize(reader);
			components = new Texture2[size];
			for(int i = 0; i < size; ++i)
			{
				var name = reader.ReadString();
				var layer = reader.ReadInt32();
				Texture2 t = new Texture2(_state.G, name, layer);
				t.offset.X = reader.ReadSingle();
				t.offset.Y = reader.ReadSingle();
				t.scale.X = reader.ReadSingle();
				t.scale.Y = reader.ReadSingle();
				t.origin.X = reader.ReadSingle();
				t.origin.Y = reader.ReadSingle();
				t.textureRect.X = reader.ReadInt32();
				t.textureRect.Y = reader.ReadInt32();
				t.textureRect.Width = reader.ReadInt32();
				t.textureRect.Height = reader.ReadInt32();
				t.srcRect.X = reader.ReadInt32();
				t.srcRect.Y = reader.ReadInt32();
				t.srcRect.Width = reader.ReadInt32();
				t.srcRect.Height = reader.ReadInt32();
				components[i] = t;
			}
		}
	}

	class CameraFollowSystem : BaseSystem, ISysUpdateable
	{
		int followID;
		TransformSystem transSys;
		TextureSystem textureSystem;
		Rectangle rect;
		public CameraFollowSystem(State state) : base(state, "CameraFollowSystem")
		{
			transSys = state.getSystem<TransformSystem>();
			textureSystem = state.getSystem<TextureSystem>();
			rect = new Rectangle(0, 0, 100, 100);
		}

		public override BaseSystem DeserializeConstructor(State state)
		{
			return new CameraFollowSystem(state);
		}

		public void Update(Global G)
		{
			var index = _state.getComponentIndex(followID, transSys.systemIndex);
			if(index != -1)
			{
				var pos = transSys.getComponent(index).position;
				_state.camera.SetPosition(pos);
			}
		}

		public void SetEntity(int id)
		{
			followID = id;
		}
	}

	class DragAndDropSystem : EntitySystem, ISysUpdateable
	{
		bool mhold = false;
		int heldEntity = -1;
		Vector2 offset;
		TextureSystem textureSys;
		TransformSystem transfromSys;
		PhysicsSystem physics;

		public DragAndDropSystem(State state) : base(state, "DragAndDrop")
		{
			offset = new Vector2(0, 0);
			textureSys = _state.getSystem<TextureSystem>();
			transfromSys = _state.getSystem<TransformSystem>();
			physics = _state.getSystem<PhysicsSystem>();
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
			var pos = new Vector2(G.mouseState.X, G.mouseState.Y);
			_state.camera.toCameraScale(ref pos);
			float mx = pos.X;
			float my = pos.Y;
			bool leftPressed = G.mouseState.LeftButton == ButtonState.Pressed;

			foreach (TouchLocation tl in G.touchCollection)
			{
					leftPressed = true;
					mx += tl.Position.X;
					my += tl.Position.Y;
			}

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
						if (rect.Contains(mx, my))
						{
							mhold = true;
							heldEntity = e;
							offset.X = mx - rect.X - rect.Width/2f + 8;
							offset.Y = my - rect.Y - rect.Width/2f + 8;
						}
					}
				}
			}
			else if(!leftPressed && mhold)
			{
				mhold = false;
					int physicsIndex = -1;
					if (physics.ContainsEntity(heldEntity, ref physicsIndex))
					{
						var p = physics.getComponent(physicsIndex);
						if (p.IsKinematic)
						{
							var vel = new Vector2(0, 0);
							p.LinearVelocity = vel;
						}
					}
				heldEntity = -1;

			}

			if(mhold)
			{

				int physicsIndex = -1;
				if(physics.ContainsEntity(heldEntity, ref physicsIndex))
				{
					var p = physics.getComponent(physicsIndex);
					if (p.IsStatic)
						physicsIndex = -1;
					var vel = new Vector2(-(p.Position.X - ConvertUnits.ToSimUnits(mx - offset.X))* ConvertUnits.ToSimUnits(100),
						-(p.Position.Y - ConvertUnits.ToSimUnits(my - offset.Y)) * ConvertUnits.ToSimUnits(100));
					p.LinearVelocity = vel;
				}

				int transIndex = -1;
				if(physicsIndex == -1 && transfromSys.ContainsEntity(heldEntity, ref transIndex))
				{
					var trans = transfromSys.getComponent(transIndex);
					trans.position.X = mx - offset.X;
					trans.position.Y = my - offset.Y;
				}
			}

		}

		public override BaseSystem DeserializeConstructor(State state)
		{
			return new DragAndDropSystem(state);
		}
	}

	[Flags]
	enum Direction
	{
		NONE = 0x0000 , UP = 0x0001, DOWN = 0x0010, RIGHT = 0x0100, LEFT = 0x1000, ALL = 0x1111
	}
	
	class Rectanglef
	{
		private float x, y, width, height;

		public Rectanglef(float x = 0, float y = 0, float width = 0, float height = 0)
		{
			this.x = x;
			this.y = y;
			this.width = width;
			this.height = height;
		}

		public bool intersectsWith(Rectanglef r2, ref Rectanglef area)
		{
			float left = Math.Max(x, r2.x);
			float right = Math.Min(x + width, r2.x + r2.width);
			float top = Math.Max(y, r2.y);
			float bot = Math.Min(y + height, r2.y + r2.height);
			if (left < right && top < bot)
			{
				area.x = left;
				area.y = top;
				area.width = right - left;
				area.height = bot - top;
				return true;
			}
			return false;
		}

		public float X
		{
			get { return this.x; }

			set { this.x = value; }
		}

		public float Y
		{
			get { return this.y; }
			set { this.y = value; }
		}

		public float Width
		{
			get { return this.width; }
			set { this.width = value; }
		}

		public float Height
		{
			get { return this.height; }
			set { this.height = value; }
		}

	}

	class colliderComponent
	{
		public int transformIndex;
		public Vector2 size;
		public Vector2 dsize;
		public Vector2 velocity;

		public Rectanglef rect;
		public Direction isTouching;
		public Direction touchable;

		public bool movable = false;

		public colliderComponent(float w, float h)
		{
			dsize = new Vector2();
			size = new Vector2(w, h);
			velocity = new Vector2();

			rect = new Rectanglef(0,0,w,h);
			touchable = Direction.ALL;
		}

		public colliderComponent(TextureSystem textures, int id)
		{
			int index = 0;
			float w = 0;
			float h = 0;
			if(textures.ContainsEntity(id, ref index))
			{
				var com = textures.getComponent(index);
				w = com.textureRect.Width;
				h = com.textureRect.Height;
			}

			dsize = new Vector2();
			size = new Vector2(w, h);
			velocity = new Vector2();

			rect = new Rectanglef(0, 0, w, h);
			touchable = Direction.ALL;
		}
	}

	class CollisionSystem : ComponentSystem<colliderComponent>, ISysUpdateable
	{
		private TransformSystem transformSys;

		private World world;

		public CollisionSystem(State state) : base(state, "BasicCollision")
		{
			transformSys = state.getSystem<TransformSystem>();
			world = new World(Vector2.Zero);
		}
		Rectanglef a = new Rectanglef();

		public void Update(Global G)
		{
			for(int i = 0; i < size; ++i)
			{
				var id = entityIDs[i];
				if (id != -1)
					updateComponent(id, i);
			}

			for (int i = 0; i < size; ++i)
			{
				for (int j = 0; j < size; ++j)
				{
					var id1 = entityIDs[i];
					var id2 = entityIDs[j];
					if (id1 != -1 && id2 != -1 && id1 != id2 && components[i].rect.intersectsWith(components[j].rect, ref a))
						collide(ref components[i], ref components[j]);
				}
			}
		}

		private void updateComponent(int id, int index)
		{
			var coll = components[index];
			int trans_index = 0;
			if(transformSys.ContainsEntity(id, ref trans_index))
			{
				var trans = transformSys.getComponent(trans_index);
				var delta = trans.position - trans.deltaPos;

				coll.rect.X =  (trans.position.X - (delta.X > 0 ? delta.X : 0));
				coll.rect.Y =  (trans.position.Y - (delta.Y > 0 ? delta.Y : 0));

				coll.rect.Width =  (coll.size.X + (delta.X > 0 ? delta.X : -delta.X));
				coll.rect.Height =  (coll.size.Y + (delta.Y > 0 ? delta.Y : -delta.Y));

				
			}

		}
		private bool collide(ref colliderComponent c1, ref colliderComponent c2)
		{

			var t1 = transformSys.getComponent(c1.transformIndex);
			var t2 = transformSys.getComponent(c2.transformIndex);

			var dir = (t1.position - t1.deltaPos) +  (t2.position - t2.deltaPos);
			if (a.Width <= a.Height)
			{
				float change = 0;
				bool c = true;
				if (dir.X > 0 && t1.deltaPos.X + c1.size.X <= c2.rect.X + 1)
				{
					if ((c1.touchable & Direction.RIGHT) == Direction.RIGHT
						&& (c2.touchable & Direction.LEFT) == Direction.LEFT)
					{
						c1.isTouching |= Direction.LEFT;
						c2.isTouching |= Direction.RIGHT;
						change = c1.size.X;
					}
					else
					{
						change = a.X - t1.position.X;
						c = false;
					}
				}
				else if (dir.X < 0 && t1.deltaPos.X >= c2.rect.X + c2.rect.Width - 1)
				{
					if ((c1.touchable & Direction.LEFT) == Direction.LEFT
						&& (c2.touchable & Direction.RIGHT) == Direction.RIGHT)
					{
						c1.isTouching |= Direction.RIGHT;
						c2.isTouching |= Direction.LEFT;
						a.Width *= -1;
						change = a.Width;
					}
					else
					{
						change = a.X - t1.position.X;
						c = false;
					}
				}
				else
				{
					c = false;
				}

				if (!c2.movable && c)
				{
					t1.position.X = a.X - change;
				} else if(!c1.movable && c)
				{
					t2.position.X = t2.position.X + a.Width;
				} else if(c)
				{
					t1.position.X = a.X - change;
					t2.position.X = t2.position.X + a.Width;
				}
			}
			else
			{
				float change = 0;
				bool c = true;
				if (dir.Y > 0 && t1.deltaPos.Y + c1.size.Y <= c2.rect.Y + 1)
				{
					if ((c1.touchable & Direction.UP) == Direction.UP
						&& (c2.touchable & Direction.DOWN) == Direction.DOWN)
					{
						c1.isTouching |= Direction.DOWN;
						c2.isTouching |= Direction.UP;
						change = c1.size.Y;
					}
					else
					{
						change = a.Y - t1.position.Y;
						c = false;
					}
				}
				else if (dir.Y < 0 && t1.deltaPos.Y >= c2.rect.Y + c2.rect.Height - 1)
				{
					if ((c1.touchable & Direction.DOWN) == Direction.DOWN
						&& (c2.touchable & Direction.UP) == Direction.UP)
					{
						c1.isTouching |= Direction.UP;
						c2.isTouching |= Direction.DOWN;
						a.Height *= -1;
						change = a.Height;
					}
					else
					{
						change = a.Y - t1.position.Y;
						c = false;
					}
				}
				else
				{
					c = false;
				}

				if (!c2.movable && c)
				{
					t1.position.Y = a.Y - change;
				}
				else if (!c1.movable && c)
				{
					t2.position.Y = t2.position.Y + a.Height;
				}
			}
			return true;
		}

		public override BaseSystem DeserializeConstructor(State state)
		{
			return new CollisionSystem(state);
		}
	}
}


namespace Entities
{
	using CS.Components;
	class Image
	{
		public int id;
		public int textureIndex;
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
			textureIndex = textureSys.AddComponent(id, texture);
		}
	}
	
}
