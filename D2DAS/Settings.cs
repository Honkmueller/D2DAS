using System.Collections.Generic;
using D2DAS.D2Hacker;

namespace D2DAS
{
	public class Settings
	{
		public string AccountID;
		public string JWTToken;
		public Dictionary<string, Hashtag> Hashtags;
	}

	public class Hashtag
	{
		public BuffType Type;
		public BuffEffect Effect;
		public BuffValueType ValueType;
		public string Value;
		public string Duration;
	}
}
