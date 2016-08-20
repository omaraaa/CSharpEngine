using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace Util
{
    class DebugGraph
    {
		private Texture2D texture;
		private int scale;
		private int defaultScale;
		private double target;
		private Rectangle dest;
		private Color[] data;

		public DebugGraph(GraphicsDevice device, Rectangle renderRect, int xScale, double targetValue)
		{
			var width = renderRect.Width;
			var height = renderRect.Height;
			texture = new Texture2D(device, width, height);
			data = new Color[width * height];
			for (int i = 0; i < data.Length; ++i)
			{
				if (i % width == 10 && (i / width) % height == height - 10)
					data[i] = Color.White;

				if ((i / width) % height == height - 1)
					data[i] = Color.White;

				if ((i / width) % height == height - (targetValue / xScale) * height)
					data[i] = Color.Green;

			}
			texture.SetData(data);
			scale = xScale;
			defaultScale = xScale;
			target = targetValue;
			dest = renderRect;
		}

		public void Update(double value)
		{
			var width = texture.Width;
			var height = texture.Height;
			//texture.GetData(data);
			Color c = Color.Red;

			if(value > this.target)
				c = Color.Green;
				

			if(value > defaultScale)
			{
				scale = (int) value * 2;
			}
			else
			{
				scale = defaultScale;
			}

			for (int i = 0; i < data.Length; ++i)
			{
				if (i % width == width - 1)
				{
					data[i] = Color.Transparent;
					if ((i / width) % height > height - (value / scale) * height)
					{
						data[i] = c;
					}

					if ((i / width) % height == height - (target / scale) * height)
						data[i] = Color.Blue;
					continue;
				}
				data[i] = data[i + 1];
			}
			texture.SetData(data);
		}

		public void Draw(SpriteBatch batch)
		{
			batch.Draw(texture, destinationRectangle: dest);
		}
    }
}
