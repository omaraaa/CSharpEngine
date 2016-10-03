using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using System;
using System.Reflection;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;

using CS;
using CS.Components;
using Util;
using Entities;
using MoonSharp.Interpreter;
using FarseerPhysics.Dynamics;
using FarseerPhysics.Dynamics.Contacts;

namespace LD36
{

	class LD36Game : Game
	{
		Global G;
		SpriteBatch batch;
		State gameState;
		GraphicsDeviceManager graphics;
		static public Queue<string> greeting = new Queue<string>();
		Timer ti;
		public static Dictionary<string, SoundEffectInstance> soundeffects = new Dictionary<string, SoundEffectInstance>();
		public static ContentManager content;
		public static bool tutorial = false;
		List<SoundEffect> sndfxs;
		RenderSystem renderSys;

		public static void PlaySound(string name, float vol = 1f, float pitch = 0f, float pan = 0f)
		{
			var s = soundeffects[name];
			if (s.State != SoundState.Playing)
			{
				s.Volume = vol;
				s.Pitch = pitch;
				s.Pan = pan;
				s.IsLooped = false;
				s.Play();
			}
		}

		public LD36Game() : base()
		{
			graphics = new GraphicsDeviceManager(this);
			Content.RootDirectory = "Content";
			//IsFixedTimeStep = false;
			//graphics.PreferredBackBufferWidth = 1920;
			//graphics.PreferredBackBufferHeight = 1080;
			//graphics.IsFullScreen = true;
			//graphics.ApplyChanges();
		}

		public void nextDialog(State state, int id)
		{
			var text = (Text) renderSys.getComponentByID(id);
			if (greeting.Count > 0)
			{
				ti.target = 2;
				text.String = greeting.Dequeue();
				if (text.String == "-_-zzZZ")
				{
					LD36Game.greeting.Enqueue("-_-z");
					LD36Game.greeting.Enqueue("-_-zz");
					LD36Game.greeting.Enqueue("-_-zzZ");
					LD36Game.greeting.Enqueue("-_-zzZZ");
				}
			}
			else
			{
				ti.target = 0.1f;
				text.String = "";
			}
			renderSys.SetComponent(id, text);
		}

		public void loadGameState()
		{
			Vector2 center = new Vector2(GraphicsDevice.Viewport.Width / 2f, GraphicsDevice.Viewport.Height / 2f);
			content = Content;
			sndfxs = new List<SoundEffect>();

			var killsound = Content.Load<SoundEffect>("killsound");
			var jumpsound = Content.Load<SoundEffect>("jumpsound");
			var landsound = Content.Load<SoundEffect>("landsound");
			var powerupsound = Content.Load<SoundEffect>("powerupsound");
			sndfxs.Add(killsound);
			sndfxs.Add(jumpsound);
			sndfxs.Add(landsound);
			sndfxs.Add(powerupsound);

			soundeffects["killsound"] = killsound.CreateInstance();
			soundeffects["jumpsound"] = jumpsound.CreateInstance();
			soundeffects["landsound"] = landsound.CreateInstance();
			soundeffects["powerupsound"] = powerupsound.CreateInstance();
			G = new Global();
			{
				var monogame = new MG.MonogameSystem(G);
				monogame.Game = this;
				var rendertargets = new RenderTargets(G);
				var camera = new MG.Camera(G);
				var spriteSys = new SpriteSystem(G);
				var fontsys = new FontSystem(G);
				var transSys = new TransformSystem(G);
				renderSys = new RenderSystem(G);
				var mouseFollow = new MouseFollowSystem(G);
				var luaSys = new GlobalLuaSystem(G);
				var tiledObjRegist = new RegistrySystem<TiledObjectContructor>(G, "TiledObjectContructor");
				var timer = new TimerSystem(G);
				tiledObjRegist.Register("playerSpawn", ObjectsGenerator.CreatePlayer);
				tiledObjRegist.Register("spike", ObjectsGenerator.CreateSpike);
				tiledObjRegist.Register("jump", ObjectsGenerator.CreateJumpPowerUp);
				tiledObjRegist.Register("checkpoint", ObjectsGenerator.CreateCheckPoint);
				tiledObjRegist.Register("infinite", ObjectsGenerator.CreateInfinitePowerUp);

				spriteSys.loadJSON("Content/player.json", "player");

				greeting.Enqueue("Hello");
				greeting.Enqueue("Collect all ancient technologies");
				greeting.Enqueue("");

				var greet = G.CreateEntity();
				fontsys.LoadFont("font");
				Text t = new Text(greeting.Dequeue(), center, fontsys, "font");
				renderSys.AddComponent(greet, t);
				TimerSystem.callbacks["nextDialog"] = nextDialog;

				ti = new Timer(2, true, "nextDialog");
				timer.AddTimer(greet, ti);


				//add cursor
				var img = new Image(G, "cursor", Vector2.Zero, Vector2.Zero);
				mouseFollow.AddEntity(img.id);
			}

			gameState = new State(G);
			{
				var camera = new MG.Camera(gameState);
				var transSys = new TransformSystem(gameState);
				var cameraFollow = new CameraFollowSystem(gameState);
				var physics = new PhysicsSystem(gameState);
				var render = new RenderSystem(gameState);
				var playerSys = new PlayerSystem(gameState);
				gameState.getSystem<MG.Camera>().setScale(new Vector2(1, 1));

				TiledLoader.LoadTiledLua(gameState, "tilemap.lua");
			}

			G.ActivateState(gameState);
		}


		protected override void Initialize()
		{

			batch = new SpriteBatch(GraphicsDevice);
			//Initialize GLobal State

			base.Initialize();
		}

		protected override void LoadContent()
		{
			loadGameState();
			base.LoadContent();
		}

		bool f5Pressed = false;
		protected override void Update(GameTime gameTime)
		{
			if (Keyboard.GetState().IsKeyDown(Keys.Escape))
			{
				Exit();
			}
			if (Keyboard.GetState().IsKeyDown(Keys.F5) && !f5Pressed)
			{
				string exePath = System.IO.Directory.GetCurrentDirectory();
				string strCmdText;
				strCmdText = "/C cd ../../../../Content/ & MGCB /@:Content.mgcb /clean /build";
				var p = System.Diagnostics.Process.Start("CMD.exe", strCmdText);

				p.WaitForExit();
				string sourceDirName = "../../../../Content/bin/DesktopGL/";
				string destDirName = "Content";
				strCmdText = "/C xcopy \"../../../../Content/bin/DesktopGL/*.*\" \"Content\" /s/h/e/k/f/c/y & LD36.exe";
				System.Diagnostics.Process.Start("CMD.exe", strCmdText);
				Exit();
			}
			if (Keyboard.GetState().IsKeyUp(Keys.F5))
				f5Pressed = false;

			G.Update((float) gameTime.ElapsedGameTime.TotalSeconds);
			base.Update(gameTime);
		}

		protected override void Draw(GameTime gameTime)
		{
			GraphicsDevice.Clear(Color.Black);

			batch.Begin();
			G.Render();
			batch.End();
			base.Draw(gameTime);
		}
	}

	static class ObjectsGenerator
	{
		public static void CreatePlayer(State state, Table layer, Table obj, float renderLayer)
		{
			var x = (float)obj.Get("x").Number;
			var y = (float)obj.Get("y").Number;

			var player = new Player(state, new Vector2(x, y), renderLayer, true);
		}

		public static void CreateSpike(State state, Table layer, Table obj, float renderLayer)
		{
			var x = (float)obj.Get("x").Number;
			var y = (float)obj.Get("y").Number;

			var img = new Image(state, "spike", new Vector2(x + 16, y + 16), Vector2.Zero, renderLayer);

			var physics = state.getSystem<PhysicsSystem>();
			var body = PhysicsObject.CreateBody(physics, 28, 26, 0);
			physics.AddComponent(img.id, body);

			body.OnCollision += killPlayer;
		}

		public static void CreateJumpPowerUp(State state, Table layer, Table obj, float renderLayer)
		{
			var x = (float)obj.Get("x").Number;
			var y = (float)obj.Get("y").Number;

			var img = new Image(state, "jump", new Vector2(x + 16, y + 16), Vector2.Zero, renderLayer);

			var physics = state.getSystem<PhysicsSystem>();
			var body = PhysicsObject.CreateBody(physics, 32, 32, 0);
			physics.AddComponent(img.id, body);
			KeyValuePair<State, int> pair = new KeyValuePair<State, int>(state, img.id);
			body.UserData = pair;
			body.OnCollision += addJump;
		}

		public static void CreateInfinitePowerUp(State state, Table layer, Table obj, float renderLayer)
		{
			var x = (float)obj.Get("x").Number;
			var y = (float)obj.Get("y").Number;

			var img = new Image(state, "infinite", new Vector2(x + 16, y + 16), Vector2.Zero, renderLayer);

			var physics = state.getSystem<PhysicsSystem>();
			var body = PhysicsObject.CreateBody(physics, 32, 32, 0);
			physics.AddComponent(img.id, body);
			KeyValuePair<State, int> pair = new KeyValuePair<State, int>(state, img.id);
			body.UserData = pair;
			body.OnCollision += addInfinite;
		}

		public static void CreateCheckPoint(State state, Table layer, Table obj, float renderLayer)
		{
			var x = (float)obj.Get("x").Number;
			var y = (float)obj.Get("y").Number;
			var w = (float)obj.Get("width").Number;
			var h = (float)obj.Get("height").Number;

			var transformSys = state.getSystem<TransformSystem>();
			var textureSys = state.getSystem<RenderSystem>();

			var id = state.CreateEntity();

			Transform transfom = new Transform();
			transfom.position = new Vector2(x + w / 2, y + h / 2);
			transformSys.AddComponent(id, transfom);

			var physics = state.getSystem<PhysicsSystem>();
			var body = PhysicsObject.CreateBody(physics, (int)w, (int)h, 0);
			physics.AddComponent(id, body);
			body.IsSensor = true;
			body.UserData = transfom.position;
			body.OnCollision += SetSpawn;
		}

		public static bool killPlayer(Fixture a, Fixture b, Contact c)
		{
			LD36.LD36Game.PlaySound("killsound");

			var player = b.Body.UserData as Player;
			player.setPos(player.spawnPos);
			return true;
		}

		public static bool SetSpawn(Fixture a, Fixture b, Contact c)
		{
			var player = b.Body.UserData as Player;
			var pos = (Vector2)a.Body.UserData;
			player.spawnPos = pos;
			return true;
		}

		public static bool addJump(Fixture a, Fixture b, Contact c)
		{
			LD36.LD36Game.PlaySound("powerupsound");

			var player = b.Body.UserData as Player;
			player.maxJumps++;

			var pair = (KeyValuePair<State, int>)a.Body.UserData;
			pair.Key.RemoveEntity(pair.Value);
			a.Body.OnCollision -= addJump;

			if (!LD36Game.tutorial)
			{
				LD36Game.greeting.Enqueue("You have aquired an ancient technology!");
				LD36Game.greeting.Enqueue("JUMP+");
				LD36Game.greeting.Enqueue("");
				LD36Game.tutorial = true;
			}

			return true;
		}

		public static bool addInfinite(Fixture a, Fixture b, Contact c)
		{
			LD36.LD36Game.PlaySound("powerupsound");

			var player = b.Body.UserData as Player;
			player.infinite = true;

			var pair = (KeyValuePair<State, int>)a.Body.UserData;
			pair.Key.RemoveEntity(pair.Value);
			a.Body.OnCollision -= addInfinite;

			LD36Game.greeting.Enqueue("Fly!");
			LD36Game.greeting.Enqueue("Thanks for playing!");
			LD36Game.greeting.Enqueue("Thank you!");
			LD36Game.greeting.Enqueue("The end!");
			LD36Game.greeting.Enqueue("The game has ended!");
			LD36Game.greeting.Enqueue("I guess you like flying.");
			LD36Game.greeting.Enqueue("woohooo!");
			LD36Game.greeting.Enqueue("...");
			LD36Game.greeting.Enqueue("....");
			LD36Game.greeting.Enqueue(".....");
			LD36Game.greeting.Enqueue("You still playing?");
			LD36Game.greeting.Enqueue("Ok, you can stop now.");
			LD36Game.greeting.Enqueue("...");
			LD36Game.greeting.Enqueue("For how long you gonna do this?");
			LD36Game.greeting.Enqueue("Please stop...");
			LD36Game.greeting.Enqueue("Please...");
			LD36Game.greeting.Enqueue("This is fun for you?");
			LD36Game.greeting.Enqueue("...");
			LD36Game.greeting.Enqueue("..");
			LD36Game.greeting.Enqueue(".");
			LD36Game.greeting.Enqueue("Let me tell you the story of this little red guy.");
			LD36Game.greeting.Enqueue("His looks might deceive you.");
			LD36Game.greeting.Enqueue("He is actually a salary man.");
			LD36Game.greeting.Enqueue("This guy spends most of his day working.");
			LD36Game.greeting.Enqueue("Unit one day.");
			LD36Game.greeting.Enqueue("He snaps!");
			LD36Game.greeting.Enqueue("He blocks the entrance!");
			LD36Game.greeting.Enqueue("Which kills everyone!");
			LD36Game.greeting.Enqueue("And now he took their souls.");
			LD36Game.greeting.Enqueue("The end!");
			LD36Game.greeting.Enqueue("Happy?");
			LD36Game.greeting.Enqueue("-_-");
			LD36Game.greeting.Enqueue("-_-z");
			LD36Game.greeting.Enqueue("-_-zz");
			LD36Game.greeting.Enqueue("-_-zzZ");
			LD36Game.greeting.Enqueue("-_-zzZZ");
			LD36Game.tutorial = true;

			return true;
		}
	}
}