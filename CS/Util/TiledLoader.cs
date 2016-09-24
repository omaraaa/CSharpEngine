using MoonSharp.Interpreter;
using CS;
using CS.Components;
using Entities;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using FarseerPhysics.Dynamics;
using System;

namespace Util
{
	internal struct Tileinfo
	{
		public int startid;
		public int count;
		public string name;
	}

	delegate void TiledObjectContructor(State state, Table layer, Table obj, float renderLayer);

	class TiledLoader
	{
		public static void LoadTiledLua(State state, string scriptName)
		{
			var G = state.G;
			var luaSys = G.getSystem<GlobalLuaSystem>();

			DynValue tileScript = luaSys.loadScript(scriptName);
			var table = tileScript.Table;

			var width = (int)(table.Get("width")).Number;
			var height = (int)(table.Get("height")).Number;
			var tw = (int)(table.Get("tilewidth")).Number;
			var th = (int)(table.Get("tileheight")).Number;

			var transSys = state.getSystem<TransformSystem>();
			var physicsSys = state.getSystem<PhysicsSystem>();
			var textureSys = state.getSystem<RenderSystem>();
			var spriteSys = state.G.getSystem<SpriteSystem>();

			var registy = G.getSystem < RegistrySystem<TiledObjectContructor>>();

			var tilesets = table.Get("tilesets").Table;
			List<Tileinfo> tilesetinfo = new List<Tileinfo>();
			foreach (var dyn in tilesets.Values)
			{
				var tileset = dyn.Table;
				var name = (tileset.Get("name")).String;
				var startid = (int)(tileset.Get("firstgid")).Number;
				var tcount = (int)(tileset.Get("tilecount")).Number;
				Tileinfo tinfo = new Tileinfo();
				tinfo.startid = startid;
				tinfo.count = tcount;
				tinfo.name = name;
				tilesetinfo.Add(tinfo);
				//load tileset frames
				spriteSys.loadJSON("Content/" + name + ".json", name);
			}

			var layers = table.Get("layers").Table;
			int count = 0;
			foreach (var dyn in layers.Values)
			{
				count++;
			}

			float incr = 1f / (count + 1);
			float renderLayer = 1;

			foreach (var dyn in layers.Values)
			{
				renderLayer -= incr;
				var layer = dyn.Table;
				var type = (layer.Get("type")).String;
				var offsetX = (float)(layer.Get("offsetx")).Number;
				var offsetY = (float)(layer.Get("offsety")).Number;
				var properties = (layer.Get("properties")).Table;
				if (type == "tilelayer")
				{
					bool isSolid = (properties.Get("IsSolid")).Boolean;
					var data = (layer.Get("data")).Table;
					var it = data.Values.GetEnumerator();
					for (int i = 0; i < width * height; ++i)
					{
						it.MoveNext();
						var indx = (int)it.Current.Number;
						int spriteIndex = -1;

						if (indx == 0)
							continue;

						Tileinfo tileinfo = new Tileinfo();
						foreach (var tile in tilesetinfo)
						{
							if (indx >= tile.startid && indx < tile.startid + tile.count)
								tileinfo = tile;
						}

						spriteIndex = indx - tileinfo.startid;

						var e = state.CreateEntity();
						Transform trans = new Transform();
						trans.position = new Vector2(((i % width) * tw) + offsetX + tw / 2, (float)Math.Floor((double)(i / width) * th + offsetY + th / 2));
						transSys.AddComponent(e, trans);

						if (isSolid)
						{
							Body b = PhysicsObject.CreateBody(physicsSys, tw, th, 0);
							physicsSys.AddComponent(e, b);
						}

						Texture2 texture = new Texture2(state.G, tileinfo.name, renderLayer);
						//texture.setRect(tw, th);


						Sprite spr = new Sprite(spriteSys, texture);
						spr.SetFrame(spriteIndex);
						textureSys.AddComponent(e, spr);
					}
				}
				else if (type == "objectgroup")
				{
					var objects = layer.Get("objects").Table;
					foreach (var dynObj in objects.Values)
					{
						var obj = dynObj.Table;
						var objType = obj.Get("type").String;
						if (registy.Has(objType))
						{
							registy.Get(objType)(state, layer, obj, renderLayer);
						}
					}
				}
				else if (type == "imagelayer")
				{
					var textureName = layer.Get("image").String.Split('.')[0];

					Image img = new Image(state, textureName, new Vector2(offsetX, offsetY), Vector2.Zero, renderLayer, true);
				}
			}
		}

		//private static void createTile(int index, bool isSolid, State state, )
	}
}