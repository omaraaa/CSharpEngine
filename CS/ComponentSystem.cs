using System.Collections.Generic;
using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Utilities;

namespace CS
{

	abstract class BaseSystem
	{
		
		public State _state;

		public uint systemIndex;
		public int updateIndex = -1;
		public int renderIndex = -1;
		public String name;

		public BaseSystem(State state, String name)
		{
			_state = state;
			systemIndex = state.RegisterSystem(this);
			this.name = name;
			state.G.RegisterSystemSerialization(name, DeserializeConstructor);
		}

		virtual public void Serialize(BinaryWriter writer)
		{

		}

		virtual public void Deserialize(BinaryReader reader)
		{

		}

		abstract public BaseSystem DeserializeConstructor(State state);
	}

	abstract class EntitySystem : BaseSystem
	{
		protected int[] entityIDs;
		protected int[] freeIndexes;
		protected int size;

		public EntitySystem(State state, string name) : base(state, name)
		{
			
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
			}
			else
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

		override public void Serialize(BinaryWriter writer)
		{
			base.Serialize(writer);
			writer.Write(size);
			foreach (var e in entityIDs)
			{
				writer.Write(e);
			}
		}

		override public void Deserialize(BinaryReader reader)
		{
			base.Deserialize(reader);
			size = reader.ReadInt32();
			entityIDs = new int[size];
			for (int i = 0; i < size; ++i)
				entityIDs[i] = reader.ReadInt32();
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
		void Render(Global G);
	}
	/*
	 * System that manages the storage and update of differening components
	 */
	abstract class ComponentSystem<T> : EntitySystem
	{
		protected T[] components;
		protected uint index;

		protected uint cachedComponent;

		public ComponentSystem(State state, String name) : base(state, name)
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

	class Camera
	{
		public Matrix matrix
		{
			get
			{
				return
				Matrix.CreateTranslation(new Vector3(-position, 0))
				* Matrix.CreateScale(new Vector3(scale, 0))
				* Matrix.CreateTranslation(new Vector3(rect.Width / 2f, rect.Height / 2f, 0))
				;
			}
		}

		Rectangle rect;
		public Vector2 position;
		public Vector2 scale;
		public Vector2 center;
		public float lerpValue = 0.5f;
		private State _state;

		public Camera(State state)
		{
			var width = state.G.game.GraphicsDevice.Viewport.Width;
			var height = state.G.game.GraphicsDevice.Viewport.Height;
			rect = new Rectangle(0, 0, width, height);
			position = new Vector2(rect.Width/2f, rect.Height / 2f);
			center = position;
			setScale(new Vector2(1, 1));
			_state = state;
		}

		public void SetPosition(Vector2 pos)
		{
			position = pos;
		}

		public void setScale(Vector2 scale)
		{
			this.scale = scale;
		}

		public void toCameraScale(ref Vector2 v)
		{
			var scale = new Vector2(matrix.Scale.X, matrix.Scale.Y);
			var pos = new Vector2(matrix.Translation.X, matrix.Translation.Y);
			v = v / scale - pos / scale;
		}

		public void toCamera(ref Vector2 v)
		{
			var scale = new Vector2(matrix.Scale.X, matrix.Scale.Y);
			var pos = new Vector2(matrix.Translation.X, matrix.Translation.Y);
			v = v - pos;
		}
	}

	class State
	{
		private BaseSystem[] systems;
		private uint[] updatableIndexes;
		private uint[] renderableIndexes;

		public int[][] entitiesIndexes;

		public Global G;
		public Camera camera;
		public int index;

		public State(Global G)
		{
			systems = new BaseSystem[0];
			updatableIndexes = new uint[0];
			renderableIndexes = new uint[0];
			entitiesIndexes = new int[0][];
			this.G = G;
			camera = new Camera(this);
		}

		public uint RegisterSystem(BaseSystem system)
		{
			var size = systems.Length;
			size++;
			Array.Resize(ref systems, size);

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
			for (int i = renderableIndexes.Length-1; i >=0; --i)
			{
				var index = renderableIndexes[i];
				var sys = systems[index] as ISysRenderable;
				sys.Render(G);
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

		protected State(SerializationInfo info, StreamingContext context)
		{
			renderableIndexes = (uint[]) info.GetValue("renderIndexes", typeof(uint[]));
		}

		public void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue("renderIndexes", renderableIndexes);

		}

		public void Serialize(BinaryWriter writer)
		{

			writer.Write(entitiesIndexes.Length);
			for(int i = 0; i < entitiesIndexes.Length; ++i)
			{
				writer.Write(entitiesIndexes[i].Length);
				foreach (int index in entitiesIndexes[i])
					writer.Write(index);
			}

			writer.Write(systems.Length);
			for(int i = 0; i < systems.Length; ++i)
			{
				writer.Write(systems[i].name);
				systems[i].Serialize(writer);
			}
		}

		public void Deserialize(BinaryReader reader)
		{

			int entitiesSize = reader.ReadInt32();
			entitiesIndexes = new int[entitiesSize][];
			for (int i = 0; i < entitiesIndexes.Length; ++i)
			{
				int s = reader.ReadInt32();
				entitiesIndexes[i] = new int[s];
				for (int j = 0; j < s; ++j)
					entitiesIndexes[i][j] = reader.ReadInt32();
			}

			int size = reader.ReadInt32();
			for (int i = 0; i < size; ++i)
			{
				String name = reader.ReadString();
				bool sameName = false;
				if (systems.Length > i && systems[i] != null && systems[i].name == name)
					sameName = true;
				if (G.systemsConstructors.ContainsKey(name))
				{
					if(!sameName)
						G.systemsConstructors[name](this);
					systems[i].Deserialize(reader);
				}
			}
		}
	}

	delegate void FunctionDelegate(ref State state, uint id);

	delegate BaseSystem DeserializationConstructor(State state);


	[Serializable()]
	class Global
	{
		private Dictionary<String, Texture2D> textures;
		private State[] activeStates;
		public Game game;
		public GameTime gametime;
		public float dt;
		public MouseState mouseState;
		public TouchCollection touchCollection;
		public KeyboardState keyboardState;
		public Dictionary<String, DeserializationConstructor> systemsConstructors;

		public Global(Game g)
		{
			game = g;
			textures = new Dictionary<String, Texture2D>();
			activeStates = new State[0];
			systemsConstructors = new Dictionary<String, DeserializationConstructor>();
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
			Array.Resize(ref activeStates, activeStates.Length + 1);

			activeStates[activeStates.Length-1] = s;
			s.index = activeStates.Length - 1;
		}

		private void loadTexture(String name)
		{
			textures[name] = game.Content.Load<Texture2D>(name);
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

		public void Serialize(FileStream fs)
		{
			fs.Position = 0;
			BinaryWriter writer = new BinaryWriter(fs);
			writer.BaseStream.Position = 0;

			//serialize textures names
			/*formatter.Serialize(fs, textures.Count);
			foreach(var pair in textures)
			{ 
				formatter.Serialize(fs, pair.Key);
			}*/

			//States
			writer.Write(activeStates.Length);
			foreach(State s in activeStates)
			{
				s.Serialize(writer);
			}

		}

		public void Deserialize(FileStream fs)
		{
			fs.Position = 0;
			BinaryReader reader = new BinaryReader(fs);
			reader.BaseStream.Position = 0;

			//load textures
			/*var texturesCount = (int) formatter.Deserialize(fs);
			for(int i = 0; i < texturesCount; ++i)
			{
				var tName = (String) formatter.Deserialize(fs);
				loadTexture(tName);
			}*/

			//States
			int stateCount = reader.ReadInt32();

			if (activeStates.Length < stateCount)
				Array.Resize(ref activeStates, stateCount);

			for(int i = 0; i < stateCount; ++i)
			{
				State s = activeStates[i];
				if (s == null)
					s = new State(this);
				s.Deserialize(reader);
				activeStates[i] = s;
			}
		}

		public void RegisterSystemSerialization(String name, DeserializationConstructor constructor)
		{
			systemsConstructors[name] = constructor;
		}

		public State getState(int index)
		{
			return activeStates[index];
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