using System.Collections.Generic;
using System;
using System.IO;

using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using Microsoft.Xna.Framework;
using MG;

namespace CS.Components
{
	

	class MessageSystem : BaseSystem, ISysUpdateable
	{
		Dictionary<string, string> messages;

		public MessageSystem(State state) : base(state, "MessageSystem")
		{
			messages = new Dictionary<string, string>();
		}

		public override BaseSystem DeserializeConstructor(State state, string name)
		{
			return new MessageSystem(state);
		}

		public override void DeserializeSystem(BinaryReader reader)
		{
		}

		public override void SerializeSystem(BinaryWriter writer)
		{
		}

		public void Update(Global G)
		{
			messages.Clear();
		}

		public void Set(string key, string value)
		{
			messages[key] = value;
		}

		public string Get(string key)
		{
			return messages[key];
		}

		public bool Has(string key)
		{
			return messages.ContainsKey(key);
		}
	}

	abstract class GUIElement
	{
		public bool IsActive{ get; set; }
		public int systemIndex = -1;
		public CallBack callback;
		public string callbackName;

		virtual public void Update(ref State state, int id)
		{

		}

		public void SetCallback(RegistrySystem<CallBack> reg, string name)
		{
			callbackName = name;
			if(reg.Has(name))
			{
				callback = reg.Get(name);
			}
		}

		
	}

	class GUIButton : GUIElement
	{
		Rectangle rect;
		bool pressed;
		bool canPress;
		public Text text;

		public GUIButton(State state, Rectangle rect) : base()
		{
			this.rect = rect;
			pressed = false;
			canPress = false;
			
		}

		

		public override void Update(ref State state, int id)
		{
			var spriteSys = state.G.getSystem<SpriteSystem>();
			var inputSys = state.G.getSystem<MonogameSystem>();
			//var textureSys = state.getSystem<TextureSystem>();
			//var textSys = state.getSystem<TextRenderingSystem>();
			var renderSys = state.getSystem<RenderSystem>();
			var camera = state.getSystem<Camera>();

			//if (inputSys.IsMouseCaptured)
			//	return;

			//var spriteIndex = state.getComponentIndex(id, spriteSys.systemIndex);
			//var sprite = spriteSys.getComponent(spriteIndex);
			if (inputSys.JustPressed(Keys.Back) && text.String.Length > 0)
			{
				text.String = text.String.Remove(text.String.Length - 1, 1);
			}

			var renderIndex = state.getComponentIndex(id, renderSys.systemIndex);
			var render = renderSys.getComponent(renderIndex) as RenderCollection;

			render.renders[1] = text;
			var sprite = (Sprite) render.renders[0];

			var mouseState = inputSys.getMouseState();
			if (IsActive)
			{
				if (!inputSys.IsMouseCaptured && sprite.texture.getRect().Contains(camera.toCameraScale(mouseState.Position.ToVector2())))
				{
					if (mouseState.LeftButton == ButtonState.Pressed && canPress)
					{
						sprite.Play("pressed", 1, false);
						pressed = true;
					}
					else if (mouseState.LeftButton == ButtonState.Released && pressed)
					{
						sprite.Play("active", 1, false);
						pressed = false;
						callback(state, id);
					}
					else if(mouseState.LeftButton == ButtonState.Released)
					{
						sprite.Play("mouseOver", 2, true);
						canPress = true;
					} else
					{
						sprite.Play("mouseOver", 2, true);
					}
					inputSys.IsMouseCaptured = true;
				} else
				{
					sprite.Play("active", 1, false);
					pressed = false;
					canPress = false;
				}
			}
			else
			{
				sprite.Play("inactive", 1, false);
			}

			base.Update(ref state, id);
		}
	}

	delegate char Pattern(char c);
	class GUITextBox : GUIElement
	{
		public Color Color1 { set; get; }
		public Color Color2 { set; get; }
		public Color TextColor { set; get; }

		private string str;
		public Text text;


		private Pattern patternMatcher = c => c;

		public GUITextBox(State state, Rectangle r)
		{
			state.G.getSystem<MonogameSystem>().Game.Window.TextInput += UpdateTextField;
		}

		public void UpdateTextField(object obj, TextInputEventArgs args)
		{
			text.String += patternMatcher(args.Character);
		}

		public override void Update(ref State state, int id)
		{
			var spriteSys = state.G.getSystem<SpriteSystem>();
			var inputSys = state.G.getSystem<MonogameSystem>();
			//var textureSys = state.getSystem<TextureSystem>();
			//var textSys = state.getSystem<TextRenderingSystem>();
			var renderSys = state.getSystem<RenderSystem>();
			var camera = state.getSystem<Camera>();

			//if (inputSys.IsMouseCaptured)
			//	return;

			//var spriteIndex = state.getComponentIndex(id, spriteSys.systemIndex);
			//var sprite = spriteSys.getComponent(spriteIndex);
			if (inputSys.JustPressed(Keys.Back) && text.String.Length > 0)
			{
				text.String = text.String.Remove(text.String.Length - 1, 1);
			}


			if (patternMatcher != null)
			{

			}
			base.Update(ref state, id);
		}
	}

	class GUIPanel : GUIElement
	{
		List<GUIElement> elements;
	}

	class GUISystem : ComponentSystem<GUIElement>, ISysUpdateable
	{
		TransformSystem transSys;
		RenderSystem renderSys;
		MonogameSystem monogameSys;

		public GUISystem(State state) : base(state, "GUISystem")
		{
		}

		protected override void SerailizeComponent(ref GUIElement component, BinaryWriter writer)
		{
			throw new NotImplementedException();
		}

		protected override GUIElement DeserailizeComponent(BinaryReader reader)
		{
			throw new NotImplementedException();
		}

		public override void SerializeSystem(BinaryWriter writer)
		{
			throw new NotImplementedException();
		}

		public override void DeserializeSystem(BinaryReader reader)
		{
			throw new NotImplementedException();
		}

		public override BaseSystem DeserializeConstructor(State state, string name)
		{
			return new GUISystem(state);
		}

		public void Update(Global G)
		{
			for(int i = 0; i < size; ++i)
			{
				if (entityIDs[i] == -1)
					continue;

				components[i].Update(ref _state, entityIDs[i]);
			}
		}

		static public int CreateButton(State state, Text text, string textureName, Rectangle rect, string callbackName, float layer = 0.9f)
		{
			var transSys = state.getSystem<TransformSystem>();
			var renderSys = state.getSystem<RenderSystem>();
			var spriteSys = state.G.getSystem<SpriteSystem>();
			var guiSys = state.getSystem<GUISystem>();
			var registry = state.G.getSystem("CallBackRegistry") as RegistrySystem<CallBack>;

			var e = state.CreateEntity();
			var trans = new Transform();
			trans.position = rect.Location.ToVector2();
			transSys.AddComponent(e, trans);

			var texture = new Texture2(state.G, textureName, layer);
			//textureSys.AddComponent(e, texture);
			texture.color = Color.Blue;

			var sprite = new Sprite(spriteSys, texture);
			sprite.Play("active", 1, true);
			sprite.texture.color = Color.Red;

			texture.setRect(rect.Width, rect.Height);
			text.Layer = layer - 0.1f;
			//textSys.AddComponent(e, text);

			RenderCollection render = new RenderCollection(sprite, text);
			renderSys.AddComponent(e, render);

			var buttonElement = new GUIButton(state, rect);
			buttonElement.IsActive = true;
			buttonElement.SetCallback(registry, callbackName);
			buttonElement.text = text;
			guiSys.AddComponent(e, buttonElement);

			return e;
		}
	}
}