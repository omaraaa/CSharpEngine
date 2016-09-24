using System.Collections.Generic;
using System.Collections;
using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using Microsoft.Xna.Framework.Graphics;

using Newtonsoft.Json;

using CS;

namespace CS.Components
{

	public struct Animation
	{
		public string name;
		public float fps;
		public int index;
		public int frameLength;
		public int[] frames;
		public bool repeat;
		public bool active;

		public Animation(String name, int[] frames, int fps, bool repeat)
		{
			this.name = name;
			this.frames = frames;
			frameLength = frames.Length;
			this.fps = fps;
			this.repeat = repeat;
			active = true;
			index = 0;
		}
	}

	/*
	 * The sprite class takes a texture and changes it's source rectangle to match specific animation frame
	*/
	struct Sprite : IRender
	{
		public Texture2 texture;
		private SpriteSystem spriteSystem;
		public Queue<Animation> queuedAnimations;
		public Animation currentAnimation;
		public bool flipH ;
		float count;

		public string Type
		{
			get
			{
				return "Sprite";
			}
		}

		public Vector2 Position
		{
			get
			{
				return ((IRender)texture).Position;
			}

			set
			{
				((IRender)texture).Position = value;
			}
		}

		public float Rotation
		{
			get
			{
				return ((IRender)texture).Rotation;
			}

			set
			{
				((IRender)texture).Rotation = value;
			}
		}

		public Rectangle Bounds
		{
			get
			{
				return ((IRender)texture).Bounds;
			}

			set
			{
				((IRender)texture).Bounds = value;
			}
		}

		public Sprite(SpriteSystem s, Texture2 tex)
		{
			texture = tex;
			count = 0;
			queuedAnimations = new Queue<Animation>();
			flipH = false;
			currentAnimation = new Animation();
			spriteSystem = s;
		}

		public void UpdateAnimation(Global G)
		{
			//Animation Step
			if(currentAnimation.active)
			{
				count += G.dt;
				if(count >= 1f/currentAnimation.fps)
				{
					count -= 1f / currentAnimation.fps;
					currentAnimation.index++;
					if(currentAnimation.frameLength - 1 < currentAnimation.index && !currentAnimation.repeat)
					{
						if (queuedAnimations.Count == 0)
							currentAnimation.active = false;
						else
							currentAnimation = queuedAnimations.Dequeue();
					} else if(currentAnimation.frameLength - 1 < currentAnimation.index)
					{
						currentAnimation.index = 0;
					}
				}
			}
		}

		public void Render(SpriteBatch batch)
		{
			((IRender)texture).Render(batch);
		}

		public void Serialize(State state, BinaryWriter writer)
		{
			writer.Write(texture.Type);
			((IRender)texture).Serialize(state, writer);

			writer.Write(currentAnimation.active);
			writer.Write(currentAnimation.name);
			writer.Write(currentAnimation.index);
			writer.Write(currentAnimation.repeat);
			writer.Write(currentAnimation.fps);
		}

		public IRender DeserializeConstructor(State state, BinaryReader reader)
		{
			var desreg = state.G.getSystem<RegistrySystem<RenderDeserializer>>();
			var spriteSys = state.G.getSystem<SpriteSystem>();


			var type = reader.ReadString();
			var texture = (Texture2) desreg.Get(type)(state, reader);
			Sprite spr = new Sprite(spriteSys, texture);
			var active = reader.ReadBoolean();
			var name = reader.ReadString();
			var index = reader.ReadInt32();
			var repeat = reader.ReadBoolean();
			var fps = reader.ReadSingle();

			spr.currentAnimation = spriteSys.animations[name];
			spr.currentAnimation.active = active;
			spr.currentAnimation.index = index;
			spr.currentAnimation.repeat = repeat;
			spr.currentAnimation.fps = fps;

			return spr;
		}

		public void Update(State state)
		{

			((IRender)texture).Update(state);

			if (!currentAnimation.active)
				return;

			var index = currentAnimation.index;
			texture.srcRect = spriteSystem.frames[texture.textureName][currentAnimation.frames[index]];
			texture.resetRect();
			texture.effect = flipH ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

			UpdateAnimation(state.G);
		}

		public void Play(String name, int fps, bool repeat)
		{
			if (currentAnimation.name != name)
			{
				currentAnimation = spriteSystem.animations[name];
				currentAnimation.fps = fps;
				currentAnimation.repeat = repeat;


				texture.srcRect = spriteSystem.frames[texture.textureName][currentAnimation.frames[0]];
				texture.resetRect();
				texture.effect = flipH ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
			}
			else
			{
				currentAnimation.fps = fps;
				currentAnimation.repeat = repeat;
			}
		}

		public void SetFrame(int frameIndex)
		{

			currentAnimation.name = "";
			currentAnimation.fps = 1;
			currentAnimation.repeat = true;
			currentAnimation.index = 0;
			currentAnimation.frames = new int[] { frameIndex };
			currentAnimation.active = true;
			currentAnimation.frameLength = 1;

			texture.srcRect = spriteSystem.frames[texture.textureName][currentAnimation.frames[0]];
			texture.resetRect();
			texture.effect = flipH ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

		}
	}

	/*
	 * The sprite system holds animation data for each texture that the sprite class uses
	*/
	class SpriteSystem : BaseSystem
	{
		RenderSystem renderSys;
		//Frames of each texture
		public Dictionary<String, Rectangle[]> frames;
		//Animations: Contians indexes for each frame of animation
		public Dictionary<String, Animation> animations;

		public SpriteSystem(Global state) : base(state, "SpriteSystem")
		{
			frames = new Dictionary<string, Rectangle[]>();
			animations = new Dictionary<string, Animation>();
			renderSys = state.getSystem<RenderSystem>();
		}

		public override BaseSystem DeserializeConstructor(State state, string name)
		{
			return new SpriteSystem((Global) state);
		}

		public void AddAnimation(Animation animation)
		{
			animations[animation.name] = animation;
		}

		public void CreateGridFrames(String textureName, int frameWidth, int framwHeight)
		{
			var tex = _state.G.getSystem<MG.MonogameSystem>().getTexture(textureName);
			var w = tex.Width;
			var h = tex.Height;
			var wc = w / frameWidth;
			var hc = h / framwHeight;
			frames[textureName] = new Rectangle[wc*hc];
			for (int i = 0; i < wc*hc; ++i)
			{
				var x = (i % wc) * frameWidth;
				var y = ((i / wc) % hc) * framwHeight;
				frames[textureName][i] = new Rectangle(x, y, frameWidth, framwHeight);

			}
		}

		public void loadJSON(string path, string texture)
		{

			var stream  = TitleContainer.OpenStream(path);
			StreamReader r = new StreamReader(stream);

			JsonTextReader json = new JsonTextReader(r);
			JsonSerializer s = new JsonSerializer();

			List<Rectangle> frames = new List<Rectangle>();

			while (json.Read())
			{

				if (json.Value != null)
				{
					//Debug.Print(json.Value.ToString());
					if(json.Value.ToString() == "frame")
					{
						Rectangle frame = new Rectangle();
						json.Read();
						json.Read();
						var x = json.ReadAsInt32();
						if(x != null)
							frame.X = x.Value;
						json.Read();
						var y = json.ReadAsInt32();
						if (y != null)
							frame.Y = y.Value;
						json.Read();
						var Width = json.ReadAsInt32();
						if (Width != null)
							frame.Width = Width.Value;
						json.Read();
						var Height = json.ReadAsInt32();
						if (Height != null)
							frame.Height = Height.Value;
						frames.Add(frame);
					}

					if (json.Value.ToString() == "frameTags")
					{
						json.Read();
						json.Read();
						while (json.TokenType.ToString() != "EndArray")
						{
							
							json.Read();
							var name = json.ReadAsString();
							json.Read();
							var from = json.ReadAsInt32();
							json.Read();
							var to = json.ReadAsInt32();

							int asize = -from.Value + to.Value + 1;
							int[] indexes = new int[asize];
							for(int i = 0; i < asize; ++i)
							{
								indexes[i] = from.Value + i;
							}

							Animation a = new Animation(name, indexes, 0, false);
							animations[a.name] = a;
							while (json.TokenType.ToString() != "EndObject")
							{
								json.Read();
							}
							json.Read();
						}
					}
				}

			}
			//load the frames
			{
				this.frames[texture] = new Rectangle[frames.Count];
				int i = 0;
				foreach (var f in frames)
				{
					this.frames[texture][i] = f;
					i++;
				}
			}
		}

		public override void SerializeSystem(BinaryWriter writer)
		{
		}

		public override void DeserializeSystem(BinaryReader reader)
		{
		}
	}

	internal struct JsonRect
	{
		public int x;
		public int y;
		public int w;
		public int h;

	}
	internal struct JsonPoint
	{
		public int x;
		public int y;
	}

	internal struct JsonFrame
	{
		public string filename;
		public JsonRect frame;
		public bool rotated;
		public bool trimmed;
		public JsonRect spriteSourceSize;
		public JsonPoint sourceSize;
		public int duration;
	}
}