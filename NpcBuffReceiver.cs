using Sandbox;
using Sandbox.Navigation;
using System;

/// <summary>
/// Приёмник глобальных бафов на конкретном NPC.
/// Хранит базовые значения и применяет множители.
/// </summary>
public sealed class NpcBuffReceiver : Component
{
	public float MoveSpeedMult { get; private set; } = 1f;
	public float DamageMult { get; private set; } = 1f;
	public float DamageTakenMult { get; private set; } = 1f;

	private float _baseMoveSpeed;
	private int _baseAttackDamage;

	private NavMeshAgent _agent;
	private float _baseAgentMaxSpeed;
	private bool _hasBaseAgentSpeed;

	private bool _inited;

	protected override void OnStart()
	{
		InitBase();

		// Если директор уже есть — применим сразу.
		if ( NpcBuffDirector.Instance != null )
			ApplyGlobalState( NpcBuffDirector.Instance.CurrentState );
	}

	private void InitBase()
	{
		if ( _inited ) return;
		_inited = true;

		_agent = Components.Get<NavMeshAgent>();

		var ghost = Components.Get<NPC>();
		if ( ghost != null )
		{
			_baseMoveSpeed = ghost.MoveSpeed;
			_baseAttackDamage = ghost.AttackDamage;
		}

		var mummy = Components.Get<MummyNPC>();
		if ( mummy != null )
		{
			_baseMoveSpeed = mummy.MoveSpeed;
			_baseAttackDamage = mummy.AttackDamage;
		}

		if ( _agent != null && _agent.IsValid )
		{
			_baseAgentMaxSpeed = _agent.MaxSpeed;
			_hasBaseAgentSpeed = true;
		}
	}

	public void ApplyGlobalState( NpcBuffState state )
	{
		InitBase();

		MoveSpeedMult = SafeMult( state.MoveSpeedMult );
		DamageMult = SafeMult( state.DamageMult );
		DamageTakenMult = SafeMult( state.DamageTakenMult );

		// 1) NavMesh скорость
		if ( _agent != null && _agent.IsValid && _hasBaseAgentSpeed )
			_agent.MaxSpeed = _baseAgentMaxSpeed * MoveSpeedMult;

		// 2) Передаём множители в конкретный NPC
		var ghost = Components.Get<NPC>();
		if ( ghost != null )
		{
			ghost.NpcBuff_MoveSpeedMult = MoveSpeedMult;
			ghost.NpcBuff_DamageMult = DamageMult;
			ghost.NpcBuff_DamageTakenMult = DamageTakenMult;

			ghost.NpcBuff_BaseMoveSpeed = _baseMoveSpeed;
			ghost.NpcBuff_BaseAttackDamage = _baseAttackDamage;
			return;
		}

		var mummy = Components.Get<MummyNPC>();
		if ( mummy != null )
		{
			mummy.NpcBuff_MoveSpeedMult = MoveSpeedMult;
			mummy.NpcBuff_DamageMult = DamageMult;
			mummy.NpcBuff_DamageTakenMult = DamageTakenMult;

			mummy.NpcBuff_BaseMoveSpeed = _baseMoveSpeed;
			mummy.NpcBuff_BaseAttackDamage = _baseAttackDamage;
		}
	}

	private static float SafeMult( float v )
	{
		if ( float.IsNaN( v ) || float.IsInfinity( v ) ) return 1f;
		if ( v < 0.01f ) return 0.01f;
		if ( v > 1000f ) return 1000f;
		return v;
	}
}
