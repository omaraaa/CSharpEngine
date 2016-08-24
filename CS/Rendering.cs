using System.Collections.Generic;
using System.Collections;
using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using Microsoft.Xna.Framework.Graphics;

using CS;

using FarseerPhysics.Dynamics;
using FarseerPhysics.Collision;
using FarseerPhysics.Collision.Shapes;
using FarseerPhysics.Common;
using FarseerPhysics.Factories;
using FarseerPhysics.Controllers;
using FarseerPhysics;

using Lidgren.Network;

using MoonSharp.Interpreter;

namespace CS.Components
{
	class Texture2
	{
		Texture2D texture;
		public String textureName;
		public float layerDepth;
		public Vector2 offset;
		public Vector2 scale;
		public Vector2 origin;
		public Rectangle textureRect;
		public Rectangle srcRect;
		public SpriteEffects effect;
		public bool useCamera = true;

		public Texture2(Global G, String textureString, float layer = 0.9f)
		{
			this.texture = G.getTexture(textureString);
			textureName = textureString;
			this.layerDepth = layer;
			this.offset = new Vector2(0, 0);

			this.scale = new Vector2(1, 1);

			origin = new Vector2(texture.Width / 2f, texture.Height / 2f);
			textureRect = new Rectangle(0, 0, texture.Width, texture.Height);
			srcRect = new Rectangle(0, 0, texture.Width, texture.Height);
			effect = SpriteEffects.None;

		}

		public void Render(SpriteBatch batch, Vector2 position, float rotation = 0)
		{
			position += offset;
			resetRect();
			batch.Draw(texture, position: position, sourceRectangle: srcRect, scale: scale, layerDepth: layerDepth, rotation: rotation, origin: origin, effects: effect);
		}

		public void setScale(float x, float y)
		{
			scale = new Vector2(x, y);
			textureRect.Width = (int)((float)srcRect.Width * x);
			textureRect.Height = (int)((float)srcRect.Height * y);
		}

		public void setRect(int w, int h)
		{
			scale.X = ((float)w) / textureRect.Width;
			scale.Y = ((float)h) / textureRect.Height;
		}

		public int Width
		{
			get
			{
				return texture.Width;
			}
		}

		public int Height
		{
			get
			{
				return texture.Height;
			}
		}

		public void resetRect()
		{
			origin = new Vector2(srcRect.Width / 2f, srcRect.Height / 2f);
			textureRect.Width = (int)((float)srcRect.Width * scale.X);
			textureRect.Height = (int)((float)srcRect.Height * scale.Y);
		}
	}



	class TextureSystem : ComponentSystem<Texture2>, ISysRenderable
	{
		TransformSystem transform;
		PhysicsSystem physics;

		public TextureSystem(State state) : base(state, "TextureSystem")
		{
			_batch = new SpriteBatch(state.G.game.GraphicsDevice);
			this.transform = state.getSystem<TransformSystem>();
			this.physics = state.getSystem<PhysicsSystem>();
		}

		SpriteBatch _batch;
		public SpriteBatch Batch
		{
			get
			{
				return _batch;
			}
		}

		uint _renderIndex;
		public uint RenderIndex
		{
			get
			{
				return _renderIndex;
			}
		}

		public void Render(Global G)
		{
			_batch.Begin(sortMode: SpriteSortMode.BackToFront, samplerState: SamplerState.PointWrap, transformMatrix: _state.camera.matrix);
			for (int i = 0; i < size; ++i)
			{
				if (entityIDs[i] == -1)
					continue;

				var transfromIndex = _state.getComponentIndex(entityIDs[i], transform.systemIndex);
				var transformC = transform.getComponent(transfromIndex);
				var textureC = components[i];

				int pindex = -1;
				if (physics != null)
					pindex = _state.getComponentIndex(entityIDs[i], physics.systemIndex);
				if (pindex != -1)
				{
					var p = physics.getComponent(pindex);
					textureC.Render(_batch, ConvertUnits.ToDisplayUnits(p.Position), p.Rotation);
				}
				else
				{
					textureC.Render(_batch, transformC.position);
				}

			}
			_batch.End();
		}

		public Rectangle getRect(int id)
		{
			var index = _state.getComponentIndex(id, systemIndex);
			if (index == -1)
				return Rectangle.Empty;

			var textureC = components[index];
			Rectangle rect = textureC.textureRect;

			int transfromIndex = -1;
			if (transform.ContainsEntity(id, ref transfromIndex))
			{
				var transformC = transform.getComponent(transfromIndex);
				rect.X = (int)(transformC.position.X - rect.Width / 2f + 8);
				rect.Y = (int)(transformC.position.Y - rect.Height / 2f + 8);
			}

			return rect;
		}

		public override BaseSystem DeserializeConstructor(State state, string name)
		{
			return new TextureSystem(state);
		}


		protected override void SerailizeComponent(ref Texture2 t, BinaryWriter writer)
		{
			writer.Write(t.textureName);
			writer.Write(t.layerDepth);
			writer.Write(t.offset.X);
			writer.Write(t.offset.Y);
			writer.Write(t.scale.X);
			writer.Write(t.scale.Y);
			writer.Write(t.origin.X);
			writer.Write(t.origin.Y);
			writer.Write(t.textureRect.X);
			writer.Write(t.textureRect.Y);
			writer.Write(t.textureRect.Width);
			writer.Write(t.textureRect.Height);
			writer.Write(t.srcRect.X);
			writer.Write(t.srcRect.Y);
			writer.Write(t.srcRect.Width);
			writer.Write(t.srcRect.Height);
		}

		protected override Texture2 DeserailizeComponent(BinaryReader reader)
		{
			var name = reader.ReadString();
			var layer = reader.ReadInt32();
			Texture2 t = new Texture2(_state.G, name, layer);
			t.offset.X = reader.ReadSingle();
			t.offset.Y = reader.ReadSingle();
			t.scale.X = reader.ReadSingle();
			t.scale.Y = reader.ReadSingle();
			t.origin.X = reader.ReadSingle();
			t.origin.Y = reader.ReadSingle();
			t.textureRect.X = reader.ReadInt32();
			t.textureRect.Y = reader.ReadInt32();
			t.textureRect.Width = reader.ReadInt32();
			t.textureRect.Height = reader.ReadInt32();
			t.srcRect.X = reader.ReadInt32();
			t.srcRect.Y = reader.ReadInt32();
			t.srcRect.Width = reader.ReadInt32();
			t.srcRect.Height = reader.ReadInt32();
			return t;
		}

		public override void SerializeSystem(BinaryWriter writer)
		{
		}

		public override void DeserializeSystem(BinaryReader reader)
		{
		}
	}

	class FontSystem : BaseSystem
	{
		Dictionary<string, SpriteFont> fonts;

		public FontSystem(State state) : base(state, "FontSystem")
		{
			fonts = new Dictionary<string, SpriteFont>();
		}

		public override BaseSystem DeserializeConstructor(State state, string name)
		{
			return new FontSystem(state);
		}

		public override void DeserializeSystem(BinaryReader reader)
		{
			var count = reader.ReadInt32();
			for(int i = 0; i < count; ++i)
			{
				var name = reader.ReadString();
				LoadFont(name);
			}
		}

		public override void SerializeSystem(BinaryWriter writer)
		{
			writer.Write(fonts.Count);
			foreach(var pair in fonts)
			{
				writer.Write(pair.Key);
			}
		}

		public SpriteFont LoadFont(string name)
		{
			if (!fonts.ContainsKey(name))
			{
				var content = _state.G.game.Content;
				var font = content.Load<SpriteFont>(name);
				fonts[name] = font;
				return font;
			}  else
			{
				return fonts[name];
			}
		}

	}

	class Text
	{
		public String String { get; set; }
		public Rectangle Bounds { get; set; }
		public Vector2 Position { get; set; }
		public SpriteFont Font { get; private set; }
		public string FontName { get; private set; }
		public Color Color { get; set; }

		public void SetFont(FontSystem fontSys, string name)
		{
			Font = fontSys.LoadFont(name);
			FontName = name;
		}
	}

	class TextRenderingSystem : ComponentSystem<Text>, ISysUpdateable, ISysRenderable
	{
		FontSystem fontSys;
		TransformSystem tranSys;
		public TextRenderingSystem(State state) : base(state, "TextRenderingSystem")
		{
			Batch = new SpriteBatch(state.G.game.GraphicsDevice);
			fontSys = state.G.getSystem<FontSystem>();
			tranSys = state.getSystem<TransformSystem>();
		}

		public SpriteBatch Batch
		{
			get;
			private set;
		}

		public override BaseSystem DeserializeConstructor(State state, string name)
		{
			return new TextRenderingSystem(state);
		}

		public override void DeserializeSystem(BinaryReader reader)
		{
		}

		public void Render(Global G)
		{
			for(int i = 0; i < size; ++i)
			{
				if (entityIDs[i] == -1)
					continue;

				var textC = components[i];
				int transIndex = -1;
				if(tranSys.ContainsEntity(entityIDs[i], ref transIndex))
				{
					var trans = tranSys.getComponent(transIndex);
					textC.Position = trans.position;
				}
				Batch.Begin();
				Batch.DrawString(textC.Font, textC.String, textC.Position, textC.Color);
				Batch.End();
			}
		}

		public override void SerializeSystem(BinaryWriter writer)
		{
		}

		public void Update(Global G)
		{
			
		}

		protected override Text DeserailizeComponent(BinaryReader reader)
		{
			Text text = new Text();
			text.String = reader.ReadString();
			var x = reader.ReadSingle();
			var y = reader.ReadSingle();
			text.Position = new Vector2(x, y);
			var fontName = reader.ReadString();
			text.SetFont(fontSys, fontName);
			var r = reader.ReadByte();
			var g = reader.ReadByte();
			var b = reader.ReadByte();
			var a = reader.ReadByte();
			text.Color = new Color(r, g, b, a);

			return text;
		}

		protected override void SerailizeComponent(ref Text component, BinaryWriter writer)
		{
			writer.Write(component.String);
			writer.Write(component.Position.X);
			writer.Write(component.Position.Y);
			writer.Write(component.FontName);
			writer.Write(component.Color.R);
			writer.Write(component.Color.G);
			writer.Write(component.Color.B);
			writer.Write(component.Color.A);
		}
	}
}