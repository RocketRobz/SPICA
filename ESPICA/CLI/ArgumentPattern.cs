using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ESPICA.CLI
{
	public class ArgumentPattern
	{

		public string key;
		public string brief;
		public Object defaultValue;
		public bool allowsMultiple;
		private String[] tags;
		private ArgumentType type;

		public ArgumentPattern(String key, String brief, ArgumentType type, Object defaultValue, params string[] tags) : this(key, brief, type, defaultValue, false, tags)
		{
			
		}

		public ArgumentPattern(String key, String brief, ArgumentType type, Object defaultValue, bool allowsMultiple, params string[] tags)
		{
			this.key = key;
			this.brief = brief;
			this.defaultValue = defaultValue;
			this.tags = tags;
			this.type = type;
			this.allowsMultiple = allowsMultiple;
		}

		public void print()
		{
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < tags.Length; i++)
			{
				if (i != 0)
				{
					sb.Append(", ");
				}
				sb.Append(tags[i]);
			}
			while (sb.Length < 30)
			{
				sb.Append(" ");
			}
			sb.Append("-    ");
			sb.Append(brief);
			if (defaultValue != null)
			{
				sb.Append(" (Default: ");
				sb.Append(defaultValue);
				sb.Append(")");
			}
			Console.WriteLine(sb.ToString());
		}

		public int getDefaultAsInt()
		{
			return (int)defaultValue;
		}

		public float getDefaultAsFloat()
		{
			return (float)defaultValue;
		}

		public string getDefaultAsString()
		{
			return defaultValue.ToString();
		}

		public bool getDefaultAsBoolean()
		{
			return (bool)defaultValue;
		}

		public String match(String str, int index)
		{
			String src = str.Substring(index);
			foreach (string tag in tags)
			{
				if (src.StartsWith(tag))
				{
					return tag;
				}
			}
			return null;
		}

		public ArgumentContent getContent(String str, String matchedTag, ArgumentBuilder bld)
		{
			String substr = str.Substring(bld.parserIndex + matchedTag.Length);
			int end = 0;
			bool b = false;
			void Loop() {
				for (; end < substr.Length; end++)
				{
					char cc = substr[end];
					switch (cc)
					{
						case '"':
							b = !b;
							break;
						case '-':
							if (!b)
							{
								return;
							}
							break;
					}
				}
			}
			Loop();
			substr = substr.Substring(0, end);
			bld.parserIndex += end + matchedTag.Length;
			String[] cntArr = ArgumentBuilder.getSplitAtSpacesWithQuotationMarks(substr);
			List<string> cnt = new List<string>();
			foreach (string s in cntArr)
			{
				string t = s.Trim();
				if (s.Length > 0)
				{
					cnt.Add(t);
				}
			}

			if (cnt.Count > 1 && !allowsMultiple)
			{
				bld.defaultContent.contents.Add(cnt.GetRange(1, cnt.Count));
				cnt = cnt.GetRange(0, 1);
			}
			if (cnt.Count == 0 && type != ArgumentType.BOOLEAN)
			{
				throw new NotSupportedException("Argument " + key + " requires parameters.");
			}
			ArgumentContent c = new ArgumentContent();
			if (type == ArgumentType.BOOLEAN)
			{
				if (cnt.Count == 0)
				{
					c.contents.Add(true);
				}
			}
			c.key = key;

			foreach (String ps in cnt)
			{
				string p;
				if (ps.StartsWith("\"") && ps.EndsWith("\"") && ps.Length > 1)
				{
					p = ps.Substring(1, ps.Length - 2);
				}
				else
                {
					p = ps;
                }

				switch (type)
				{
					case ArgumentType.FLOAT:
						float flt;
						try
						{
							flt = float.Parse(p);
						}
						catch (Exception ex)
						{
							throw new NotSupportedException(p + "is not a valid floating point parameter for argument " + key + ".");
						}
						c.contents.Add((float)flt);
						break;
					case ArgumentType.INT:
						int intv;
						try
						{
							intv = int.Parse(p);
						}
						catch (Exception ex)
						{
							throw new NotSupportedException(p + " is not a valid integer parameter for argument " + key + ".");
						}
						c.contents.Add((int)intv);
						break;
					case ArgumentType.STRING:
						c.contents.Add(p);
						break;
					case ArgumentType.BOOLEAN:
						c.contents.Add(bool.Parse(p));
						break;
				}
			}
			return c;
		}
	}
}
