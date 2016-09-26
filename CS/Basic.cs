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

using Lidgren.Network;

using MoonSharp.Interpreter;
using MG;

namespace CS.Components
{
	[MoonSharpUserData]
	struct Transform
	{
		public Vector2 position;
		public Vector2 deltaPos;

		public void SetPosition(Vector2 pos)
		{
			position = pos;
		}
	}
	class TransformSystem : ComponentSystem<Transform>, ISysUpdateable
	{
		public TransformSystem(State state) : base(state, "TransformSystem")
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
			for(int i = 0; i < size; ++i)
			{
				var t = components[i];
				t.deltaPos = t.position;
				components[i] = t;
			}
		}

		public List<int> GetUpdated()
		{
			List<int> entities = new List<int>();
			for(int i = 0; i < size; ++i)
			{
				if (entityIDs[i] == -1)
					continue;

				if (components[i].position != components[i].deltaPos)
					entities.Add(entityIDs[i]);
			}
			return entities;
		}

		public override BaseSystem DeserializeConstructor(State state, string name)
		{
			return new TransformSystem(state);
		}

		protected override void SerailizeComponent(ref Transform component, BinaryWriter writer)
		{
			writer.Write(component.position.X);
			writer.Write(component.position.Y);
			writer.Write(component.deltaPos.Y);
			writer.Write(component.deltaPos.Y);
		}

		protected override Transform DeserailizeComponent(BinaryReader reader)
		{
			Transform t = new Transform();
			t.position.X = reader.ReadSingle();
			t.position.Y = reader.ReadSingle();
			t.deltaPos.X = reader.ReadSingle();
			t.deltaPos.Y = reader.ReadSingle();
			return t;
		}

		public override void SerializeSystem(BinaryWriter writer)
		{
		}

		public override void DeserializeSystem(BinaryReader reader)
		{
		}
	}

	class MouseFollowSystem : EntitySystem, ISysUpdateable
	{
		private TransformSystem transform;
		private MonogameSystem monogameSys;
		private Camera camera;
		public MouseFollowSystem(State state) : base(state, "MouseFollow")
		{
			transform = state.getSystem<TransformSystem>();
			monogameSys = state.G.getSystem<MonogameSystem>();
			camera = state.getSystem<Camera>();
		}


		public override void Initialize()
		{
			base.Initialize();
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

				var transfromIndex = _state.getComponentIndex(entity, Index);
				var transformC = transform.getComponent(transfromIndex);
				var pos = transformC.position;

				float tx = 0;
				float ty = 0;

				foreach (TouchLocation tl in monogameSys.touchCollection)
				{
					tx = tl.Position.X;
					ty = tl.Position.Y;
				}

				float mx = tx + monogameSys.mouseState.X;
				float my = ty + monogameSys.mouseState.Y;

				pos.X = mx;
				pos.Y = my;
				camera.toCameraScale(ref pos);
				transformC.position = pos;
				transform.SetComponent(entity, transformC);
			}
		}

		public override BaseSystem DeserializeConstructor(State state, string name)
		{
			return new MouseFollowSystem(state);
		}

		public override void SerializeSystem(BinaryWriter writer)
		{
		}

		public override void DeserializeSystem(BinaryReader reader)
		{
		}
	}

	

	class CameraFollowSystem : BaseSystem, ISysUpdateable
	{
		int followID = -1;
		TransformSystem transSys;
		RenderSystem renderSys;
		Rectangle rect;

		Camera camera;

		public CameraFollowSystem(State state) : base(state, "CameraFollowSystem")
		{
			transSys = state.getSystem<TransformSystem>();
			renderSys = state.getSystem<RenderSystem>();
			camera = state.getSystem<Camera>();
			rect = new Rectangle(0, 0, 100, 100);
		}

		public override BaseSystem DeserializeConstructor(State state, string name)
		{
			return new CameraFollowSystem(state);
		}

		public void Update(Global G)
		{
			if (followID != -1)
			{
				var index = _state.getComponentIndex(followID, transSys.Index);
				if (index != -1)
				{
					var trans = transSys.getComponent(index);
					var dist = Vector2.Distance(trans.position, trans.deltaPos)/ _state.G.dt;
					var pos = trans.position;
					pos = Vector2.SmoothStep(camera.position, pos, 1 - (float)Math.Exp(-10 * _state.G.dt));
					camera.SetPosition(pos);
				}
			}
		}

		public void SetEntity(int id)
		{
			followID = id;
		}

		public override void Serialize(BinaryWriter writer)
		{
			base.Serialize(writer);
			writer.Write(followID);
		}

		public override void Deserialize(BinaryReader reader)
		{
			base.Deserialize(reader);
			followID = reader.ReadInt32();
		}

		public override void SerializeSystem(BinaryWriter writer)
		{
		}

		public override void DeserializeSystem(BinaryReader reader)
		{
		}
	}

	class DragAndDropSystem : EntitySystem, ISysUpdateable
	{
		bool mhold = false;
		int heldEntity = -1;
		Vector2 offset;
		RenderSystem renderSys;
		TransformSystem transfromSys;
		PhysicsSystem physics;
		Camera camera;
		MonogameSystem monogameSys;

		public DragAndDropSystem(State state) : base(state, "DragAndDrop")
		{
			offset = new Vector2(0, 0);
			renderSys = _state.getSystem<RenderSystem>();
			transfromSys = _state.getSystem<TransformSystem>();
			physics = _state.getSystem<PhysicsSystem>();
			camera = state.getSystem<Camera>();
			monogameSys = _state.G.getSystem<MonogameSystem>();
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
			var pos = new Vector2(monogameSys.mouseState.X, monogameSys.mouseState.Y);
			camera.toCameraScale(ref pos);
			float mx = pos.X;
			float my = pos.Y;
			bool leftPressed = monogameSys.mouseState.LeftButton == ButtonState.Pressed;

			foreach (TouchLocation tl in monogameSys.touchCollection)
			{
				var pos2 = new Vector2(tl.Position.X, tl.Position.Y);
				camera.toCameraScale(ref pos2);
				leftPressed = true;
				mx = pos2.X;
				my = pos2.Y;
			}

			if (leftPressed && !mhold)
			{
				foreach (var e in entityIDs)
				{
					if (e == -1)
						continue;

					var tIndex = _state.getComponentIndex(e, Index);
					if (tIndex != -1)
					{
						var rect = renderSys.getComponentByID(e).Bounds;
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

		public override BaseSystem DeserializeConstructor(State state, string name)
		{
			return new DragAndDropSystem(state);
		}

		public override void SerializeSystem(BinaryWriter writer)
		{
		}

		public override void DeserializeSystem(BinaryReader reader)
		{
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
	/*
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

		public override BaseSystem DeserializeConstructor(State state, string name)
		{
			return new CollisionSystem(state);
		}

		protected override void SerailizeComponent(ref colliderComponent component, BinaryWriter writer)
		{
			throw new NotImplementedException();
		}

		protected override colliderComponent DeserailizeComponent(BinaryReader reader)
		{
			throw new NotImplementedException();
		}

		public override void SerializeSystem(BinaryWriter writer)
		{
			throw new NotImplementedException();
		}

		public override void DeserializeSystem(BinaryReader reader)
		{
			throw new NotImplementedException();
		}
	}*/

	delegate void CallBack(State state, int id);
	class Timer
	{
		public float target;
		public float count;
		public bool repeat;
		public bool active;
		public List<string> callbackNames;
		public List<CallBack> callbacks;

		public Timer(float target, bool repeat, params string[] callbackNames)
		{
			this.target = target;
			count = 0;
			this.repeat = repeat;
			this.callbackNames = new List<string>();
			callbacks = new List<CallBack>();
			foreach (var callbackName in callbackNames)
			{
				this.callbackNames.Add(callbackName);
				this.callbacks.Add(TimerSystem.callbacks[callbackName]);
			}
			active = true;
		}
	}

	

	class TimerSystem : ComponentSystem<Timer[]>, ISysUpdateable
	{
		static public Dictionary<string, CallBack> callbacks = new Dictionary<string, CallBack>();

		public TimerSystem(State state) : base(state, "TimerSystem")
		{
			callbacks["KillEntity"] = KillEntity;
		}

		public override BaseSystem DeserializeConstructor(State state, string name)
		{
			return new TimerSystem(state);
		}

		public void Update(Global G)
		{
			for(int i = 0; i < size; ++i)
			{
				if (entityIDs[i] == -1)
					continue;

				var arr = components[i];
				for(int j = 0; j < arr.Length; ++j)
				{
					var t = arr[j];

					if (!t.active)
						continue;

					t.count += G.dt;
					if(t.count >= t.target)
					{
						foreach(var callback in t.callbacks)
							callback(_state, entityIDs[i]);
						if(!t.repeat)
						{
							t.active = false;
						}
						t.count = 0;
					}
				}
			}
		}
		
		public void AddTimer(int id, Timer timer)
		{
			int index = _state.getComponentIndex(id, Index);
			if(index == -1)
			{
				index = AddComponent(id, new Timer[1]);
				components[index][0] = timer;
			} else
			{
				var arr = components[index];
				Array.Resize(ref arr, arr.Length + 1);
				arr[arr.Length - 1] = timer;
			}
		}

		public void AddKillTimer(int id, float time)
		{
			Timer t = new Timer(time, false, "KillEntity");
			AddTimer(id, t);
		}

		static public void KillEntity(State state, int id)
		{
			state.RemoveEntity(id);
		}

		protected override void SerailizeComponent(ref Timer[] component, BinaryWriter writer)
		{
			writer.Write(component.Length);
			foreach(Timer t in component)
			{
				writer.Write(t.active);
				writer.Write(t.callbackNames.Count);
				foreach(var name in t.callbackNames)
				writer.Write(name);
				writer.Write(t.count);
				writer.Write(t.repeat);
				writer.Write(t.target);
			}
		}

		protected override Timer[] DeserailizeComponent(BinaryReader reader)
		{
			var length = reader.ReadInt32();
			Timer[] timers = new Timer[length];
			for(int i = 0; i < length; ++i)
			{
				var active = reader.ReadBoolean();
				var callbackNamescount = reader.ReadInt32();
				List<String> callbackNames = new List<string>();
				for (int j = 0; j < callbackNamescount; ++j)
				{
					var callbackString = reader.ReadString();
					callbackNames.Add(callbackString);
				}
				var count = reader.ReadSingle();
				var repeat = reader.ReadBoolean();
				var target = reader.ReadSingle();

				timers[i] = new Timer(target, repeat, callbackNames.ToArray());
				timers[i].active = active;
				timers[i].count = count;
			}

			return timers;
		}

		public override void SerializeSystem(BinaryWriter writer)
		{
		}

		public override void DeserializeSystem(BinaryReader reader)
		{
		}
	}

	class RegistrySystem<T> : BaseSystem
	{
		Dictionary<string, T> registry;
		public RegistrySystem(State state, string name) : base(state, name)
		{
			registry = new Dictionary<string, T>();
		}

		public override BaseSystem DeserializeConstructor(State state, string name)
		{
			return new RegistrySystem<T>(state, name);
		}

		public override void DeserializeSystem(BinaryReader reader)
		{
		}

		public override void SerializeSystem(BinaryWriter writer)
		{
		}

		public void Register(string name, T obj)
		{
			registry[name] = obj;
		}

		public T Get(string name)
		{
			return registry[name];
		}

		public bool Has(string name)
		{
			return registry.ContainsKey(name);
		}
	}

	class GridSystem : BaseSystem, ISysUpdateable
	{
		int[,] grid;
		int width;
		int height;
		int tilewidth;
		int tileheight;

		public GridSystem(State state, string name) : base(state, name)
		{
		}

		public override BaseSystem DeserializeConstructor(State state, string name)
		{
			throw new NotImplementedException();
		}

		public override void DeserializeSystem(BinaryReader reader)
		{
			throw new NotImplementedException();
		}

		public override void SerializeSystem(BinaryWriter writer)
		{
			throw new NotImplementedException();
		}

		public void Update(Global G)
		{
			throw new NotImplementedException();
		}

		public void SetGrid(int w, int h, int tw, int th)
		{
			width = w;
			height = h;
			tilewidth = tw;
			tileheight = th;

			grid = new int[w,h];
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

		public Image(State state, String image, Vector2 position, Vector2 offset, float layer = 0.9f, bool topright = false)
		{
			var transformSys = state.getSystem<TransformSystem>();
			var textureSys = state.getSystem<RenderSystem>();

			id = state.CreateEntity();

			Transform transfom = new Transform();
			transfom.position = position;
			transformSys.AddComponent(id, transfom);

			Texture2 texture = new Texture2(state.G, image, layer);
			texture.offset = offset;

			if(topright)
			{
				texture.offset += new Vector2(texture.textureRect.Width / 2, texture.textureRect.Height / 2);
			}

			textureIndex = textureSys.AddComponent(id, texture);
		}
	}
	
}
