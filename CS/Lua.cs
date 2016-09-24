using CS;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Loaders;
#if DEBUG && !ANDROID
using MoonSharp.RemoteDebugger;
#endif
using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Diagnostics;
using MG;

namespace CS.Components
{
	public class MyCustomScriptLoader : ScriptLoaderBase
	{
		public override object LoadFile(string file, Table globalContext)
		{
			var stream = TitleContainer.OpenStream(file);
			var reader = new StreamReader(stream, Encoding.UTF8);
			string value = reader.ReadToEnd();
			stream.Close();
			return value;
		}

		public override bool ScriptFileExists(string name)
		{
			var stream = TitleContainer.OpenStream(name);
			if (stream == null)
				return false;
			else
				stream.Close();
			return true;
		}
	}

	class GlobalLuaSystem : BaseSystem
	{
		public Script luaScript;

		public bool debug = false;

		public GlobalLuaSystem(State state) : base(state, "GlobalLuaSystem")
		{
			luaScript = new Script();
			LoadMainLuaScript();
		}

		public override BaseSystem DeserializeConstructor(State state, string name)
		{
			return new GlobalLuaSystem(state);
		}

		private void LoadMainLuaScript()
		{
			var G = _state.G;

			UserData.RegistrationPolicy = MoonSharp.Interpreter.Interop.InteropRegistrationPolicy.Automatic;
			/*var assemblies = AppDomain.CurrentDomain.GetAssemblies();
			foreach (var a in assemblies)
			{
				UserData.RegisterAssembly(a);
			}*/

			UserData.RegisterAssembly();

			UserData.RegisterType<LuaSystem>();
			UserData.RegisterType<LuaEntitySystem>();
			UserData.RegisterType<ConstructorInfo>();
			UserData.RegisterType<MouseState>();
			UserData.RegisterType<ButtonState>();
			UserData.RegisterType<Game>();
			UserData.RegisterType<State>();
			UserData.RegisterType<BaseSystem>();
			UserData.RegisterType<Transform>();
			UserData.RegisterType<Texture2>();
			UserData.RegisterType<FarseerPhysics.Dynamics.Body>();
			UserData.RegisterType<TransformSystem>();
			UserData.RegisterType<PhysicsSystem>();
			UserData.RegisterType<RenderSystem>();
			UserData.RegisterType<Camera>();



			luaScript.Globals["ContentDir"] = G.getSystem<MonogameSystem>().Game.Content.RootDirectory;
			ConstructorInfo constinfo = typeof(LuaSystem).GetConstructor(new[] { typeof(State), typeof(string) });
			luaScript.Globals["LuaSystem"] = constinfo;
			constinfo = typeof(LuaEntitySystem).GetConstructor(new[] { typeof(State), typeof(string) });
			luaScript.Globals["LuaEntitySystem"] = constinfo;
			constinfo = typeof(Vector2).GetConstructor(new[] { typeof(double), typeof(double) });
			luaScript.Globals["Vector2"] = constinfo;
			luaScript.Globals["ButtonState"] = UserData.CreateStatic<ButtonState>();
			luaScript.Globals["New"] = new New();
			luaScript.Globals["PhysicsObject"] = new PhysicsObject();

			luaScript.Options.ScriptLoader = new MyCustomScriptLoader();

#if DEBUG && !ANDROID
			RemoteDebuggerService remoteDebugger = null;

			if (remoteDebugger == null && debug)
			{
				remoteDebugger = new RemoteDebuggerService();

				// the last boolean is to specify if the script is free to run 
				// after attachment, defaults to false
				remoteDebugger.Attach(luaScript, "script", false);
				Process.Start(remoteDebugger.HttpUrlStringLocalHost);
			}

			// start the web-browser at the correct url. Replace this or just
			// pass the url to the user in some way.
#endif

			var stream = TitleContainer.OpenStream("Content/main.lua");
			using (var reader = new StreamReader(stream, Encoding.UTF8))
			{
				string value = reader.ReadToEnd();
				// Do something with the value
				luaScript.DoString(value);
			}
			stream.Close();

			//luaState.LoadFile(Content.RootDirectory + "/systems.lua");
		}

		public DynValue loadScript(string name)
		{
			DynValue script;
			var stream = TitleContainer.OpenStream("Content/" + name);
			using (var reader = new StreamReader(stream, Encoding.UTF8))
			{
				string value = reader.ReadToEnd();
				// Do something with the value
				script = luaScript.DoString(value);
			}
			stream.Close();

			return script;
		}

		public override void SerializeSystem(BinaryWriter writer)
		{
		}

		public override void DeserializeSystem(BinaryReader reader)
		{
		}
	}

	class LuaSystem : BaseSystem, ISysUpdateable
	{
		Script luaScript;

		bool init = false;

		private DynValue InitFunction { get; set; }
		private DynValue UpdateFunction { get; set; }

		public string InitFunctionName { get; set; }
		public string UpdateFunctionName { get; set; }

		public Table table;

		public LuaSystem(State state, String name) : base(state, name)
		{
			luaScript = state.G.getSystem<GlobalLuaSystem>().luaScript;
			InitFunctionName = "";
			UpdateFunctionName = "";
		}

		public void SetInitFunction(string functionName)
		{
			if (functionName == "")
				return;
			InitFunctionName = functionName;
			InitFunction = luaScript.Globals.Get(InitFunctionName);
		}

		public void SetUpdateFunction(string functionName)
		{
			if (functionName == "")
				return;
			UpdateFunctionName = functionName;
			UpdateFunction = luaScript.Globals.Get(functionName);
		}

		public override BaseSystem DeserializeConstructor(State state, string name)
		{
			return new LuaSystem(state, name);
		}

		public void Update(Global G)
		{
			if (!init && InitFunctionName != "")
			{
				luaScript.Call(InitFunction, this, _state);
				init = true;
			}

			if (UpdateFunctionName != "")
				luaScript.Call(UpdateFunction, this, _state);
		}

		public override void SerializeSystem(BinaryWriter writer)
		{
			writer.Write(InitFunctionName);
			writer.Write(UpdateFunctionName);
		}

		public override void DeserializeSystem(BinaryReader reader)
		{
			var initName = reader.ReadString();
			SetInitFunction(initName);

			var updateName = reader.ReadString();
			SetUpdateFunction(updateName);
		}
	}

	class LuaEntitySystem : EntitySystem, ISysUpdateable
	{
		Script luaScript;

		bool init = false;

		private DynValue InitFunction { get; set; }
		private DynValue PreUpdateFunction { get; set; }
		private DynValue EntityUpdateFunction { get; set; }
		private DynValue RemoveEntityFunction { get; set; }
		private DynValue AddEntityFunction { get; set; }

		public string InitFunctionName { get; set; }
		public string PreUpdateFunctionName { get; set; }
		public string EntityUpdateFunctionName { get; set; }
		public string RemoveEntityFunctionName { get; set; }
		public string AddEntityFunctionName { get; set; }

		public Table table;


		public LuaEntitySystem(State state, String name) : base(state, name)
		{
			luaScript = state.G.getSystem<GlobalLuaSystem>().luaScript;
			InitFunctionName = "";
			PreUpdateFunctionName = "";
			EntityUpdateFunctionName = "";
			RemoveEntityFunctionName = "";
			AddEntityFunctionName = "";
		}

		public void SetInitFunction(string functionName)
		{
			if (functionName == "")
				return;
			InitFunctionName = functionName;
			InitFunction = luaScript.Globals.Get(InitFunctionName);
		}

		public void SetPreUpdateFunction(string functionName)
		{
			if (functionName == "")
				return;
			PreUpdateFunctionName = functionName;
			PreUpdateFunction = luaScript.Globals.Get(functionName);
		}

		public void SetEntityUpdateFunction(string functionName)
		{
			if (functionName == "")
				return;
			EntityUpdateFunctionName = functionName;
			EntityUpdateFunction = luaScript.Globals.Get(functionName);
		}
		public void SetRemoveEntityFunction(string functionName)
		{
			if (functionName == "")
				return;
			RemoveEntityFunctionName = functionName;
			RemoveEntityFunction = luaScript.Globals.Get(functionName);
		}
		public void SetAddEntityFunction(string functionName)
		{
			if (functionName == "")
				return;
			AddEntityFunctionName = functionName;
			AddEntityFunction = luaScript.Globals.Get(functionName);
		}

		public override BaseSystem DeserializeConstructor(State state, string name)
		{
			return new LuaEntitySystem(state, name);
		}

		public void Update(Global G)
		{
			if (!init && InitFunctionName != "")
			{
				luaScript.Call(InitFunction, this, _state);
				init = true;
			}

			if (PreUpdateFunctionName != "")
				luaScript.Call(PreUpdateFunction, this, _state);
			if (EntityUpdateFunctionName != "")
				for (int i = 0; i < size; ++i)
				{
					if (entityIDs[i] == -1)
						continue;
					luaScript.Call(EntityUpdateFunction, this, _state, entityIDs[i]);
				}
		}

		public override int AddEntity(int id)
		{
			int r = base.AddEntity(id);
			if (AddEntityFunctionName != "")
				luaScript.Call(AddEntityFunction, this, _state, id);

			return r;
		}

		public override void RemoveEntity(int id)
		{
			if (RemoveEntityFunctionName != "")
				luaScript.Call(RemoveEntityFunction, this, _state, id);

			base.RemoveEntity(id);
		}

		public override void SerializeSystem(BinaryWriter writer)
		{

			writer.Write(InitFunctionName);
			writer.Write(PreUpdateFunctionName);
			writer.Write(EntityUpdateFunctionName);
			writer.Write(RemoveEntityFunctionName);
			writer.Write(AddEntityFunctionName);
		}

		public override void DeserializeSystem(BinaryReader reader)
		{
			var initName = reader.ReadString();
			SetInitFunction(initName);
			var preupdateName = reader.ReadString();
			SetPreUpdateFunction(preupdateName);
			var entityupdate = reader.ReadString();
			SetEntityUpdateFunction(entityupdate);
			var remove = reader.ReadString();
			SetRemoveEntityFunction(remove);
			var add = reader.ReadString();
			SetAddEntityFunction(add);
		}
	}

	[MoonSharpUserData]
	class New
	{
		public static Vector2 Vector2(double x, double y)
		{
			return new Vector2((float)x, (float)y);
		}

		public static Transform Transform()
		{
			return new Transform();
		}

		public static Texture2 Texture2(Global G, string name, double layer = 0.9)
		{
			return new Texture2(G, name, (float)layer);
		}
	}
}