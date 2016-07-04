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
	abstract class BaseSystem
	{
		public State _state;
		public uint systemIndex;
		protected int[] entityIDs;
		protected int size;


		public BaseSystem(State state)
		{
			_state = state;
			systemIndex = state.RegisterSystem(this);
			entityIDs = new int[0];
			size = 0;
		}

		public void AddEntity(int id)
		{
			size++;
			Array.Resize(ref entityIDs, size);
			entityIDs[size - 1] = id;
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
		protected uint index;

		protected uint cachedComponent;

		public ComponentSystem(State state) : base(state)
		{
			components = new T[0];
			size = 0;
			index = 0;
			cachedComponent = 0;
		}

		public void AddComponent(int id, T component)
		{
			AddEntity(id);
			Array.Resize(ref components, size);
			components[size - 1] = component;

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


		public void AddComponent(int entityID, uint SystemID, int index)
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
		public float dt;
		public MouseState mouseState;

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