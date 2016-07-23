using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using System;
using System.Diagnostics;

using CS;
using CS.Components;
using Util;
using Entities;

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
			// TODO: Add your initialization logic here
			graphics.PreferredBackBufferWidth = GraphicsDevice.DisplayMode.Width;
			graphics.PreferredBackBufferHeight = GraphicsDevice.DisplayMode.Height;
			graphics.SynchronizeWithVerticalRetrace = false;
			graphics.IsFullScreen = true;
			graphics.ApplyChanges();

			fpsGraph = new DebugGraph(GraphicsDevice, new Rectangle(0, 50, 200, 25), 100, 60);

			G = new Global(this);

			state = new State(G);
			transSys = new TransformSystem(state);
			physics = new PhysicsSystem(state);
			textureSys = new TextureSystem(state, GraphicsDevice, transSys, physics);
			mousesys = new MouseFollowSystem(state, transSys);
			ddSys = new DragAndDropSystem(state);
			collSys = new CollisionSystem(state);
			PlayerSystem playerSys = new PlayerSystem(state);
			r = new Random();

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
				Image img = new Image(state, "cursor", new Vector2(0, 0), 0.1f);
				Image img2 = new Image(state, "landscape1", new Vector2(10, 10));
				eid = img2.id;
				ddSys.AddEntity(eid);
				mousesys.AddEntity(img.id);
				var textC = textureSys.getComponent(img2.textureIndex);
				var p2 = PhysicsObject.CreateBody(physics, textC.textureRect.Width, textC.textureRect.Height, FarseerPhysics.Dynamics.BodyType.Dynamic);
				physics.AddComponent(eid, p2);
				var player = new Player(physics.world, p2);
				playerSys.AddComponent(eid, player);
			}

			var thickness = 100;
			{
				var e = state.CreateEntity();
				Transform t = new Transform();
				t.position.Y = graphics.PreferredBackBufferHeight + thickness/2;
				t.position.X = graphics.PreferredBackBufferWidth / 2;
				transSys.AddComponent(e, t);
				var p = PhysicsObject.CreateBody(physics, graphics.PreferredBackBufferWidth, thickness, FarseerPhysics.Dynamics.BodyType.Static);
				physics.AddComponent(e, p);
			}

			{
				var e = state.CreateEntity();
				Transform t = new Transform();
				t.position.Y = 0- thickness/2;
				t.position.X = graphics.PreferredBackBufferWidth / 2;
				transSys.AddComponent(e, t);
				var p = PhysicsObject.CreateBody(physics, graphics.PreferredBackBufferWidth, thickness, FarseerPhysics.Dynamics.BodyType.Static);
				physics.AddComponent(e, p);
			}
			{
				var e = state.CreateEntity();
				Transform t = new Transform();
				t.position.Y = graphics.PreferredBackBufferHeight / 2;
				t.position.X = graphics.PreferredBackBufferWidth + thickness/2;
				transSys.AddComponent(e, t);
				var p = PhysicsObject.CreateBody(physics, thickness, graphics.PreferredBackBufferHeight, FarseerPhysics.Dynamics.BodyType.Static);
				physics.AddComponent(e, p);
			}
			{
				var e = state.CreateEntity();
				Transform t = new Transform();
				t.position.Y = graphics.PreferredBackBufferHeight / 2;
				t.position.X = 0- thickness / 2;
				transSys.AddComponent(e, t);
				var p = PhysicsObject.CreateBody(physics, thickness, graphics.PreferredBackBufferHeight, FarseerPhysics.Dynamics.BodyType.Static);
				physics.AddComponent(e, p);
			}
			G.ActivateState(state);

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
			if (G.mouseState.RightButton == ButtonState.Pressed || touch.Count == 2)
			{
				var e = state.CreateEntity();
				Transform t = new Transform();
				t.position.X = (float)mousestate.X;
				t.position.Y = (float) mousestate.Y;
				if(touch.Count == 2)
				{
					t.position.X += touch[0].Position.X;
					t.position.Y +=  touch[0].Position.Y;
				}
				Debug.Assert(!float.IsNaN(t.position.X) && !float.IsNaN(t.position.Y));
				transSys.AddComponent(e, t);

				Texture2 texture = new Texture2(G, "SomeGuy1");
				texture.setScale(0.5f, 0.5f);
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
