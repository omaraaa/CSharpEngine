
using System;
using System.IO;
using CS;
using Microsoft.Xna.Framework;

namespace MG
{
	class Camera : BaseSystem
	{
		public Matrix matrix
		{
			get
			{
				return
				Matrix.CreateTranslation(new Vector3(-position.ToPoint().ToVector2(), 0))
				* Matrix.CreateScale(new Vector3(scale, 0))
				* Matrix.CreateTranslation(new Vector3(rect.Width / 2f, rect.Height / 2f, 0))
				;
			}
		}

		Rectangle rect;
		public Vector2 position;
		public Vector2 scale;
		public Vector2 center;
		public float lerpValue = 0.1f;
		private State _state;

		public Camera(State state) : base(state, "Camera")
		{
			var width = state.G.game.GraphicsDevice.Viewport.Width;
			var height = state.G.game.GraphicsDevice.Viewport.Height;
			rect = new Rectangle(0, 0, width, height);
			position = new Vector2(rect.Width / 2f, rect.Height / 2f);
			center = position;
			setScale(new Vector2(1, 1));
			_state = state;
		}

		public void SetPosition(Vector2 pos)
		{
			position = Vector2.SmoothStep(position, pos, lerpValue);
		}

		public void setScale(Vector2 scale)
		{
			this.scale = scale;
		}

		public void toCameraScale(ref Vector2 v)
		{
			var scale = new Vector2(matrix.Scale.X, matrix.Scale.Y);
			var pos = new Vector2(matrix.Translation.X, matrix.Translation.Y);
			v = v / scale - pos / scale;
		}

		public Vector2 toCameraScale(Vector2 v)
		{
			var scale = new Vector2(matrix.Scale.X, matrix.Scale.Y);
			var pos = new Vector2(matrix.Translation.X, matrix.Translation.Y);
			v = v / scale - pos / scale;
			return v;
		}

		public void toCamera(ref Vector2 v)
		{
			var scale = new Vector2(matrix.Scale.X, matrix.Scale.Y);
			var pos = new Vector2(matrix.Translation.X, matrix.Translation.Y);
			v = v - pos;
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

		public override void SerializeSystem(BinaryWriter writer)
		{
			throw new NotImplementedException();
		}

		public override void DeserializeSystem(BinaryReader reader)
		{
			throw new NotImplementedException();
		}
	}
}