using CS;

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using Microsoft.Xna.Framework.Graphics;

namespace MG
{
	class MonogameSystem : BaseSystem, ISysUpdateable
	{
		public Game Game { get; set; }

		//Input
		public MouseState mouseState;
		public TouchCollection touchCollection;
		public KeyboardState keyboardState;

		public bool IsMouseCaptured { get; set; }
		private List<Keys> lastKeyboardState;
		private List<Keys> pressedKeys;
		private List<Keys> releasedKeys;

		public MonogameSystem(State state) : base(state, "MonogameSystem")
		{
			IsMouseCaptured = false;
			lastKeyboardState = new List<Keys>();
			pressedKeys = new List<Keys>();
			releasedKeys = new List<Keys>();
		}

		public override BaseSystem DeserializeConstructor(State state, string name)
		{
			return new MonogameSystem(state);
		}

		public override void DeserializeSystem(BinaryReader reader)
		{
		}

		public override void SerializeSystem(BinaryWriter writer)
		{
		}

		public Texture2D getTexture(string name)
		{
			return Game.Content.Load<Texture2D>(name);
		}

		public void Update(Global G)
		{
			pressedKeys.Clear();
			releasedKeys.Clear();
			IsMouseCaptured = false;
			mouseState = Mouse.GetState();
			keyboardState = Keyboard.GetState();
			var keys = keyboardState.GetPressedKeys();

			foreach (Keys key in keys)
			{
				if (!lastKeyboardState.Contains(key))
				{
					pressedKeys.Add(key);
				}
			}

			lastKeyboardState = new List<Keys>(keys);
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
}
