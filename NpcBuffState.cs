using System;

public enum NpcBuffKind
{
	MoveSpeed,
	Damage,
	Resistance
}

/// <summary>
/// Глобальное состояние бафов для NPC (стакуется бесконечно).
/// Resistance = множитель входящего урона (DamageTakenMult).
/// Например 0.95 = -5% входящего урона.
/// </summary>
public struct NpcBuffState
{
	public float MoveSpeedMult;
	public float DamageMult;
	public float DamageTakenMult;

	public int MoveSpeedStacks;
	public int DamageStacks;
	public int ResistanceStacks;

	public static NpcBuffState Default => new NpcBuffState
	{
		MoveSpeedMult = 1f,
		DamageMult = 1f,
		DamageTakenMult = 1f,
		MoveSpeedStacks = 0,
		DamageStacks = 0,
		ResistanceStacks = 0
	};

	public void ApplyStack( NpcBuffKind kind, float perStackPercent )
	{
		float step = MathF.Max( 0.0001f, perStackPercent ) / 100f;

		switch ( kind )
		{
			case NpcBuffKind.MoveSpeed:
				MoveSpeedStacks++;
				MoveSpeedMult *= (1f + step);
				break;

			case NpcBuffKind.Damage:
				DamageStacks++;
				DamageMult *= (1f + step);
				break;

			case NpcBuffKind.Resistance:
				ResistanceStacks++;
				DamageTakenMult *= MathF.Max( 0.01f, 1f - step ); // -5% => 0.95
				break;
		}
	}

	public int GetStacks( NpcBuffKind kind )
	{
		return kind switch
		{
			NpcBuffKind.MoveSpeed => MoveSpeedStacks,
			NpcBuffKind.Damage => DamageStacks,
			NpcBuffKind.Resistance => ResistanceStacks,
			_ => 0
		};
	}
}
