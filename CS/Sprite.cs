using System.Collections.Generic;
using System.Collections;
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using Microsoft.Xna.Framework.Graphics;

using CS;

namespace CS.Components
{

	struct Animation
	{
		String AnimationName;
		float speed;
		int index;
		int[] indexs;
	}

	/*
	 * The sprite class takes a texture and changes it's source rectangle to match specific animation frame
	*/
	class Sprite
	{
		public String textureName;
		public Animation currentAnimation;
		public Sprite(String textureName)
		{

		}
	}

	/*
	 * The sprite system holds animation data for each texture that the sprite class uses
	*/
	class SpriteSystem : ComponentSystem<Sprite>, ISysUpdateable
	{
		public SpriteSystem(State state) : base(state, "SpriteSystem")
		{
		}

		public override BaseSystem DeserializeConstructor(State state)
		{
			throw new NotImplementedException();
		}

		public void Update(Global G)
		{
			throw new NotImplementedException();
		}
	}
}