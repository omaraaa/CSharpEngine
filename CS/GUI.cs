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
		public bool IsMouseCaptured { get; private set; }
		KeyboardState keyboardState;

		public InputSystem(State state) : base(state, "InputSystem")
		{
			IsMouseCaptured = false;
		}

		public override BaseSystem DeserializeConstructor(State state, string name)
		{
			return new InputSystem(state);
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
			IsMouseCaptured = false;
			mouseState = G.mouseState;
			keyboardState = G.keyboardState;
		}

		public MouseState getMouseState()
		{
			IsMouseCaptured = true;
			return mouseState;
		}
	}

	class MessageSystem : BaseSystem
	{
		public MessageSystem(State state, string name) : base(state, name)
		{
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

		public GUIButton(Rectangle rect) : base()
		{
			this.rect = rect;
			pressed = false;
			canPress = false;
		}

		public override void Update(ref State state, int id)
		{
			var spriteSys = state.getSystem<SpriteSystem>();
			var inputSys = state.G.getSystem<InputSystem>();
			var textureSys = state.getSystem<TextureSystem>();

			if (inputSys.IsMouseCaptured)
				return;

			var spriteIndex = state.getComponentIndex(id, spriteSys.systemIndex);
			var sprite = spriteSys.getComponent(spriteIndex);

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

	class GUITextBox : GUIElement
	{
		public Color Color1 { set; get; }
		public Color Color2 { set; get; }
		public Color TextColor { set; get; }
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

			texture.setRect(rect.Width, rect.Height);

			spriteSys.AddComponent(e, sprite);

			text.Layer = layer;
			textSys.AddComponent(e, text);

			var buttonElement = new GUIButton(rect);
			buttonElement.IsActive = true;
			buttonElement.SetCallback(registry, callbackName);
			guiSys.AddComponent(e, buttonElement);

			return e;
		}
	}
}