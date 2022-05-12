using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ESPICA.CLI
{
	public class ArgumentBuilder {
		public List<ArgumentPattern> patterns = new List<ArgumentPattern>();

		public ArgumentBuilder(params ArgumentPattern[] patterns)
		{
			this.patterns = new List<ArgumentPattern>(patterns);
		}

		public int parserIndex = 0;
		public ArgumentContent defaultContent;
		public List<ArgumentContent> cnt = new List<ArgumentContent>();

		public void print()
		{
			foreach (ArgumentPattern ptn in patterns)
			{
				ptn.print();
			}
		}

		public ArgumentContent getContent(string name)
		{
			return getContent(name, false);
		}

		public ArgumentContent getContent(string name, bool allowsNullValue)
		{
			foreach (ArgumentContent c in cnt)
			{
				if (c.key.Equals(name))
				{
					return c;
				}
			}
			foreach (ArgumentPattern ptn in patterns)
			{
				if (ptn.key.Equals(name))
				{
					if (ptn.defaultValue == null)
					{
						if (ptn.allowsMultiple)
						{
							ArgumentContent d = new ArgumentContent();
							d.key = name;
							return d;
						}
						if (allowsNullValue)
						{
							return null;
						}
						throw new NotSupportedException("Required argument not supplied: " + name);
					}
					ArgumentContent dummy = new ArgumentContent();
					dummy.key = name;
					dummy.contents.Add(ptn.defaultValue);
					return dummy;
				}
			}
			throw new ArgumentException("Unbuilt argument requested.");
		}

		public void parse(string[] args)
		{
			defaultContent = new ArgumentContent();
			StringBuilder comb = new StringBuilder();
			foreach (string a in args)
			{
				string a2 = a;
				if (!a2.StartsWith("-"))
                {
					a2 = '"' + a2 + '"';
                }
				comb.Append(a2);
			}
			string str = comb.ToString().Trim();
			cnt = new List<ArgumentContent>();
			bool argStart = false;

			for (; parserIndex < str.Length; parserIndex++)
			{
				if (str[parserIndex] == '-')
				{
					if (!argStart)
					{
						string[] defaultArgs = getSplitAtSpacesWithQuotationMarks(str.Substring(0, parserIndex));
						foreach (string dflt in defaultArgs)
						{
							string t = dflt.Trim();
							if (t.Length > 0)
							{
								defaultContent.contents.Add(t);
							}
						}
					}
					argStart = true;
					bool matched = false;
					foreach (ArgumentPattern p in patterns)
					{
						string matchStr = p.match(str, parserIndex);
						if (matchStr != null)
						{
							cnt.Add(p.getContent(str, matchStr, this));
							matched = true;
							parserIndex--;
							break;
						}
					}
					if (!matched)
					{
						int lastIndex = str.IndexOf(' ', parserIndex);
						if (lastIndex == -1)
						{
							lastIndex = str.Length;
						}
						throw new ArgumentException("Invalid argument: " + str.Substring(parserIndex, lastIndex - parserIndex));
					}
				}
			}
			if (!argStart)
			{
				string[] defaultArgs = getSplitAtSpacesWithQuotationMarks(str);
				foreach (string dflt in defaultArgs)
				{
					string t = dflt.Trim();
					if (t.Length > 0)
					{
						defaultContent.contents.Add(t);
					}
				}
			}
		}

		public static string[] getSplitAtSpacesWithQuotationMarks(string str)
		{
			string[] cnt = System.Text.RegularExpressions.Regex.Split(str, "(?<=^[^\"]*(?:\"[^\"]*\"[^\"]*)*) (?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");
			//https://stackoverflow.com/questions/14655023/split-a-string-that-has-white-spaces-unless-they-are-enclosed-within-quotes

			return cnt;
		}
	}
}
