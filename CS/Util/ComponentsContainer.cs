using System;
using System.Collections.Generic;
using System.Text;

namespace CS.Util
{
    class ComponentsContainer<T>
    {
		T[] array;
		uint[] entityIDs;
		int size;
		uint index;

		uint cachedElement;

		public ComponentsContainer(int size=0)
		{
			array = new T[0];
			this.size = size;
			index = 0;

			cachedElement = 0;
		}

		public void Add(uint index, T element)
		{
			size++;
			Array.Resize(ref array, size);
			Array.Resize(ref entityIDs, size);

			array[size - 1] = element;
			entityIDs[size - 1] = index;
		}

    }
}
