using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using System;
using System.IO;
using System.Diagnostics;

using CS;
using CS.Components;
using Util;
using Entities;

using Lidgren.Network;
using Lidgren;
using MoonSharp.Interpreter;

namespace TankComProject
{
	
	delegate void funcD(params object[] objs);

	/// <summary>
	/// This is the main type for your game.
	/// </summary>
	public class Game1 : Game
	{

		GraphicsDeviceManager graphics;
		SpriteBatch spriteBatch;
		Texture2D texture;
		Rectangle distRect;
		SpriteFont font;
		Global G;
		State state;
		DebugGraph fpsGraph;
		TransformSystem transSys;
		TextureSystem textureSys;
		MouseFollowSystem mousesys;
		DragAndDropSystem ddSys;
		CollisionSystem collSys;
		PhysicsSystem physics;
		FileStream fs;
		Random r;
		int eid;

		double MoonSharpFactorial()
		{
			string script = @"    
		-- defines a factorial function
		function fact (n)
			if (n == 0) then
				return 1
			else
				return n*fact(n - 1)
			end
		end

		return fact(5)";

			DynValue res = Script.RunString(script);
			return res.Number;
		}

		public Game1()
		{
			graphics = new GraphicsDeviceManager(this);
			Content.RootDirectory = "Content";
			//TargetElapsedTime = new System.TimeSpan(0, 0, 0, 0, 33/2);
			IsFixedTimeStep = false;

		}

		/// <summary>
		/// Allows the game to perform any initialization it needs to before starting to run.
		/// This is where it can query for any required services and load any non-graphic
		/// related content.  Calling base.Initialize will enumerate through any components
		/// and initialize them as well.
		/// </summary>
		protected override void Initialize()
		{
			var config = new NetPeerConfiguration("TankCom")
			{ Port = 12345 };
			var server = new NetServer(config);
			server.Start();

#if ANDROID
			fs = new FileStream(Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), "data"), FileMode.OpenOrCreate, FileAccess.ReadWrite);
#else
			fs = new FileStream("data", FileMode.OpenOrCreate, FileAccess.ReadWrite);
#endif       
			// TODO: Add your initialization logic here
			//graphics.PreferredBackBufferWidth = GraphicsDevice.DisplayMode.Width;
			//graphics.PreferredBackBufferHeight = GraphicsDevice.DisplayMode.Height;
			graphics.SynchronizeWithVerticalRetrace = false;
			//graphics.IsFullScreen = true;
			graphics.ApplyChanges();

			fpsGraph = new DebugGraph(GraphicsDevice, new Rectangle(0, 50, 200, 25), 100, 60);

			G = new Global(this);

			state = new State(G);
			State guiState = new State(G);
#if ANDROID
			state.camera.scale = new Vector2(4, 4);
			guiState.camera.scale = new Vector2(4, 4);
#endif

			transSys = new TransformSystem(state);
			physics = new PhysicsSystem(state);
			textureSys = new TextureSystem(state);
			CameraFollowSystem cameraFollow = new CameraFollowSystem(state);
			SpriteSystem sprSys = new SpriteSystem(state);
			mousesys = new MouseFollowSystem(state);
			ddSys = new DragAndDropSystem(state);
			collSys = new CollisionSystem(state);
			PlayerSystem playerSys = new PlayerSystem(state);

			
			TransformSystem transSys2 = new TransformSystem(guiState);
			//PhysicsSystem physics2 = new PhysicsSystem(guiState);
			TextureSystem textureSys2 = new TextureSystem(guiState);
			//CameraFollowSystem cameraFollow2 = new CameraFollowSystem(guiState);
			SpriteSystem sprSys2 = new SpriteSystem(guiState);
			MouseFollowSystem mousesys2 = new MouseFollowSystem(guiState);
			DragAndDropSystem ddSys2 = new DragAndDropSystem(guiState);
			CollisionSystem collSys2 = new CollisionSystem(guiState);
			//PlayerSystem playerSys2 = new PlayerSystem(guiState);
			

			r = new Random();


			//sprSys.CreateGridFrames("player", 30, 27);
			{
				sprSys.loadJSON("Content/player.json", "player");
			}

			//state.RegisterSystem(transSys);
			//state.RegisterSystem(textureSys);
			for (int i = 0; i <0; ++i)
			{
				var e = state.CreateEntity();
				Transform t = new Transform();
				transSys.AddComponent(e, t);

				Texture2 texture = new Texture2(G, "SomeGuy1");
				texture.setScale(1, 1);
				t.position = new Vector2(4*i, 100);
				textureSys.AddComponent(e, texture);
				ddSys.AddEntity(e);


				var p = PhysicsObject.CreateBody(physics, texture.textureRect.Width, texture.textureRect.Height, FarseerPhysics.Dynamics.BodyType.Dynamic);
				//p.body.IsBullet = true;
				physics.AddComponent(e, p);
				//mousesys.AddEntity(e);
			}
			{
				Image img = new Image(guiState, "cursor", new Vector2(0, 0), 0.1f);
				Image img2 = new Image(state, "player", new Vector2(100, 100));
				eid = img2.id;
				ddSys.AddEntity(eid);
				mousesys2.AddEntity(img.id);
				var textC = textureSys.getComponent(img2.textureIndex);
				var sprite = new Sprite("player");
				sprSys.AddComponent(eid, sprite);
				sprSys.Play("idle", eid,1, true);
				textC.setScale(2, 2);
				var player = new Player(eid, physics, textC.textureRect);
				playerSys.AddComponent(eid, player);
				cameraFollow.SetEntity(eid);
			}

			var thickness = 32;
			{
				var e = state.CreateEntity();
				Transform t = new Transform();
				t.position.Y = graphics.PreferredBackBufferHeight + thickness/2;
				t.position.X = graphics.PreferredBackBufferWidth / 2;
				transSys.AddComponent(e, t);

				var texture = new Texture2(G, "BlockUnit");
				//texture.setRect(graphics.PreferredBackBufferWidth, thickness);
				texture.srcRect.Width = graphics.PreferredBackBufferWidth/2;
				texture.srcRect.Height = thickness/2;
				texture.setScale(2, 2);
				textureSys.AddComponent(e, texture);

				var p = PhysicsObject.CreateBody(physics, graphics.PreferredBackBufferWidth, thickness, FarseerPhysics.Dynamics.BodyType.Static);
				physics.AddComponent(e, p);
			}

			{
				var e = state.CreateEntity();
				Transform t = new Transform();
				t.position.Y = 0- thickness/2;
				t.position.X = graphics.PreferredBackBufferWidth / 2;
				transSys.AddComponent(e, t);

				var texture = new Texture2(G, "BlockUnit");
				texture.srcRect.Width = graphics.PreferredBackBufferWidth / 2;
				texture.srcRect.Height = thickness / 2;
				texture.setScale(2, 2);
				textureSys.AddComponent(e, texture);

				var p = PhysicsObject.CreateBody(physics, graphics.PreferredBackBufferWidth, thickness, FarseerPhysics.Dynamics.BodyType.Static);
				physics.AddComponent(e, p);
			}
			{
				var e = state.CreateEntity();
				Transform t = new Transform();
				t.position.Y = graphics.PreferredBackBufferHeight / 2;
				t.position.X = graphics.PreferredBackBufferWidth + thickness/2;
				transSys.AddComponent(e, t);

				var texture = new Texture2(G, "BlockUnit");
				texture.srcRect.Width = thickness/2;
				texture.srcRect.Height = graphics.PreferredBackBufferHeight/2;
				texture.setScale(2, 2);
				textureSys.AddComponent(e, texture);

				var p = PhysicsObject.CreateBody(physics, thickness, graphics.PreferredBackBufferHeight, FarseerPhysics.Dynamics.BodyType.Static);
				physics.AddComponent(e, p);
			}
			{
				var e = state.CreateEntity();
				Transform t = new Transform();
				t.position.Y = graphics.PreferredBackBufferHeight / 2;
				t.position.X = 0- thickness / 2;
				transSys.AddComponent(e, t);

				var texture = new Texture2(G, "BlockUnit");
				texture.srcRect.Width = thickness/2;
				texture.srcRect.Height = graphics.PreferredBackBufferHeight/2;
				texture.setScale(2, 2);
				textureSys.AddComponent(e, texture);

				var p = PhysicsObject.CreateBody(physics, thickness, graphics.PreferredBackBufferHeight, FarseerPhysics.Dynamics.BodyType.Static);
				physics.AddComponent(e, p);
			}
			G.ActivateState(state);
			G.ActivateState(guiState);

			base.Initialize();
		}

		/// <summary>
		/// LoadContent will be called once per game and is the place to load
		/// all of your content.
		/// </summary>
		protected override void LoadContent()
		{
			// Create a new SpriteBatch, which can be used to draw textures.
			spriteBatch = new SpriteBatch(GraphicsDevice);
			texture = this.Content.Load<Texture2D>("landscape1");
			int winH = texture.Height;
			int winW = texture.Width;
			distRect = new Rectangle(0, 0, winW, winH);

			font = this.Content.Load<SpriteFont>("font");

			
			// TODO: use this.Content to load your game content here
		}

		/// <summary>
		/// UnloadContent will be called once per game and is the place to unload
		/// game-specific content.
		/// </summary>
		protected override void UnloadContent()
		{
			// TODO: Unload any non ContentManager content here
		}
		//bool mhold = false;
		MouseState mousestate;
		/// <summary>
		/// Allows the game to run logic such as updating the world,
		/// checking for collisions, gathering input, and playing audio.
		/// </summary>
		/// <param name="gameTime">Provides a snapshot of timing values.</param>
		protected override void Update(GameTime gameTime)
		{
			if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
				Exit();
			if (Keyboard.GetState().IsKeyDown(Keys.F1))
				mousesys.AddEntity(1);
			if (Keyboard.GetState().IsKeyDown(Keys.F2))
				mousesys.RemoveEntity(1);




			// TODO: Add your update logic here
			var dmx = mousestate.X;
			var dmy = mousestate.Y;
			mousestate = Mouse.GetState();
			var touch = TouchPanel.GetState();
			var scale = state.camera.matrix.Scale;
			if (G.mouseState.RightButton == ButtonState.Pressed || touch.Count == 2)
			{
				var e = state.CreateEntity();
				Transform t = new Transform();
				var pos = new Vector2(mousestate.X, mousestate.Y);
				state.camera.toCamera(ref pos);
				t.position.X = (float)pos.X/scale.X;
				t.position.Y = (float)pos.Y/scale.Y;
				if(touch.Count == 2)
				{
					var pos2 = new Vector2(touch[0].Position.X, touch[0].Position.Y);
					state.camera.toCamera(ref pos2);
					t.position.X = pos2.X / scale.X;
					t.position.Y = pos2.Y / scale.Y;
				}
				Debug.Assert(!float.IsNaN(t.position.X) && !float.IsNaN(t.position.Y));
				transSys.AddComponent(e, t);

				Texture2 texture = new Texture2(G, "BlockUnit");
				texture.setScale(2f, 2f);
				//t.position.X += r.Next() % 3;
				textureSys.AddComponent(e, texture);
				ddSys.AddEntity(e);


				var p = PhysicsObject.CreateBody(physics, texture.textureRect.Width, texture.textureRect.Height, FarseerPhysics.Dynamics.BodyType.Dynamic);
				//p.body.IgnoreCCD = true;
				//p.body.Restitution = 0;
				//p.body.FixedRotation = true;
				//p.body.Friction = 1;
				//p.body.IsBullet = true;
				physics.AddComponent(e, p);
				//mousesys.AddEntity(e);
			}
			G.Update(gameTime);

			//var fpsTime = ((double)1000 / gameTime.ElapsedGameTime.Milliseconds);

			//fpsGraph.Update(fpsTime);

			if (G.keyboardState.IsKeyDown(Keys.F11))
				G.Serialize(fs);
			if(G.keyboardState.IsKeyDown(Keys.F12))
				G.Deserialize(fs);


			base.Update(gameTime);
		}
		Vector2 position = new Vector2(0, 0);
		/// <summary>
		/// This is called when the game should draw itself.
		/// </summary>
		/// <param name="gameTime">Provides a snapshot of timing values.</param>
		protected override void Draw(GameTime gameTime)
		{
			GraphicsDevice.Clear(Color.CornflowerBlue);
			var fpsTime = ((double)1000 / gameTime.ElapsedGameTime.Milliseconds);
#if DEBUG
			fpsGraph.Update(fpsTime);
#endif
			spriteBatch.Begin(sortMode: SpriteSortMode.FrontToBack, samplerState: SamplerState.PointClamp);
			{ 
				Vector2 textMiddlePoint = font.MeasureString("HELLO") / 2;
				// Places text in center of the screen
				G.Render(spriteBatch);
#if DEBUG
				spriteBatch.DrawString(font, "FPS: " + fpsTime.ToString(), position, Color.White, 0, Vector2.Zero, 1f, SpriteEffects.None, 0.9f);
				spriteBatch.DrawString(font, "Memory:" + GC.GetTotalMemory(false) / 1024, new Vector2(0, 10), Color.White, 0, Vector2.Zero, 1f, SpriteEffects.None, 0.9f);
				fpsGraph.Draw(spriteBatch);
#endif
			}
			spriteBatch.End();

			base.Draw(gameTime);
		}
	}
}
