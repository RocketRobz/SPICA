using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ESPICA.CLI
{
	public class ArgumentContent
	{
		public string key;
		public List<Object> contents = new List<Object>();

		public bool booleanValue()
		{
			return booleanValue(0);
		}

		public int intValue()
		{
			return intValue(0);
		}

		public float floatValue()
		{
			return floatValue(0);
		}

		public string stringValue()
		{
			return stringValue(0);
		}

		public bool booleanValue(int idx)
		{
			rangeCheck(idx);
			return (bool)contents[idx];
		}

		public int intValue(int idx)
		{
			rangeCheck(idx);
			return (int)contents[idx];
		}

		public float floatValue(int idx)
		{
			rangeCheck(idx);
			return (float)contents[idx];
		}

		public string stringValue(int idx)
		{
			rangeCheck(idx);
			return contents[idx].ToString();
		}

		private void rangeCheck(int idx)
		{
			if (contents.Count <= idx)
			{
				throw new IndexOutOfRangeException("Requested parameter " + idx + " for " + key + " out of range.");
			}
		}
	}
}
