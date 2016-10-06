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
using MG;

namespace CS.Components
{

	interface IRender
	{
		String Type { get; }
		Vector2 Position { get; set; }
		float Rotation { get; set; }
		Rectangle Bounds { get; set; }

		void Render(SpriteBatch batch);
		void Update(State state);
		void Serialize(State state, BinaryWriter writer);
		IRender DeserializeConstructor(State state, BinaryReader reader);
	}

	class RenderCollection : IRender
	{
		public RenderCollection(params IRender[] renders)
		{
			this.renders = renders;
			updateBounds();
		}

		public IRender[] renders;

		public Vector2 Position { get; set; }
		public float Rotation { get; set; }

		public string Type
		{
			get
			{
				return "RenderCollection";
			}
		}

		Rectangle _bounds;
		public Rectangle Bounds { get { updateBounds();  return _bounds;  } set { _bounds = value;  } }

		public IRender DeserializeConstructor(State state, BinaryReader reader)
		{
			var desreg = state.G.getSystem<RegistrySystem<RenderDeserializer>>();
			var length = reader.ReadInt32();

			var rendrs = new IRender[length];
			for(int i = 0; i < length; ++i)
			{
				var type = reader.ReadString();
				rendrs[i] = desreg.Get(type)(state, reader);
			}
			return new RenderCollection(rendrs);
		}

		public void Render(SpriteBatch batch)
		{
			for(int i = 0; i < renders.Length; ++i)
			{
				renders[i].Position = Position;
				renders[i].Rotation = Rotation;
				renders[i].Render(batch);
			}
		}

		public void Serialize(State state, BinaryWriter writer)
		{
			var desreg = state.G.getSystem<RegistrySystem<RenderDeserializer>>();

			writer.Write(renders.Length);
			foreach (var render in renders)
			{
				desreg.Register(render.Type, render.DeserializeConstructor);
				writer.Write(render.Type);
				render.Serialize(state, writer);
			}
		}

		public void Update(State state)
		{
			for (int i = 0; i < renders.Length; ++i)
			{
				renders[i].Update(state);
			}
		}

		private void updateBounds()
		{
			for (int i = 0; i < renders.Length; ++i)
			{
				var rbounds = renders[i].Bounds;
				if (rbounds.X < _bounds.X)
					_bounds.X = rbounds.X;
				if (rbounds.Y < _bounds.Y)
					_bounds.Y = rbounds.Y;
				if (rbounds.Width > _bounds.Width)
					_bounds.Width = rbounds.Width;
				if (rbounds.Height > _bounds.Height)
					_bounds.Height = rbounds.Height;
			}
		}
	}


	class Texture2 : IRender
	{
		public Texture2D texture;
		public String textureName;
		public float layerDepth;
		public Vector2 offset;
		public Vector2 scale;
		public Vector2 origin;
		public Vector2 position;
		public float rotation = 0;
		public Rectangle textureRect;
		public Rectangle srcRect;
		public SpriteEffects effect;
		public bool useCamera = true;
		public int layer = 0;
		public Color color = Color.White;

		public Texture2(Global G, String textureString, float layer = 0.9f)
		{
			this.texture = G.getSystem<MG.MonogameSystem>().getTexture(textureString);
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
			batch.Draw(texture, position: position, sourceRectangle: srcRect, scale: scale, layerDepth: layerDepth, rotation: rotation, origin: origin, effects: effect, color:color);
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
			textureRect.Width = w;
			textureRect.Height = h;
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

		public string Type
		{
			get
			{
				return "Texture2D";
			}
		}

		public Vector2 Position
		{
			get
			{
				return position;
			}

			set
			{
				position = value;
			}
		}

		public float Rotation
		{
			get { return rotation; }
			set { rotation = value; }
		}

		public Rectangle Bounds
		{
			get
			{
				return getRect();
			}

			set { Bounds = value; }
		}

		public void resetRect()
		{
			origin = new Vector2(srcRect.Width / 2f, srcRect.Height / 2f);
			textureRect.Width = (int)((float)srcRect.Width * scale.X);
			textureRect.Height = (int)((float)srcRect.Height * scale.Y);
		}

		public void Render(SpriteBatch batch)
		{
			position += offset;
			resetRect();
			batch.Draw(texture, position: position, sourceRectangle: srcRect, scale: scale, layerDepth: layerDepth, rotation: rotation, origin: origin, effects: effect, color:color);
		}

		public void Serialize(State state, BinaryWriter writer)
		{
			writer.Write(textureName);
			writer.Write(layerDepth);
			writer.Write(offset.X);
			writer.Write(offset.Y);
			writer.Write(scale.X);
			writer.Write(scale.Y);
			writer.Write(origin.X);
			writer.Write(origin.Y);
			writer.Write(textureRect.X);
			writer.Write(textureRect.Y);
			writer.Write(textureRect.Width);
			writer.Write(textureRect.Height);
			writer.Write(srcRect.X);
			writer.Write(srcRect.Y);
			writer.Write(srcRect.Width);
			writer.Write(srcRect.Height);
		}

		public IRender DeserializeConstructor(State state, BinaryReader reader)
		{
			var name = reader.ReadString();
			var layer = reader.ReadInt32();
			Texture2 t = new Texture2(state.G, name, layer);
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

		public Rectangle getRect()
		{
			Rectangle rect = textureRect;
			rect.X = (int)(Position.X - rect.Width / 2f);
			rect.Y = (int)(Position.Y - rect.Height / 2f);

			return rect;
		}

		public void Update(State state)
		{
		}
	}

	class RenderTargets : Data<RenderTarget2D>, ISysRenderable
	{
		SpriteBatch _batch;
		public RenderTargets(State state) : base(state, "RenderTargets")
		{
			_batch = new SpriteBatch(state.G.getSystem<MG.MonogameSystem>().Game.GraphicsDevice);

		}

		public override BaseSystem DeserializeConstructor(State state, string name)
		{
			throw new NotImplementedException();
		}

		public override void DeserializeSystem(BinaryReader reader)
		{
			throw new NotImplementedException();
		}
		Color c = Color.White;
		public void Render(Global G)
		{
			cleanup();
			_batch.Begin(blendState: BlendState.AlphaBlend);
			for(int i = size-1; i >= 0; --i)
			{
				if(used[i] == -1)
					continue;
				_batch.Draw(data[i], data[i].Bounds, c);
			}
			_batch.End();
		}

		public override void SerializeSystem(BinaryWriter writer)
		{
			throw new NotImplementedException();
		}

		protected override RenderTarget2D DeserailizeData(BinaryReader reader)
		{
			throw new NotImplementedException();
		}

		protected override void SerailizeData(ref RenderTarget2D component, BinaryWriter writer)
		{
			throw new NotImplementedException();
		}
	}

	delegate IRender RenderDeserializer(State state, BinaryReader reader);
	class RenderSystem : ComponentSystem<IRender>, ISysUpdateable, ISysRenderable
	{
		TransformSystem transform;
		PhysicsSystem physics;
		RegistrySystem<RenderDeserializer> deserializeReg;
		Effect effect;
		float timer = 0;
		RenderTarget2D target2D;
		RenderTargets targets;
		int targetIndex;

		public RenderSystem(State state) : base(state, "RenderSystem")
		{
			_batch = new SpriteBatch(state.G.getSystem<MG.MonogameSystem>().Game.GraphicsDevice);
			this.transform = state.getSystem<TransformSystem>();
			this.physics = state.getSystem<PhysicsSystem>();
			deserializeReg = state.G.getSystem<RegistrySystem<RenderDeserializer>>();
			if(deserializeReg == null)
			{
				deserializeReg = new RegistrySystem<RenderDeserializer>(state.G, "RenderDeserializer");
			}
			//effect = state.G.getSystem<MG.MonogameSystem>().Game.Content.Load<Effect>("testeffect");
			target2D = new RenderTarget2D(_batch.GraphicsDevice, _batch.GraphicsDevice.PresentationParameters.BackBufferWidth,
				_batch.GraphicsDevice.PresentationParameters.BackBufferHeight,
				false,
				SurfaceFormat.Color,
				DepthFormat.Depth24);
			targets = state.G.getSystem<RenderTargets>();
			targetIndex = targets.AddData(target2D);
			targets.RegisterUse(targetIndex);
			
		}

		public override void Deactivate()
		{
			targets.DeregisterUse(targetIndex);
			base.Deactivate();
		}

		public override int AddComponent(int id, IRender component)
		{
			if(!deserializeReg.Has(component.Type))
				deserializeReg.Register(component.Type, component.DeserializeConstructor);
			return base.AddComponent(id, component);
		}

		public override void SetComponent(int id, IRender component)
		{
			if (!deserializeReg.Has(component.Type))
				deserializeReg.Register(component.Type, component.DeserializeConstructor);
			base.SetComponent(id, component);
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
			_batch.GraphicsDevice.SetRenderTarget(target2D);
			_batch.GraphicsDevice.Clear(Color.Transparent);

			_batch.Begin(sortMode: SpriteSortMode.Deferred, samplerState: SamplerState.PointWrap, transformMatrix: _state.getSystem<MG.Camera>().matrix, depthStencilState: DepthStencilState.DepthRead);
			for (int i = 0; i < size; ++i)
			{
				if (entityIDs[i] == -1)
					continue;
				components[i].Render(_batch);

			}
			_batch.End();

			_batch.GraphicsDevice.SetRenderTarget(null);
		}

		public override BaseSystem DeserializeConstructor(State state, string name)
		{
			return new RenderSystem(state);
		}

		public override void SerializeSystem(BinaryWriter writer)
		{
		}

		public override void DeserializeSystem(BinaryReader reader)
		{
		}

		public void Update(Global G)
		{
			for (int i = 0; i < size; ++i)
			{
				if (entityIDs[i] == -1)
					continue;

				var textureC = components[i];
				var transfromIndex = _state.getComponentIndex(entityIDs[i], transform.Index);
				if (transfromIndex != -1)
				{
					var transformC = transform.getComponent(transfromIndex);
					textureC.Position = transformC.position;
				}

				//effect.Parameters["SpriteTexture"].SetValue(textureC.texture);
				int pindex = -1;
				if (physics != null)
					pindex = _state.getComponentIndex(entityIDs[i], physics.Index);


				if (pindex != -1)
				{
					var p = physics.getComponent(pindex);
					textureC.Rotation = p.Rotation;
				}
				components[i] = textureC;
				components[i].Update(_state);
			}
		}

		protected override void SerailizeComponent(ref IRender component, BinaryWriter writer)
		{
			writer.Write(component.Type);
			component.Serialize(_state, writer);
		}

		protected override IRender DeserailizeComponent(BinaryReader reader)
		{
			var type = reader.ReadString();
			return deserializeReg.Get(type)(_state, reader);
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
				var content = _state.G.getSystem<MG.MonogameSystem>().Game.Content;
				var font = content.Load<SpriteFont>(name);
				fonts[name] = font;
				return font;
			}  else
			{
				return fonts[name];
			}
		}

	}

	enum Align
	{
		LEFT, CENTER, RIGHT
	}

	struct Text : IRender
	{
		public String String { get; set; }
		//public Rectangle Bounds { get; set; }
		public Vector2 Position { get; set; }
		public SpriteFont Font { get; private set; }
		public string FontName { get; private set; }
		public Color Color { get; set; }
		public float Layer { get; set; }
		public Align Alignment { get; set; }

		public string Type
		{
			get
			{
				return "Text";
			}
		}

		public float Rotation{get;set;}

		public Rectangle Bounds
		{
			get;
			set;
		}

		public Text(string text, Vector2 pos, FontSystem fsys, string fontName)
		{
			String = text;
			Position = pos;
			Font = null;
			FontName = "";
			Color = Color.White;
			Layer = 0.9f;
			Alignment = Align.CENTER;
			Rotation = 0;

			Font = fsys.LoadFont(fontName);
			FontName = fontName;

			Bounds = new Rectangle(Position.ToPoint(), Font.MeasureString(String).ToPoint());
		}

		public void SetFont(FontSystem fontSys, string name)
		{
			Font = fontSys.LoadFont(name);
			FontName = name;
		}

		public void Render(SpriteBatch batch)
		{
			Vector2 textMiddlePoint = Vector2.Zero;
			switch (Alignment)
			{
				case Align.CENTER:
					textMiddlePoint = Font.MeasureString(String) / 2;
					break;
				case Align.RIGHT:
					textMiddlePoint = Font.MeasureString(String);
					break;
				case Align.LEFT:
					break;
			}
			batch.DrawString(Font, String, Position - textMiddlePoint, Color, 0, Vector2.Zero, 1, SpriteEffects.None, Layer);
		}

		public void Serialize(State state, BinaryWriter writer)
		{
			writer.Write(String);
			writer.Write(Position.X);
			writer.Write(Position.Y);
			writer.Write(FontName);
			writer.Write(Color.R);
			writer.Write(Color.G);
			writer.Write(Color.B);
			writer.Write(Color.A);
			writer.Write(Rotation);
		}

		public IRender DeserializeConstructor(State state, BinaryReader reader)
		{
			Text text = new Text();
			text.String = reader.ReadString();
			var x = reader.ReadSingle();
			var y = reader.ReadSingle();
			text.Position = new Vector2(x, y);
			var fontName = reader.ReadString();
			text.SetFont(state.G.getSystem<FontSystem>(), fontName);
			var r = reader.ReadByte();
			var g = reader.ReadByte();
			var b = reader.ReadByte();
			var a = reader.ReadByte();
			text.Color = new Color(r, g, b, a);
			text.Rotation = reader.ReadSingle();
			return text;
		}

			public void Update(State state)
			{
			}
		}
	/*
	class TextRenderingSystem : ComponentSystem<Text>
	{
		FontSystem fontSys;
		TransformSystem tranSys;
		public TextRenderingSystem(State state) : base(state, "TextRenderingSystem")
		{
			Batch = new SpriteBatch(state.G.getSystem<MG.MonogameSystem>().Game.GraphicsDevice);
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

		public void Render(ref SpriteBatch batch)
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
				Vector2 textMiddlePoint = Vector2.Zero;
				switch(textC.Alignment)
				{
					case Align.CENTER:
						textMiddlePoint = textC.Font.MeasureString(textC.String) / 2;
						break;
					case Align.RIGHT:
						textMiddlePoint = textC.Font.MeasureString(textC.String);
						break;
					case Align.LEFT:
						break;
				}
				batch.DrawString(textC.Font, textC.String, textC.Position - textMiddlePoint, textC.Color, 0, Vector2.Zero, 1, SpriteEffects.None, textC.Layer);
			}
		}

		public override void SerializeSystem(BinaryWriter writer)
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
	}*/
}