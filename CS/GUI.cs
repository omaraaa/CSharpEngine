
using System;
using System.IO;

using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;

namespace CS.Components
{
	class InputSystem : BaseSystem, ISysUpdateable
	{
		MouseState mouseState;
		bool isMouseCaptured;
		KeyboardState keyboardState;

		public InputSystem(State state, string name) : base(state, name)
		{
			isMouseCaptured = false;
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

		public void Update(Global G)
		{
			isMouseCaptured = false;
			mouseState = G.mouseState;
			keyboardState = G.keyboardState;
		}
	}

	class GUIElement
	{
		public bool IsClickable { get; set; }
	}

	class GUIButton : GUIElement
	{

	}

	class GUIText : GUIElement
	{

	}

	class GUISystem : ComponentSystem<GUIElement>, ISysUpdateable
	{
		TransformSystem transSys;
		TextureSystem textureSys;
		InputSystem inputSys;

		public GUISystem(State state, string name) : base(state, name)
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
			throw new NotImplementedException();
		}
	}
}