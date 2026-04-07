public static class StatueRewardSystem
{
	public static int CapturedCount { get; private set; } = 0;

	public static int GetCurrentReward()
	{
		int reward = 10;

		for ( int i = 0; i < CapturedCount; i++ )
			reward *= 2;

		if ( reward > 200 )
			reward = 200;

		return reward;
	}

	public static int RegisterCaptureAndGetReward()
	{
		int reward = GetCurrentReward();
		CapturedCount++;
		return reward;
	}

	public static void ResetProgress()
	{
		CapturedCount = 0;
	}
}
