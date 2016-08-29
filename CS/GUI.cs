using System.Collections.Generic;
using System;
using System.IO;

using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using Microsoft.Xna.Framework;

namespace CS.Components
{
	class InputSystem : BaseSystem, ISysUpdateable
	{
		MouseState mouseState;
		public bool IsMouseCaptured { get; set; }
		KeyboardState keyboardState;
		private List<Keys> lastKeyboardState;
		private List<Keys> pressedKeys;
		private List<Keys> releasedKeys;

		public InputSystem(State state) : base(state, "InputSystem")
		{
			IsMouseCaptured = false;
			lastKeyboardState = new List<Keys>();
			pressedKeys = new List<Keys>();
			releasedKeys = new List<Keys>();
		}

		public override BaseSystem DeserializeConstructor(State state, string name)
		{
			return new InputSystem(state);
		}

		public override void DeserializeSystem(BinaryReader reader)
		{
		}

		public override void SerializeSystem(BinaryWriter writer)
		{
		}

		public void Update(Global G)
		{
			pressedKeys.Clear();
			releasedKeys.Clear();
			IsMouseCaptured = false;
			mouseState = G.mouseState;
			keyboardState = G.keyboardState;
			var keys = keyboardState.GetPressedKeys();

			foreach(Keys key in keys)
			{
				if (!lastKeyboardState.Contains(key))
				{
					pressedKeys.Add(key);
				}
			}

			lastKeyboardState =  new List<Keys>(keys);
		}

		public MouseState getMouseState()
		{
			return mouseState;
		}

		public bool GetJustPressed(out Keys[] keys)
		{
			keys = pressedKeys.ToArray();
			
			return pressedKeys.Count != 0;
		}

		public bool JustPressed(Keys key)
		{
			return pressedKeys.Contains(key);
		}
	}

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
			throw new NotImplementedException();
		}

		public override void SerializeSystem(BinaryWriter writer)
		{
			throw new NotImplementedException();
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
			state.G.game.Window.TextInput += UpdateTextField;
		}

		public void UpdateTextField(object obj, TextInputEventArgs args)
		{
			text.String += args.Character;
		}

		public override void Update(ref State state, int id)
		{
			var spriteSys = state.getSystem<SpriteSystem>();
			var inputSys = state.G.getSystem<InputSystem>();
			var textureSys = state.getSystem<TextureSystem>();
			var textSys = state.getSystem<TextRenderingSystem>();

			if (inputSys.IsMouseCaptured)
				return;

			var spriteIndex = state.getComponentIndex(id, spriteSys.systemIndex);
			var sprite = spriteSys.getComponent(spriteIndex);

			if(inputSys.JustPressed(Keys.Back) && text.String.Length > 0)
			{
				text.String = text.String.Remove(text.String.Length - 1, 1);
			}

			textSys.SetComponent(id, text);

			var mouseState = inputSys.getMouseState();
			if (IsActive)
			{
				if (textureSys.getRect(id).Contains(state.camera.toCameraScale(mouseState.Position.ToVector2())))
				{
					if (mouseState.LeftButton == ButtonState.Pressed && canPress)
					{
						spriteSys.Play("pressed", id, 1, false);
						pressed = true;
					}
					else if (mouseState.LeftButton == ButtonState.Released && pressed)
					{
						spriteSys.Play("active", id, 1, false);
						pressed = false;
						callback(state, id);
					}
					else if(mouseState.LeftButton == ButtonState.Released)
					{
						spriteSys.Play("mouseOver", id, 1, false);
						canPress = true;
					} else
					{
						spriteSys.Play("mouseOver", id, 1, false);
					}
					inputSys.IsMouseCaptured = true;
				} else
				{
					spriteSys.Play("active", id, 1, false);
					pressed = false;
					canPress = false;
				}
			}
			else
			{
				spriteSys.Play("inactive", id, 1, false);
			}

			base.Update(ref state, id);
		}
	}

	delegate bool Pattern(char c);
	class GUITextBox : GUIElement
	{
		public Color Color1 { set; get; }
		public Color Color2 { set; get; }
		public Color TextColor { set; get; }

		private string str;

		private Pattern patternMatcher = null;

		public override void Update(ref State state, int id)
		{



			if(patternMatcher != null)
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
		TextureSystem textureSys;
		InputSystem inputSys;

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
			throw new NotImplementedException();
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
			var textureSys = state.getSystem<TextureSystem>();
			var textSys = state.getSystem<TextRenderingSystem>();
			var spriteSys = state.getSystem<SpriteSystem>();
			var guiSys = state.getSystem<GUISystem>();
			var registry = state.G.getSystem("CallBackRegistry") as RegistrySystem<CallBack>;

			var e = state.CreateEntity();
			var trans = new Transform();
			trans.position = rect.Location.ToVector2();
			transSys.AddComponent(e, trans);

			var texture = new Texture2(state.G, textureName, layer);
			textureSys.AddComponent(e, texture);

			var sprite = new Sprite(textureName);
			spriteSys.Play("active", e, 1, true);

			spriteSys.AddComponent(e, sprite);

			texture.setRect(rect.Width, rect.Height);
			text.Layer = layer;
			textSys.AddComponent(e, text);

			var buttonElement = new GUIButton(state, rect);
			buttonElement.IsActive = true;
			buttonElement.SetCallback(registry, callbackName);
			buttonElement.text = text;
			guiSys.AddComponent(e, buttonElement);

			return e;
		}
	}
}