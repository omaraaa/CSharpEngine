using System.Collections.Generic;
using System.IO;
using System.Collections;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;

using CS.Util;

namespace CS
{
	[Serializable()]
	class Entity : ISerializable
	{
		private State _state;
		private int _id;
		//private Dictionary<KeyValuePair<Type, String>, object> components;

		public Entity(State state, int id)
		{
			_state = state;
			_id = id;
			//components = new Dictionary<KeyValuePair<Type, string>, object>();
		}

		public Entity(SerializationInfo info, StreamingContext context)
		{

		}

		public State State
		{
			get { return _state; }
		}

		public int ID
		{
			get { return _id; }
		}


		public Entity AddComponent<T>(ref T c, String s)
		{
			var key = new KeyValuePair<Type, String>(typeof(T), s);
			//components[key] = c;
			//_state.AddComponent(c);
			return this;
		}

		public bool HasComponent<T>(String s)
		{
			var key = new KeyValuePair<Type, String>(typeof(T), s);
			if (false)//components.ContainsKey(key))
			{
				return true;
			}

			return false;
		}

		public T GetComponent<T>(String s)
		{
			var key = new KeyValuePair<Type, String>(typeof(T), s);
			//if (this.HasComponent<T>(s))
				//return (T)components[key];
			//else
				throw new Exception("Component " + s + " of type " + typeof(T).Name + " does not exist.\n");
		}

		public void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			throw new NotImplementedException();
		}
	}

	

	delegate void CFunc(ref Global G);

	[Serializable()]
	class StaticComponent : ISerializable
	{
		protected Entity e;
		public int index;
		public uint layer;
		//abstract public void render(ref Global G);

		public StaticComponent(Entity entitiy, params Type[] types)
		{
			e = entitiy;
			layer = 1;
		}

		public StaticComponent(SerializationInfo info, StreamingContext context)
		{
			//e = 
		}

		public void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			//info.AddValue("entity", e.ID);
			info.AddValue("layer", layer);
		}
	}

	abstract class UpdatableComponent : StaticComponent
	{
		abstract public void Update(ref Global G);
		protected bool sleep = false;
		public int sleepIndex = -1;

		public UpdatableComponent(Entity entity, params Type[] types) : base(entity,  types)
		{
			index = entity.State.AddComponent(this);
		}

		public void setSleeping(bool isSleeping)
		{
			var state = e.State;
			if (isSleeping)
			{
				sleepIndex = state.SetToSleep(this);
				sleep = true;
				//Console.Write("sleeping");
			} else
			{
				state.SetToActive(this);
				sleep = false;
			}
		}

		public bool Sleeping
		{
			get
			{
				return sleep;
			}
		}
	}

	abstract class RenderableComponent : UpdatableComponent
	{
		abstract public void Render(SpriteBatch batch);

		public RenderableComponent(Entity entity, params Type[] types) : base(entity, types)
		{
			entity.State.AddComponent(this);
		}
	}

	abstract class BaseSystem
	{
		public State _state;
		public uint systemIndex;

		public BaseSystem(State state)
		{
			_state = state;
			systemIndex = state.RegisterSystem(this);
		}
	}

	interface ISysUpdateable
	{
		uint UpdateIndex
		{
			get;
		}
		void Update(Global G);
	}

	interface ISysRenderable
	{
		uint RenderIndex
		{
			get;
		}
		SpriteBatch Batch
		{
			get;
		}
		void Render();
	}

	/*
	 * System that manages the storage and update of differening components
	 */
	abstract class ComponentSystem<T> : BaseSystem
	{
		protected T[] components;
		protected uint[] entityIDs;
		protected int size;
		protected uint index;

		protected uint cachedComponent;

		public ComponentSystem(State state) : base(state)
		{
			components = new T[0];
			entityIDs = new uint[0];
			size = 0;
			index = 0;
			cachedComponent = 0;
		}

		public void AddComponent(uint id, T component)
		{
			size++;
			Array.Resize(ref components, size);
			Array.Resize(ref entityIDs, size);

			components[size - 1] = component;
			entityIDs[size - 1] = id;

			_state.AddComponent(id, systemIndex, size - 1);
		}

		public T getComponent(int index)
		{
			return components[index];
		}
	}

	[Serializable()]
	class State
	{
		private UpdatableComponent[] components;
		private UpdatableComponent[] sleepingComponents;
		private RenderableComponent[] rendercomponents;

		private BaseSystem[] systems;
		private uint[] updatableIndexes;
		private uint[] renderableIndexes;

		public int[][] entitiesIndexes;


		private Entity[] entities;
		public Global G;

		public State(Global G)
		{
			components = new UpdatableComponent[0];
			rendercomponents = new RenderableComponent[0];
			sleepingComponents = new UpdatableComponent[0];
			systems = new BaseSystem[0];
			updatableIndexes = new uint[0];
			renderableIndexes = new uint[0];
			entitiesIndexes = new int[0][];
			entities = new Entity[0];
			this.G = G;
		}

		private void UpdateComponent(UpdatableComponent c)
		{
			c.Update(ref G);
		}

		public uint RegisterSystem(BaseSystem system)
		{
			var size = systems.Length;
			size++;
			Array.Resize(ref systems, size + 1);

			systems[size - 1] = system;

			if(system is ISysUpdateable)
			{
				var usize = updatableIndexes.Length;
				usize++;
				Array.Resize(ref updatableIndexes, usize);
				updatableIndexes[usize - 1] = (uint) size - 1;
			}

			if (system is ISysRenderable)
			{
				var usize = renderableIndexes.Length;
				usize++;
				Array.Resize(ref renderableIndexes, usize);
				renderableIndexes[usize - 1] = (uint)size - 1;
			}

			return (uint) size - 1;
		}

		public void Update()
		{
			foreach(UpdatableComponent c in components)
			{
				if (c == null)
					continue;
				UpdateComponent(c);
			}

			foreach (RenderableComponent c in rendercomponents)
			{
					UpdateComponent(c);
			}

			foreach (uint index in updatableIndexes)
			{
				var sys = systems[index] as ISysUpdateable;
				sys.Update(G);
			}
		}

		public void Render(SpriteBatch batch)
		{
			foreach (RenderableComponent c in rendercomponents)
			{
					c.Render(batch);
			}

			foreach (uint index in renderableIndexes)
			{
				var sys = systems[index] as ISysRenderable;
				sys.Render();
			}
		}

		public Entity CreateEntity()
		{
			Array.Resize(ref entities, entities.Length+1);
			Array.Resize(ref entitiesIndexes, entities.Length);
			entitiesIndexes[entities.Length - 1] = new int[systems.Length];
			for(int i = 0; i < systems.Length; ++i)
			{
				entitiesIndexes[entities.Length - 1][i] = -1;
			}
			return entities[entities.Length-1] = new Entity(this, entities.Length-1);
		}

		public int AddComponent(UpdatableComponent c)
		{
			Array.Resize(ref components, components.Length + 1);
			components[components.Length - 1] = c;
			return (components.Length - 1);
		}
		public int AddComponent(RenderableComponent c)
		{
			Array.Resize(ref rendercomponents, rendercomponents.Length + 1);
			rendercomponents[rendercomponents.Length - 1] = c;
			return (rendercomponents.Length - 1);
		}
		public int SetToSleep(UpdatableComponent c)
		{
			components[c.index] = null;
			var len = sleepingComponents.Length;
			Array.Resize(ref sleepingComponents, len + 1);
			sleepingComponents[len] = c;
			return (len);
		}
		public void SetToActive(UpdatableComponent c)
		{
			components[c.index] = c;
			sleepingComponents[c.sleepIndex] = null;
		}

		public void AddComponent(uint entityID, uint SystemID, int index)
		{
			entitiesIndexes[entityID][SystemID] = index;
		}
	}

	delegate void FunctionDelegate(ref State state, uint id);

	



	[Serializable()]
	class Global
	{
		private Dictionary<String, FunctionDelegate> functions;
		private Dictionary<String, Texture2D> textures;
		private State[] activeStates;
		public Game game;
		public GameTime gametime;
		public MouseState mouseState;

		public Global(Game g)
		{
			game = g;
			textures = new Dictionary<String, Texture2D>();
		}

		public void Update(GameTime gametime)
		{
			this.gametime = gametime;

			mouseState = Mouse.GetState();

			foreach (State s in activeStates)
			{
				s.Update();
			}
		}

		public void Render(SpriteBatch batch)
		{
			foreach (State s in activeStates)
			{
				s.Render(batch);
			}
		}

		public void ActivateState(State s)
		{
			if (activeStates != null)
				Array.Resize(ref activeStates, activeStates.Length + 1);
			else
				activeStates = new State[1];
			activeStates[activeStates.Length-1] = s;
		}

		public Texture2D getTexture(String name)
		{
			if (textures.ContainsKey(name))
				return textures[name];
			else
			{
				return textures[name] = game.Content.Load<Texture2D>(name);
			}
		}
	}
}

/*
G.registerComponent<Position>();
G.registerComponent<PositonPhysics>(typeof(Position));
G.registerComponent<Collider>(typeof(Position));

var entity = state.createEntitiy();
var t = Transform.add(entity, 0, 0);
var s = Sprite.add(entity, G.texture("player"));
var c = Collider.add(entity, s.getLocalRect());
*/