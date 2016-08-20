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
	class Sprite
	{
		public String textureName;
		public Queue<Animation> queuedAnimations;
		public Animation currentAnimation;
		public bool flipH = false;
		float count;
		public Sprite(String textureName)
		{
			this.textureName = textureName;
			count = 0;
			queuedAnimations = new Queue<Animation>();
		}

		public void Update(Global G)
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

	}

	/*
	 * The sprite system holds animation data for each texture that the sprite class uses
	*/
	class SpriteSystem : ComponentSystem<Sprite>, ISysUpdateable
	{
		TextureSystem textureSys;
		//Frames of each texture
		Dictionary<String, Rectangle[]> frames;
		//Animations: Contians indexes for each frame of animation
		Dictionary<String, Animation> animations;

		public SpriteSystem(State state) : base(state, "SpriteSystem")
		{
			textureSys = state.getSystem<TextureSystem>();
			frames = new Dictionary<string, Rectangle[]>();
			animations = new Dictionary<string, Animation>();
		}

		public override BaseSystem DeserializeConstructor(State state, string name)
		{
			return new SpriteSystem(state);
		}

		public void Update(Global G)
		{
			
			for(int i = 0; i < size; ++i)
			{
				if (entityIDs[i] == -1)
					continue;
				if (!components[i].currentAnimation.active)
					continue;

				int textureIndex = -1;
				if(textureSys.ContainsEntity(entityIDs[i], ref textureIndex))
				{
					var tex = textureSys.getComponent(textureIndex);
					var texName = components[i].textureName;
					var index = components[i].currentAnimation.index;
					tex.srcRect = frames[texName][components[i].currentAnimation.frames[index]];
					tex.resetRect();
					tex.effect = components[i].flipH ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
				}

				components[i].Update(G);
			}
		}

		public void Play(String name, int eID, int fps, bool repeat)
		{
			var index = _state.getComponentIndex(eID, systemIndex);
			if (index == -1)
				return;

			if (components[index].currentAnimation.name != name)
			{
				components[index].currentAnimation = animations[name];
				components[index].currentAnimation.fps = fps;
				components[index].currentAnimation.repeat = repeat;

				int textureIndex = -1;
				if (textureSys.ContainsEntity(eID, ref textureIndex))
				{
					var tex = textureSys.getComponent(textureIndex);
					var texName = components[index].textureName;

					tex.srcRect = frames[texName][components[index].currentAnimation.frames[0]];
					tex.resetRect();
					tex.effect = components[index].flipH ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
				}
			}
		}

		public void AddAnimation(Animation animation)
		{
			animations[animation.name] = animation;
		}

		public void CreateGridFrames(String textureName, int frameWidth, int framwHeight)
		{
			var tex = _state.G.getTexture(textureName);
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

		public override void Serialize(BinaryWriter writer)
		{
			base.Serialize(writer);
			for (int i = 0; i < size; ++i)
			{
				writer.Write(components[i].textureName);
				writer.Write(components[i].currentAnimation.active);
				writer.Write(components[i].currentAnimation.name);
				writer.Write(components[i].currentAnimation.index);
				writer.Write(components[i].currentAnimation.repeat);
				writer.Write(components[i].currentAnimation.fps);
			}
		}

		public override void Deserialize(BinaryReader reader)
		{
			base.Deserialize(reader);
			components = new Sprite[size];
			for (int i = 0; i < size; ++i)
			{
				string textureName = reader.ReadString();
				components[i] = new Sprite(textureName);
				var active = reader.ReadBoolean();
				var name = reader.ReadString();
				var index = reader.ReadInt32();
				var repeat = reader.ReadBoolean();
				var fps = reader.ReadSingle();

				components[i].currentAnimation = animations[name];
				components[i].currentAnimation.active = active;
				components[i].currentAnimation.index = index;
				components[i].currentAnimation.repeat = repeat;
				components[i].currentAnimation.fps = fps;
			}
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