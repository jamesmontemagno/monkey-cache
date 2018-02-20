using System;

namespace MonkeyCache.TestsShared
{
	public class TestLid : ILid
	{
		public bool WasUsed { get; set; }

		public string AddingToBarrel(string content) => Reverse(content);

		public string GettingFromBarrel(string content) => Reverse(content);

		string Reverse(string data)
		{
			WasUsed = true;

			var chars = data.ToCharArray();
			Array.Reverse(chars);
			return new string(chars);
		}
	}
}
