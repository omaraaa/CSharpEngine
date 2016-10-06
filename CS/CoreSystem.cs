using System.Collections.Generic;
using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using System.Diagnostics;


namespace CS
{

	public abstract class BaseSystem
	{
		public GlobalData G { get; private set; }

		public uint Index { get; private set; }
		public uint Owner { get; set; }
		public int updateIndex = -1;
		public int renderIndex = -1;
		public String name;

		public BaseSystem(GlobalData G, String name)
		{
			Owner = 0;
			Index = (uint) G.AddData(this);
			this.name = name;
		}

		virtual public void Initialize() { }
		virtual public void Deactivate() { }

		abstract public void SerializeSystem(BinaryWriter writer);
		abstract public void DeserializeSystem(BinaryReader reader);

		virtual public void Serialize(BinaryWriter writer)
		{
			SerializeSystem(writer);
		}

		virtual public void Deserialize(BinaryReader reader)
		{
			DeserializeSystem(reader);
		}

		abstract public BaseSystem DeserializeConstructor(State state, string name);
	}

	public abstract class EntitySystem : BaseSystem
	{
		public State State { get; private set; }

		protected int[] entityIDs;
		protected int[] freeIndexes;
		protected int size;
		protected int serializeSize = 0;

		public EntitySystem(State state, string name) : base(state.G, name)
		{
			State = state;	
			entityIDs = new int[0];
			freeIndexes = new int[0];
			size = 0;
		}

		virtual public int AddEntity(int id)
		{
			var cindex = State.getComponentIndex(id, Index);
			if (cindex != -1 && cindex < size)
			{
				entityIDs[cindex] = id;
			}
			if (freeIndexes.Length == 0)
			{
				size++;
				Array.Resize(ref entityIDs, size);
				entityIDs[size - 1] = id;

				State.AddComponent(id, Index, size - 1);
				return size - 1;
			}
			else
			{
				var index = freeIndexes[freeIndexes.Length - 1];

				entityIDs[index] = id;
				State.AddComponent(id, Index, index);

				Array.Resize(ref freeIndexes, freeIndexes.Length - 1);

				return index;
			}
		}

		virtual public void RemoveEntity(int id)
		{
			var index = state.getComponentIndex(id, Index);
			if (index == -1)
				return;

			entityIDs[index] = -1;

			Array.Resize(ref freeIndexes, freeIndexes.Length + 1);
			freeIndexes[freeIndexes.Length - 1] = index;
			state.RemoveComponent(id, Index);
		}

		public bool ContainsEntity(int id, ref int index)
		{
			var indx = state.getComponentIndex(id, Index);
			index = indx;
			if (indx != -1)
				return true;
			else
				return false;
		}

		virtual public void SerializeEntity(int index, BinaryWriter writer)
		{

		}
		virtual public void DeserializeEntity(int id, int index, BinaryReader reader)
		{
			if(index >= size)
			{
				AddEntity(id);
			} else
			{
				entityIDs[index] = id;
			}
		}

		override public void Serialize(BinaryWriter writer)
		{
			base.Serialize(writer);
			writer.Write(freeIndexes.Length);
			foreach (var index in freeIndexes)
				writer.Write(index);

			writer.Write(size);
			foreach (var e in entityIDs)
			{
				writer.Write(e);
			}
		}

		override public void Deserialize(BinaryReader reader)
		{
			base.Deserialize(reader);

			int freesize = reader.ReadInt32();
			freeIndexes = new int[freesize];
			for (int i = 0; i < freesize; ++i)
				freeIndexes[i] = reader.ReadInt32();

			var s = reader.ReadInt32();
			entityIDs = new int[s];
			size = s;
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
		void Render(Global G);
	}
	/*
	 * System that manages the storage and update of differening components
	 */
	public abstract class ComponentSystem<T> : EntitySystem
	{
		protected T[] components;

		protected uint cachedComponent;

		public ComponentSystem(State state, String name) : base(state, name)
		{
			components = new T[0];
			size = 0;
			cachedComponent = 0;
		}

		virtual public int AddComponent(int id, T component)
		{
			var index = AddEntity(id);
			if(components.Length < size)
				Array.Resize(ref components, size);
			components[index] = component;
			return index;
		}

		virtual public void SetComponent(int id, T component)
		{
			var index = _state.getComponentIndex(id, Index);
			if (index < size)
				components[index] = component;
		}

		public T getComponent(int index)
		{
			return components[index];
		}

		public T getComponentByID(int id)
		{
			var index = _state.getComponentIndex(id, Index);

			return components[index];
		}

		public override void Serialize(BinaryWriter writer)
		{
			base.Serialize(writer);
			for(int i = 0; i < size; ++i)
			{
				SerailizeComponent(ref components[i], writer);
			}
			postSerialization(writer);
		}

		override public void Deserialize(BinaryReader reader)
		{
			base.Deserialize(reader);
			components = new T[size];
			for (int i = 0; i < size; ++i)
			{
				components[i] = DeserailizeComponent(reader);
			}
			postDeserialization(reader);
		}

		protected abstract void SerailizeComponent(ref T component, BinaryWriter writer);
		public override void SerializeEntity(int index, BinaryWriter writer)
		{
			SerailizeComponent(ref components[index], writer);
			base.SerializeEntity(index, writer);
		}

		protected abstract T DeserailizeComponent(BinaryReader reader);
		public override void DeserializeEntity(int id, int index, BinaryReader reader)
		{
			T component = DeserailizeComponent(reader);
			if (index >= size)
			{
				AddComponent(id, component);
			}
			else
			{
				base.DeserializeEntity(id, index, reader);
				components[index] = component;
			}
		}

		virtual protected void postSerialization(BinaryWriter writer)
		{

		}

		virtual protected void postDeserialization(BinaryReader reader)
		{

		}
	}


	public abstract class Data<T> : BaseSystem
	{
		protected T[] data;
		protected int[] freeIndexes;
		protected int[] used;
		protected int size;
		protected uint cachedComponent;

		public bool RemoveUnused { get; set; }

		public Data(State state, String name) : base(state, name)
		{
			data = new T[0];
			used = new int[0];
			freeIndexes = new int[0];
			size = 0;
			cachedComponent = 0;
			RemoveUnused = true;
		}

		virtual public int AddData(T component)
		{
			if (freeIndexes.Length == 0)
			{
				size++;
				Array.Resize(ref data, size);
				Array.Resize(ref used, size);
				data[size - 1] = component;
				used[size - 1] = 0;

				return size - 1;
			}
			else
			{
				var index = freeIndexes[freeIndexes.Length - 1];

				data[index] = component;
				used[index] = 0;

				Array.Resize(ref freeIndexes, freeIndexes.Length - 1);

				return index;
			}
			
		}

		virtual protected void RemoveData(int index)
		{
			if (index == -1)
				return;

			used[index] = -1;

			Array.Resize(ref freeIndexes, freeIndexes.Length + 1);
			freeIndexes[freeIndexes.Length - 1] = index;
		}

		public void RegisterUse(int index)
		{
			used[index] += 1;
		}

		public void DeregisterUse(int index)
		{
			used[index] -= 1;
		}

		public T this[int key]
		{
			get
			{
				return data[key];
			}
			set
			{
				data[key] = value;
			}
		}

		public override void Serialize(BinaryWriter writer)
		{
			base.Serialize(writer);
			for (int i = 0; i < size; ++i)
			{
				SerailizeData(ref data[i], writer);
			}
			postSerialization(writer);
		}

		override public void Deserialize(BinaryReader reader)
		{
			base.Deserialize(reader);
			data = new T[size];
			for (int i = 0; i < size; ++i)
			{
				data[i] = DeserailizeData(reader);
			}
			postDeserialization(reader);
		}

		protected abstract void SerailizeData(ref T component, BinaryWriter writer);
		public void SerializeData(int index, BinaryWriter writer)
		{
			SerailizeData(ref data[index], writer);
		}

		protected abstract T DeserailizeData(BinaryReader reader);
		public void DeserializeData(int index, BinaryReader reader)
		{
			T component = DeserailizeData(reader);
			if (index >= size)
			{
				AddData(component);
			}
			else
			{
				data[index] = component;
			}
		}

		virtual protected void postSerialization(BinaryWriter writer) { }
		virtual protected void postDeserialization(BinaryReader reader) { }

		protected void cleanup()
		{
			for(int i = 0; i < size; ++i)
			{
				if(used[i] == 0)
				{
					RemoveData(i);
				}
			}
		}
	}
	public class EntityManager : BaseSystem
	{
		//entities[0][0,0] means entity 0 data 0 index 0
		int[][] entities;
		private int[] removedEntities;
		private Stack<int> toRemoveEntities;
		public Queue<int> addedEntities;

		public EntityManager(State state, string name) : base(state, name)
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

		public int CreateEntity()
		{
			int index = 0;
			if (removedEntities.Length != 0)
			{
				index = removedEntities[removedEntities.Length - 1];
				Array.Resize(ref removedEntities, removedEntities.Length - 1);
			}
			else
			{
				Array.Resize(ref entities, entities.Length + 1);
				index = entities.Length - 1;
			}
			entities[index] = new int[entities.Length];
			for (int i = 0; i < entities.Length; ++i)
			{
				entities[index][i] = -1;
			}
			addedEntities.Enqueue(index);
			return index;
		}
	}

	public class GlobalData : Data<BaseSystem>, ISysUpdateable, ISysRenderable
	{

		private uint[] updateIndexes;
		private uint[] renderIndexes;

		public GlobalData() : base(null, "CSystem")
		{
			root = new Node("G");
			base.AddData(root);
		}

		public override void Initialize()
		{
			
			base.Initialize();
		}

		public override BaseSystem DeserializeConstructor(State state, string name)
		{
			return new CSystem();
		}

		public override void DeserializeSystem(BinaryReader reader)
		{
			throw new NotImplementedException();
		}

		public override void SerializeSystem(BinaryWriter writer)
		{
			throw new NotImplementedException();
		}

		protected override BaseSystem DeserailizeData(BinaryReader reader)
		{
			throw new NotImplementedException();
		}

		protected override void SerailizeData(ref BaseSystem component, BinaryWriter writer)
		{
			throw new NotImplementedException();
		}

		public void Update(Global G)
		{
			for (int i = 0; i < size; ++i)
			{
				if (data[i].updateIndex != -1)
				{
					var sys = (ISysUpdateable)data[i];
					sys.Update(G);
				}
			}
		}

		public void Render(Global G)
		{
			throw new NotImplementedException();
		}
	}

	public struct State
	{
		private uint[][] data;
		private uint[] systems;

		private uint[] updatableIndexes;
		private uint[] renderableIndexes;

		private int[][] entitiesIndexes;
		private int[] removedEntities;
		private Stack<int> toRemoveEntities;
		public Queue<int> addedEntities;

		public GlobalData G;
		public int index;

		public bool Paused { get; set; }

		public State(GlobalData G)
		{
			systems = new BaseSystem[0];
			updatableIndexes = new uint[0];
			renderableIndexes = new uint[0];
			entitiesIndexes = new int[0][];
			removedEntities = new int[0];
			toRemoveEntities = new Stack<int>();
			addedEntities = new Queue<int>();
			this.G = G;
		}

		protected void Initialize()
		{
		}

		public void Deactivate()
		{
			foreach (var sys in systems)
				sys.Deactivate();
		}

		public int EntitiesCount()
		{
			return entitiesIndexes.Length;
		}

		public uint RegisterSystem(BaseSystem system)
		{
			var size = systems.Length;
			size++;
			Array.Resize(ref systems, size);
			var type = system.GetType();
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
			if (Paused)
				return;
			CleanUp();
			foreach (uint index in updatableIndexes)
			{
				var sys = systems[index] as ISysUpdateable;
				sys.Update(G);
			}
		}

		public void Render()
		{
			if (Paused)
				return;
			for (int i = renderableIndexes.Length-1; i >=0; --i)
			{
				var index = renderableIndexes[i];
				var sys = systems[index] as ISysRenderable;
				sys.Render(G);
			}
		}

		public int CreateEntity()
		{
			int index = 0;
			if (removedEntities.Length != 0)
			{
				index = removedEntities[removedEntities.Length - 1];
				Array.Resize(ref removedEntities, removedEntities.Length - 1);
			} else
			{
				Array.Resize(ref entitiesIndexes, entitiesIndexes.Length + 1);
				index = entitiesIndexes.Length - 1;
			}
			entitiesIndexes[index] = new int[systems.Length];
			for(int i = 0; i < systems.Length; ++i)
			{
				entitiesIndexes[index][i] = -1;
			}
			addedEntities.Enqueue(index);
			return index;
		}


		public void AddComponent(int entityID, uint systemID, int index)
		{
			entitiesIndexes[entityID][systemID] = index;
		}

		public void RemoveComponent(int entityID, uint systemID)
		{
			entitiesIndexes[entityID][systemID] = -1;
		}

		public void RemoveEntity(int id)
		{
			toRemoveEntities.Push(id);
		}

		private void CleanUp()
		{
			for(int i = 0; i < toRemoveEntities.Count; ++i)
			{
				var id = toRemoveEntities.Pop();
				CleanEntity(id);
			}
		}

		private void CleanEntity(int id)
		{
			for (int i = 0; i < entitiesIndexes[id].Length; ++i)
			{
				if (entitiesIndexes[id][i] != -1)
				{
					var sys = systems[i] as EntitySystem;
					sys.RemoveEntity(id);
				}
			}

			Array.Resize(ref removedEntities, removedEntities.Length + 1);
			removedEntities[removedEntities.Length - 1] = id;
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

		public BaseSystem getSystem(String name)
		{
			foreach (BaseSystem system in systems)
			{
				if (system.name == name)
					return system;
			}

			return null;
		}

		public BaseSystem getSystem(uint index)
		{
			return systems[index];
		}

		protected State(SerializationInfo info, StreamingContext context)
		{
			renderableIndexes = (uint[]) info.GetValue("renderIndexes", typeof(uint[]));
		}

		public void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue("renderIndexes", renderableIndexes);

		}

		public void SerializeEntity(int id, BinaryWriter writer)
		{
			writer.Write(id);
			writer.Write(entitiesIndexes[id].Length);
			for (int i = 0; i < entitiesIndexes[id].Length; ++i)
			{
				var index = entitiesIndexes[id][i];
				writer.Write(index);

				if(index != -1)
				{
					var sys = systems[i] as EntitySystem;
					sys.SerializeEntity(index, writer);
				}
			}
		}

		public int DeserializeEntity(BinaryReader reader)
		{
			var id = reader.ReadInt32();
			var length = reader.ReadInt32();
			if (id >= entitiesIndexes.Length)
				Array.Resize(ref entitiesIndexes, entitiesIndexes.Length + 1);

			entitiesIndexes[id] = new int[length];

			for(int i = 0; i < length; ++i)
			{
				var index = reader.ReadInt32();
				entitiesIndexes[id][i] = index;

				if(index != -1)
				{
					var sys = systems[i] as EntitySystem;
					sys.DeserializeEntity(id, index, reader);
				}
			}

			return id;
		}

		public void Serialize(BinaryWriter writer)
		{
			writer.Write(removedEntities.Length);
			foreach (int index in removedEntities)
				writer.Write(index);

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
			int removedSize = reader.ReadInt32();
			removedEntities = new int[removedSize];
			for(int i = 0; i < removedSize; ++i)
			{
				removedEntities[i] = reader.ReadInt32();
			}


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
						G.systemsConstructors[name](this, name);
					systems[i].Deserialize(reader);
				}
			}
		}
	}

	delegate void FunctionDelegate(ref State state, uint id);

	public delegate BaseSystem DeserializationConstructor(State state, string name);


	[Serializable()]
	public class Global : State
	{
		private State[] activeStates;
		public float dt;
		public Dictionary<String, DeserializationConstructor> systemsConstructors;

		public Global() : base(null)
		{
			activeStates = new State[0];
			systemsConstructors = new Dictionary<String, DeserializationConstructor>();
			this.G = this;
			Initialize();
		}

		public void Update(float deltaTime)
		{
			dt = deltaTime;
			base.Update();

			foreach (State s in activeStates)
			{
				if (s != null)
					s.Update();
			}
		}

		public new void Render()
		{

			foreach (State s in activeStates)
			{
				if(s != null)
					s.Render();
			}
			base.Render();
		}

		public void ActivateState(State s)
		{
			Array.Resize(ref activeStates, activeStates.Length + 1);

			activeStates[activeStates.Length-1] = s;
			s.index = activeStates.Length - 1;
		}

		public void deactivateState(int index)
		{
			activeStates[index].Deactivate();
			activeStates[index] = null;
		}

		public void setState(int index, State state)
		{
			activeStates[index] = state;
		}

		public void Serialize(Stream fs)
		{
			fs.Position = 0;
			BinaryWriter writer = new BinaryWriter(fs);
			writer.BaseStream.Position = 0;

			base.Serialize(writer);

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

		public void Deserialize(Stream fs)
		{
			fs.Position = 0;
			BinaryReader reader = new BinaryReader(fs);
			reader.BaseStream.Position = 0;

			base.Deserialize(reader);

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
