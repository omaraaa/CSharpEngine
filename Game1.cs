using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

using CS;
using CS.Components;
using Util;
using Entities;

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
		int eid;

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
			//graphics.SynchronizeWithVerticalRetrace = false;
			graphics.IsFullScreen = true;
			graphics.ApplyChanges();

			fpsGraph = new DebugGraph(GraphicsDevice, new Rectangle(0, 50, 200, 25), 100, 60);

			G = new Global(this);

			state = new State(G);
			transSys = new TransformSystem(state);
			textureSys = new TextureSystem(state, GraphicsDevice, transSys);
			mousesys = new MouseFollowSystem(state, transSys);
			DragAndDropSystem ddSys = new DragAndDropSystem(state);

			//state.RegisterSystem(transSys);
			//state.RegisterSystem(textureSys);
			for (int i = 0; i < 1; ++i)
			{
				var e = state.CreateEntity();
				Transform t = new Transform();
				t.position = new Vector2(0, 0);
				t.velocity = new Vector2(131, 0);
				transSys.AddComponent(e, t);

				Texture2 texture = new Texture2(G, "SomeGuy1");
				texture.setScale(4, 4);
				textureSys.AddComponent(e, texture);
				ddSys.AddEntity(e);
				//mousesys.AddEntity(e);
			}
			Image img = new Image(state, "cursor", new Vector2(0, 0), 0.1f);
			Image img2 = new Image(state, "landscape1", new Vector2(0, 0));
			eid = img2.id;
			ddSys.AddEntity(eid);
			mousesys.AddEntity(img.id);
			G.ActivateState(state);
			//e = state.CreateEntity();
			//p = new Position(10, 10);
			//Transform pos = new Transform(e, 10, 10);
			//pos.vel.X = 200;
			//e.AddComponent(ref pos, "pos");
			//var mf = new MouseFollower(e, pos);
			//e.AddComponent(ref mf, "mouseFollower");
			//Texture2DC text = new Texture2DC(e, "landscape1", e.GetComponent<Transform>("pos"));
			//e.AddComponent(ref text, "texture");
			String[] imgs = { "cursor", "SomeGuy1" };

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
		bool mhold = false;
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

			fpsGraph.Update(fpsTime);
			spriteBatch.Begin(sortMode: SpriteSortMode.FrontToBack, samplerState: SamplerState.PointClamp);
			{ 
				Vector2 textMiddlePoint = font.MeasureString("HELLO") / 2;
				// Places text in center of the screen
				G.Render(spriteBatch);
				spriteBatch.DrawString(font, "FPS: " + fpsTime.ToString(), position, Color.White, 0, Vector2.Zero, 0.5f, SpriteEffects.None, 0.9f);
				spriteBatch.DrawString(font, "Memory:" + GC.GetTotalMemory(false) / 1024, new Vector2(0, 10), Color.White, 0, Vector2.Zero, 0.5f, SpriteEffects.None, 0.9f);
				fpsGraph.Draw(spriteBatch);
			}
			spriteBatch.End();

			base.Draw(gameTime);
		}
	}
}
