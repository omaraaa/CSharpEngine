using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Text;

namespace CS.Util
{

	enum SerializationMode
	{
		SERIALIZE, DESEARIALIZE
	};

	class Serializer
	{
		SerializationMode mode;
		FileStream filestream;
		IFormatter formatter;

		public Serializer(FileStream filestream, IFormatter formatter)
		{
			this.filestream = filestream;
			this.formatter = formatter;
			mode = SerializationMode.SERIALIZE;
		}

		public void SetMode(SerializationMode mode)
		{
			this.mode = mode;
		}

		public T Serialize<T>(T obj)
		{
			switch(mode)
			{
				case SerializationMode.SERIALIZE:
					formatter.Serialize(filestream, obj);
					break;
				case SerializationMode.DESEARIALIZE:
					obj = (T) formatter.Deserialize(filestream);
					break;
			}
			return obj;
		}


	}
}
