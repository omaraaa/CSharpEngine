using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using System;
using System.Collections.Generic;
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
		CameraFollowSystem cameraFollow;
		SpriteSystem sprSys;
		PlayerSystem playerSys;
		FileStream fs;
		Random r;
		int eid = -1;
		int Clienteid = -1;
		NetClient peer;
		NetServer server;
		NetPeerConfiguration config;
		NetPeerConfiguration clientConfig;
		float acc = 0;
		float serverRate = 1 / 20f;

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
			config = new NetPeerConfiguration("TankCom")
			{ Port = 12345 };

			clientConfig = new NetPeerConfiguration("TankCom");

			peer = new NetClient(clientConfig);

			server = new NetServer(config);

#if ANDROID
			fs = new FileStream(Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), "data"), FileMode.OpenOrCreate, FileAccess.ReadWrite);
#else
			//fs = new FileStream("data", FileMode.OpenOrCreate, FileAccess.ReadWrite);
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
			cameraFollow = new CameraFollowSystem(state);
			sprSys = new SpriteSystem(state);
			mousesys = new MouseFollowSystem(state);
			ddSys = new DragAndDropSystem(state);
			collSys = new CollisionSystem(state);
			playerSys = new PlayerSystem(state);

			
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
				mousesys2.AddEntity(img.id);
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
			{
				if (peer.ServerConnection != null)
				{
					var msg = peer.CreateMessage();
					msg.Write(3);
					msg.Write(Clienteid);
					peer.SendMessage(msg, NetDeliveryMethod.ReliableOrdered);
					peer.Shutdown("disconnecting");
				}
				Exit();
			}
			if (Keyboard.GetState().IsKeyDown(Keys.F1) && peer.ServerConnection == null)
			{
				peer.Start();
				var connection = peer.Connect("192.168.1.100", 12345);		
			}
			if (Keyboard.GetState().IsKeyDown(Keys.F2) && server.Status == NetPeerStatus.NotRunning)
			{
				server.Start();
			}
			NetIncomingMessage message;
			while ((message = peer.ReadMessage()) != null)
			{
				switch (message.MessageType)
				{
					case NetIncomingMessageType.Data:
						// handle custom messages
						int type = message.ReadInt32();
						if(type == 0)
						{
							//Clienteid = message.ReadInt32();
							Image img2 = new Image(state, "player", new Vector2(100, 100));
							var id = img2.id;
							ddSys.AddEntity(id);
							var textC = textureSys.getComponent(img2.textureIndex);
							var sprite = new Sprite("player");
							sprSys.AddComponent(id, sprite);
							sprSys.Play("idle", id, 1, true);
							textC.setScale(2, 2);
							var player = new Player(id, physics, textC.textureRect, false);
							playerSys.AddComponent(id, player);
							//cameraFollow.SetEntity(id);
							var msg = peer.CreateMessage();
							msg.Write(1);
							msg.Write(id);
							peer.SendMessage(msg, NetDeliveryMethod.ReliableOrdered);
						}
						else if (type == 1)
						{
							var s = new MemoryStream();
							int cap = message.ReadInt32();
							byte[] bytes = message.ReadBytes(cap);
							s.Write(bytes, 0, cap);
							s.Position = 0;
							BinaryReader reader = new BinaryReader(s);
							state.Deserialize(reader);
							if(Clienteid == -1)
								Clienteid = message.ReadInt32();
							playerSys.setControl(Clienteid);
							cameraFollow.SetEntity(Clienteid);
						}
						else if (type == 2)
						{
							var id = message.ReadInt32();
							var x = message.ReadSingle();
							var y = message.ReadSingle();

							var ti = state.getComponentIndex(id, transSys.systemIndex);
							var pi = state.getComponentIndex(id, physics.systemIndex);
							if (ti != -1)
							{
								var t = new Vector2();
								var t2 = transSys.getComponent(ti);
								t.X = x;
								t.Y = y;

								if(Clienteid == id)
								{
									if(Vector2.Distance(t, t2.position) > 1)
									{
										t2.position = t;
									}
								} else
								{
									t2.position = t;
								}
							}
							if (pi != -1)
							{
								var p = physics.getComponent(pi);
								var vel = new Vector2(message.ReadSingle(), message.ReadSingle());
								if (Clienteid == id)
								{
									if (Vector2.Distance(p.LinearVelocity, vel) > FarseerPhysics.ConvertUnits.ToSimUnits(1f))
									{
										p.LinearVelocity = vel;
									}
								}
								else
								{
									p.LinearVelocity = vel;
								}
								
							}
						}
						else if (type == 3)
						{
							var id = message.ReadInt32();
							if(id != -1)
								state.RemoveEntity(id);
						} else if (type == 4)
						{
							Exit();
						}
						break;

					case NetIncomingMessageType.StatusChanged:
						// handle connection status messages
						switch (message.SenderConnection.Status)
						{
							case NetConnectionStatus.Connected:
									var msg = peer.CreateMessage();
									msg.Write(0);
									//msg.Write(eid);
									peer.SendMessage(msg, NetDeliveryMethod.ReliableOrdered);
								break;
						}
						break;

					case NetIncomingMessageType.DebugMessage:
						// handle debug messages
						// (only received when compiled in DEBUG mode)
						Console.WriteLine(message.ReadString());
						break;
					case NetIncomingMessageType.WarningMessage:
						Console.WriteLine(message.ReadString());
						break;
					/* .. */
					case NetIncomingMessageType.ConnectionApproval:
						message.SenderConnection.Approve();
						break;
					/* .. */
					default:
						Console.WriteLine("unhandled message with type: "
							+ message.MessageType);
						break;
				}
			}

			while ((message = server.ReadMessage()) != null)
			{
				switch (message.MessageType)
				{
					case NetIncomingMessageType.Data:
						// handle custom messages
						int type = message.ReadInt32();
						if (type == 0)
						{
							Image img2 = new Image(state, "player", new Vector2(100, 100));
							var id = img2.id;
							ddSys.AddEntity(id);
							var textC = textureSys.getComponent(img2.textureIndex);
							var sprite = new Sprite("player");
							sprSys.AddComponent(id, sprite);
							sprSys.Play("idle", id, 1, true);
							textC.setScale(2, 2);
							var player = new Player(id, physics, textC.textureRect, false);
							playerSys.AddComponent(id, player);

							//cameraFollow.SetEntity(id);
							var msg = server.CreateMessage();
							msg.Write(1);
							var s = new MemoryStream();
							BinaryWriter writer = new BinaryWriter(s);
							state.Serialize(writer);
							writer.Flush();
							s.Position = 0;
							byte[] bytes = s.ToArray();
							msg.Write(bytes.Length);
							msg.Write(bytes);
							//Clienteid = message.ReadInt32();
							msg.Write(id);
							writer.Close();
							server.SendToAll(msg, NetDeliveryMethod.ReliableOrdered);
						}
						else if (type == 1)
						{
							var s = new MemoryStream();
							int cap = message.ReadInt32();
							byte[] bytes = message.ReadBytes(cap);
							s.Write(bytes, 0, cap);
							BinaryReader reader = new BinaryReader(s);
							state.Deserialize(reader);
							Clienteid = message.ReadInt32();
						}
						else if (type == 2)
						{
							var id = message.ReadInt32();
							var x = message.ReadSingle();
							var y = message.ReadSingle();

							var ti = state.getComponentIndex(id, transSys.systemIndex);
							var pi = state.getComponentIndex(id, physics.systemIndex);
							if (ti != -1)
							{
								var t = transSys.getComponent(ti);
								t.position.X = x;
								t.position.Y = y;
							}
							if (pi != -1)
							{
								var p = physics.getComponent(pi);
								var vel = new Vector2(message.ReadSingle(), message.ReadSingle());
								p.LinearVelocity = vel;
							}
						}
						else if (type == 3)
						{
							var id = message.ReadInt32();
							if (id != -1)
								state.RemoveEntity(id);

						}
						else if (type == 4)
						{
							Exit();
						}
						break;

					case NetIncomingMessageType.StatusChanged:
						// handle connection status messages
						switch (message.SenderConnection.Status)
						{

						}
						break;

					case NetIncomingMessageType.DebugMessage:
						// handle debug messages
						// (only received when compiled in DEBUG mode)
						Console.WriteLine(message.ReadString());
						break;
					case NetIncomingMessageType.WarningMessage:
						Console.WriteLine(message.ReadString());
						break;
					/* .. */
					case NetIncomingMessageType.ConnectionApproval:
						message.SenderConnection.Approve();
						break;
					/* .. */
					default:
						Console.WriteLine("unhandled message with type: "
							+ message.MessageType);
						break;
				}
			}

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
			if (Clienteid != -1)
			{
				var msg = peer.CreateMessage();
				msg.Write(2);
				msg.Write(Clienteid);
				var t = transSys.getComponent(
					state.getComponentIndex(Clienteid, transSys.systemIndex));
				var p = physics.getComponent(
					state.getComponentIndex(Clienteid, physics.systemIndex));
				msg.Write(t.position.X);
				msg.Write(t.position.Y);
				msg.Write(p.LinearVelocity.X);
				msg.Write(p.LinearVelocity.Y);
				peer.SendMessage(msg, peer.Connections[0], NetDeliveryMethod.ReliableOrdered);
			}
			
			G.Update(gameTime);
			acc += G.dt;
			if (server.Status == NetPeerStatus.Running && acc >= serverRate)
			{
				List<int> updatedEntities = transSys.GetUpdated();
				foreach (int e in updatedEntities)
				{
					var msg = server.CreateMessage();
					msg.Write(2);
					msg.Write(e);
					var t = transSys.getComponent(
						state.getComponentIndex(e, transSys.systemIndex));
					var p = physics.getComponent(
						state.getComponentIndex(e, physics.systemIndex));
					msg.Write(t.position.X);
					msg.Write(t.position.Y);
					msg.Write(p.LinearVelocity.X);
					msg.Write(p.LinearVelocity.Y);
					server.SendToAll(msg, NetDeliveryMethod.ReliableOrdered);
				}

				acc -= serverRate;
			}
			//var fpsTime = ((double)1000 / gameTime.ElapsedGameTime.Milliseconds);

			//fpsGraph.Update(fpsTime);

			if (G.keyboardState.IsKeyDown(Keys.F11))
			{
				fs = new FileStream("data", FileMode.OpenOrCreate, FileAccess.ReadWrite);
				G.Serialize(fs);
				fs.Close();
			}
			if (G.keyboardState.IsKeyDown(Keys.F12))
			{
				fs = new FileStream("data", FileMode.OpenOrCreate, FileAccess.ReadWrite);
				G.Deserialize(fs);
				fs.Close();
			}


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
