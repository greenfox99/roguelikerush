public static class TimeSlowSystem
{
	// Активен ли скилл
	public static bool IsActive { get; set; } = false;

	// Насколько замедляется мир
	public static float WorldScale => IsActive ? 0.5f : 1f;
}
