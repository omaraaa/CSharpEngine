using System.Collections.Generic;
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using Microsoft.Xna.Framework.Graphics;

namespace CS
{
	abstract class BaseSystem
	{
		public State _state;
		public uint systemIndex;
		protected int[] entityIDs;
		protected int[] freeIndexes;
		protected int size;

		public int updateIndex = -1;
		public int renderIndex = -1;

		public BaseSystem(State state)
		{
			_state = state;
			systemIndex = state.RegisterSystem(this);
			entityIDs = new int[0];
			freeIndexes = new int[0];
			size = 0;
		}

		public int AddEntity(int id)
		{
			var cindex = _state.getComponentIndex(id, systemIndex);
			if (cindex != -1)
				return cindex;
			if (freeIndexes.Length == 0)
			{
				size++;
				Array.Resize(ref entityIDs, size);
				entityIDs[size - 1] = id;

				_state.AddComponent(id, systemIndex, size - 1);
				return size - 1;
			} else
			{
				var index = freeIndexes[freeIndexes.Length - 1];

				entityIDs[index] = id;
				_state.AddComponent(id, systemIndex, index);

				Array.Resize(ref freeIndexes, freeIndexes.Length - 1);

				return index;
			}
		}

		public void RemoveEntity(int id)
		{
			var index = _state.getComponentIndex(id, systemIndex);
			if (index == -1)
				return;

			entityIDs[index] = -1;

			Array.Resize(ref freeIndexes, freeIndexes.Length + 1);
			freeIndexes[freeIndexes.Length - 1] = index;
			_state.RemoveComponent(id, systemIndex);
		}

		public bool ContainsEntity(int id, ref int index)
		{
			var indx = _state.getComponentIndex(id, systemIndex);
			index = indx;
			if (indx != -1)
				return true;
			else
				return false;
		}

	}

	interface ISysUpdateable
	{
		void Update(Global G);
	}

	interface ISysRenderable
	{
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
		protected uint index;

		protected uint cachedComponent;

		public ComponentSystem(State state) : base(state)
		{
			components = new T[0];
			size = 0;
			index = 0;
			cachedComponent = 0;
		}

		public int AddComponent(int id, T component)
		{
			var index = AddEntity(id);
			if(index+1 >= size)
				Array.Resize(ref components, size);
			components[index] = component;
			return index;
		}

		public T getComponent(int index)
		{
			return components[index];
		}
	}

	[Serializable()]
	class State
	{
		private BaseSystem[] systems;
		private uint[] updatableIndexes;
		private uint[] renderableIndexes;

		public int[][] entitiesIndexes;

		public Global G;

		public State(Global G)
		{
			systems = new BaseSystem[0];
			updatableIndexes = new uint[0];
			renderableIndexes = new uint[0];
			entitiesIndexes = new int[0][];
			this.G = G;
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
				system.updateIndex = usize - 1;
			}

			if (system is ISysRenderable)
			{
				var usize = renderableIndexes.Length;
				usize++;
				Array.Resize(ref renderableIndexes, usize);
				renderableIndexes[usize - 1] = (uint)size - 1;
				system.renderIndex = usize - 1;
			}

			return (uint) size - 1;
		}

		public void Update()
		{
			foreach (uint index in updatableIndexes)
			{
				var sys = systems[index] as ISysUpdateable;
				sys.Update(G);
			}
		}

		public void Render(SpriteBatch batch)
		{
			foreach (uint index in renderableIndexes)
			{
				var sys = systems[index] as ISysRenderable;
				sys.Render();
			}
		}

		public int CreateEntity()
		{
			Array.Resize(ref entitiesIndexes, entitiesIndexes.Length+1);
			entitiesIndexes[entitiesIndexes.Length - 1] = new int[systems.Length];
			for(int i = 0; i < systems.Length; ++i)
			{
				entitiesIndexes[entitiesIndexes.Length - 1][i] = -1;
			}
			return entitiesIndexes.Length - 1;
		}


		public void AddComponent(int entityID, uint systemID, int index)
		{
			entitiesIndexes[entityID][systemID] = index;
		}

		public void RemoveComponent(int entityID, uint systemID)
		{
			entitiesIndexes[entityID][systemID] = -1;
		}

		public int getComponentIndex(int entityID, uint systemID)
		{
			return entitiesIndexes[entityID][systemID];
		}

		public T getSystem<T>()  where T : BaseSystem
		{
			foreach (BaseSystem system in systems)
			{
				if (system is T)
					return system as T;
			}

			return null;
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
		public float dt;
		public MouseState mouseState;
		public TouchCollection touchCollection;
		public KeyboardState keyboardState;

		public Global(Game g)
		{
			game = g;
			textures = new Dictionary<String, Texture2D>();
		}

		public void Update(GameTime gametime)
		{
			this.gametime = gametime;
			dt = (float) gametime.ElapsedGameTime.TotalSeconds;

			mouseState = Mouse.GetState();
			touchCollection = TouchPanel.GetState();
			keyboardState = Keyboard.GetState();

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