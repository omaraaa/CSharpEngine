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

	

	public interface IRender
	{
		void Render(SpriteBatch batch);
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

		public void resetRect()
		{
			origin = new Vector2(srcRect.Width / 2f, srcRect.Height / 2f);
			textureRect.Width = (int)((float)srcRect.Width * scale.X);
			textureRect.Height = (int)((float)srcRect.Height * scale.Y);
		}

		public void Render(SpriteBatch batch)
		{
			resetRect();
			batch.Draw(texture, position: position, sourceRectangle: srcRect, scale: scale, layerDepth: layerDepth, rotation: rotation, origin: origin, effects: effect);
		}
	}

	class Layer
	{
		public Effect effect;
		public int index;
		public List<IRender> renders;
		public SpriteSortMode spriteSort = SpriteSortMode.BackToFront;
		public BlendState blendState = BlendState.NonPremultiplied;
		public SamplerState samplerState = SamplerState.PointWrap;
		public DepthStencilState depthState = DepthStencilState.Default;
		public RasterizerState rasterState = RasterizerState.CullNone;
		public Camera camera;

		public Layer()
		{
			camera = null;
			renders = new List<IRender>();
			effect = null;
		}
	}
	class Renderer : BaseSystem, ISysRenderable
	{

		SortedDictionary<int, Layer> layers;
		SpriteBatch batch;
		MonogameSystem monogame;
		public Renderer(State state) : base(state, "Renderer")
		{
			monogame = state.G.getSystem<MonogameSystem>();
			layers = new SortedDictionary<int, Layer>();
			batch = new SpriteBatch(monogame.Game.GraphicsDevice);
		}

		public void PushRender(IRender render, int layerIndex)
		{
			if (layers.ContainsKey(layerIndex))
			{
				var layer = layers[layerIndex];
				layer.renders.Add(render);
			} else
			{
				var layer = new Layer();
				layer.renders.Add(render);
				AddLayer(layerIndex, layer);
			}
		}

		public void RemoveRender(IRender render, int layerIndex)
		{
			if (layers.ContainsKey(layerIndex))
			{
				var layer = layers[layerIndex];
				layer.renders.Remove(render);
			}
		}

		public void AddLayer(int index, Layer l)
		{
			l.index = index;
			layers[index] = l;
		}

		public override BaseSystem DeserializeConstructor(State state, string name)
		{
			throw new NotImplementedException();
		}

		public override void DeserializeSystem(BinaryReader reader)
		{
			throw new NotImplementedException();
		}

		public override void SerializeSystem(BinaryWriter writer)
		{
			throw new NotImplementedException();
		}

		void ISysRenderable.Render(Global G)
		{
			foreach(var pair in layers)
			{
				var layer = pair.Value;
				if(layer.camera != null)
					batch.Begin(layer.spriteSort, layer.blendState, layer.samplerState, layer.depthState, layer.rasterState, layer.effect, layer.camera.matrix);
				else
					batch.Begin(layer.spriteSort, layer.blendState, layer.samplerState, layer.depthState, layer.rasterState, layer.effect);


				foreach (var render in layer.renders)
					render.Render(batch);

				batch.End();
			}
		}
	}

	class TextureSystem : ComponentSystem<Texture2>, ISysUpdateable
	{
		TransformSystem transform;
		PhysicsSystem physics;
		TextRenderingSystem textrender;
		Renderer renderer;
		Effect effect;
		float timer = 0;

		public TextureSystem(State state) : base(state, "TextureSystem")
		{
			_batch = new SpriteBatch(state.G.getSystem<MG.MonogameSystem>().Game.GraphicsDevice);
			this.transform = state.getSystem<TransformSystem>();
			this.physics = state.getSystem<PhysicsSystem>();
			textrender = state.getSystem<TextRenderingSystem>();
			renderer = state.G.getSystem<Renderer>();
			//effect = state.G.getSystem<MG.MonogameSystem>().Game.Content.Load<Effect>("testeffect");
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
			timer += G.dt;


			//effect.Parameters["time"].SetValue(timer);

			_batch.Begin(sortMode: SpriteSortMode.BackToFront, samplerState: SamplerState.PointWrap, transformMatrix: _state.getSystem<MG.Camera>().matrix, depthStencilState: DepthStencilState.DepthRead);
			for (int i = 0; i < size; ++i)
			{
				if (entityIDs[i] == -1)
					continue;

				var transfromIndex = _state.getComponentIndex(entityIDs[i], transform.systemIndex);
				var transformC = transform.getComponent(transfromIndex);
				var textureC = components[i];

				//effect.Parameters["SpriteTexture"].SetValue(textureC.texture);
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
			if(textrender != null)
				textrender.Render(ref _batch);
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
				rect.X = (int)(transformC.position.X - rect.Width / 2f);
				rect.Y = (int)(transformC.position.Y - rect.Height / 2f);
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

		public void Update(Global G)
		{
			for (int i = 0; i < size; ++i)
			{
				if (entityIDs[i] == -1)
					continue;

				var transfromIndex = _state.getComponentIndex(entityIDs[i], transform.systemIndex);
				var transformC = transform.getComponent(transfromIndex);
				var textureC = components[i];

				//effect.Parameters["SpriteTexture"].SetValue(textureC.texture);
				int pindex = -1;
				if (physics != null)
					pindex = _state.getComponentIndex(entityIDs[i], physics.systemIndex);


				textureC.position = transformC.position;
				if (pindex != -1)
				{
					var p = physics.getComponent(pindex);
					textureC.rotation = p.Rotation;
				}

				
			}
		}

		public override int AddComponent(int id, Texture2 component)
		{
			renderer.PushRender(component, component.layer);
			return base.AddComponent(id, component);
		}

		public override void RemoveEntity(int id)
		{
			renderer.RemoveRender(components[id], components[id].layer);
			base.RemoveEntity(id);
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

	class Text : IRender
	{
		public String String { get; set; }
		//public Rectangle Bounds { get; set; }
		public Vector2 Position { get; set; }
		public SpriteFont Font { get; private set; }
		public string FontName { get; private set; }
		public Color Color { get; set; }
		public float depth { get; set; }
		public int layer = 0;
		public Align Alignment { get; set; }
		
		public Text(string text, Vector2 pos, FontSystem fsys, string fontName)
		{
			String = text;
			Position = pos;
			Font = null;
			FontName = "";
			Color = Color.White;
			depth = 0.9f;
			Alignment = Align.CENTER;

			SetFont(fsys, fontName);
		}

		public Text()
		{
			String = "";
			Position = Vector2.Zero;
			Font = null;
			FontName = "";
			Color = Color.White;
			depth = 0.9f;
			Alignment = Align.CENTER;
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
			batch.DrawString(Font, String, Position - textMiddlePoint, Color, 0, Vector2.Zero, 1, SpriteEffects.None, depth);
		}
	}

	class TextRenderingSystem : ComponentSystem<Text>, ISysUpdateable
	{
		FontSystem fontSys;
		TransformSystem tranSys;
		Renderer renderer;
		public TextRenderingSystem(State state) : base(state, "TextRenderingSystem")
		{
			Batch = new SpriteBatch(state.G.getSystem<MG.MonogameSystem>().Game.GraphicsDevice);
			fontSys = state.G.getSystem<FontSystem>();
			tranSys = state.getSystem<TransformSystem>();
			renderer = state.G.getSystem<Renderer>();
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
			
		}

		public override void SerializeSystem(BinaryWriter writer)
		{
		}

		public void Update(Global G)
		{
			for (int i = 0; i < size; ++i)
			{
				if (entityIDs[i] == -1)
					continue;

				var textC = components[i];
				int transIndex = -1;
				if (tranSys.ContainsEntity(entityIDs[i], ref transIndex))
				{
					var trans = tranSys.getComponent(transIndex);
					textC.Position = trans.position;
				}

			}
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

		public override int AddComponent(int id, Text component)
		{
			renderer.PushRender(component, component.layer);
			return base.AddComponent(id, component);
		}

		public override void RemoveEntity(int id)
		{
			renderer.RemoveRender(components[id], components[id].layer);
			base.RemoveEntity(id);
		}
	}
}