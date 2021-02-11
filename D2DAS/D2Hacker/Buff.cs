namespace D2DAS.D2Hacker
{
	public enum BuffType
	{
		Speed,
		Health,
		Mana,
		Gold
	}

	public enum BuffEffect
	{
		Relative,
		Absolute
	}

	public enum BuffValueType
	{
		Total,
		Percentage
	}

	public class Buff
	{
		public Buff(
			BuffType type,
			BuffEffect effect,
			BuffValueType valueType,
			int value,
			float duration
			)
		{
			ID = NextID++;
			Type = type;
			Effect = effect;
			ValueType = valueType;
			Value = value;
			Duration = duration;

			BaseValue = -1;
			DurationLeft = duration;
		}

		public int ID { get; private set; }
		public BuffType Type { get; private set; }
		public BuffEffect Effect { get; private set; }
		public BuffValueType ValueType { get; private set; }
		public int Value { get; private set; }
		public float Duration { get; private set; }

		public int BaseValue { get; set; }
		public float DurationLeft { get; set; }

		private static int NextID = 0;
	}
}
