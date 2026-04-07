using Sandbox;
using System;

/// <summary>
/// Стамина для спринта поверх встроенного PlayerController (без reflection).
/// Идея простая:
/// - До боя (Waiting / Preparing) стамина НЕ тратится
/// - Во время боя (Fighting) стамина работает как обычно
/// - Пока есть стамина и зажат run -> RunSpeed = BaseRunSpeed * SprintRunSpeedMultiplier
/// - Когда стамина 0 в бою -> RunSpeed = WalkSpeed
/// - До боя спринт разрешён даже при 0 стамины, потому что расход выключен
/// - Стамина регенится с задержкой после спринта
/// </summary>
public sealed class PlayerStamina : Component
{
	// =========================
	// CONFIG
	// =========================
	[Property, Group( "Stamina" )] public float MaxStamina { get; set; } = 100f;
	[Property, Group( "Stamina" )] public float DrainPerSecond { get; set; } = 25f;
	[Property, Group( "Stamina" )] public float RegenPerSecond { get; set; } = 18f;
	[Property, Group( "Stamina" )] public float RegenDelayAfterSprint { get; set; } = 0.35f;

	[Property, Group( "Sprint" )] public float SprintRunSpeedMultiplier { get; set; } = 1.0f;
	[Property, Group( "Sprint" )] public bool RequireMovementToDrain { get; set; } = true;
	[Property, Group( "Sprint" )] public float MinMoveSpeedToDrain { get; set; } = 10f;

	[Property] public bool DebugMode { get; set; } = false;

	// =========================
	// UPGRADES (stacking, % на всю игру)
	// =========================
	public int MoveSpeedStacks { get; private set; }
	public int StaminaRegenStacks { get; private set; }
	public int StaminaDrainStacks { get; private set; }

	// +10% скорости за стак (линейно)
	public float MoveSpeedMultiplier => 1f + 0.10f * MoveSpeedStacks;

	// +15% регена за стак (мультипликативно)
	public float StaminaRegenMultiplier => MathF.Pow( 1.15f, StaminaRegenStacks );

	// -10% расхода за стак (мультипликативно)
	public float StaminaDrainMultiplier => MathF.Pow( 0.90f, StaminaDrainStacks );

	public void AddMoveSpeedStack()
	{
		MoveSpeedStacks++;
		if ( DebugMode ) Log.Info( $"🏃 MoveSpeedStacks={MoveSpeedStacks} (x{MoveSpeedMultiplier:0.###})" );
	}

	public void AddStaminaRegenStack()
	{
		StaminaRegenStacks++;
		if ( DebugMode ) Log.Info( $"🔋 StaminaRegenStacks={StaminaRegenStacks} (x{StaminaRegenMultiplier:0.###})" );
	}

	public void AddStaminaDrainStack()
	{
		StaminaDrainStacks++;
		if ( DebugMode ) Log.Info( $"🧃 StaminaDrainStacks={StaminaDrainStacks} (x{StaminaDrainMultiplier:0.###})" );
	}

	// =========================
	// RUNTIME
	// =========================
	public float CurrentStamina { get; private set; }

	public float StaminaFraction
	{
		get
		{
			if ( MaxStamina <= 0.001f ) return 0f;
			return Clamp01( CurrentStamina / MaxStamina );
		}
	}

	public bool IsExhausted => CurrentStamina <= 0.001f;

	private PlayerController _controller;
	private GameManager _gameManager;

	private float _baseWalkSpeed = -1f;
	private float _baseRunSpeed = -1f;

	private float _timeSinceSprintEnd = 999f;

	protected override void OnStart()
	{
		CurrentStamina = MaxStamina > 0f ? MaxStamina : 0f;

		_controller = Components.Get<PlayerController>( true );
		_gameManager = Scene.GetAllComponents<GameManager>().FirstOrDefault();

		if ( _controller == null )
		{
			Log.Warning( "PlayerStamina: не нашёл PlayerController на игроке. Добавь компонент на того же GameObject." );
			return;
		}

		_baseWalkSpeed = _controller.WalkSpeed;
		_baseRunSpeed = _controller.RunSpeed;

		if ( DebugMode )
			Log.Info( $"🟦 PlayerStamina start. Walk={_baseWalkSpeed} Run={_baseRunSpeed}" );
	}

	protected override void OnUpdate()
	{
		if ( _controller == null || !_controller.IsValid )
		{
			_controller = Components.Get<PlayerController>( true );
			if ( _controller == null ) return;

			if ( _baseWalkSpeed < 0f ) _baseWalkSpeed = _controller.WalkSpeed;
			if ( _baseRunSpeed < 0f ) _baseRunSpeed = _controller.RunSpeed;
		}

		if ( _gameManager == null || !_gameManager.IsValid )
			_gameManager = Scene.GetAllComponents<GameManager>().FirstOrDefault();

		_timeSinceSprintEnd += Time.Delta;

		bool isFightActive = IsFightActive();
		bool wantSprint = Input.Down( "run" ); // у тебя Alt Move Button = run
		bool isMoving = !RequireMovementToDrain || GetMoveSpeed() >= MinMoveSpeedToDrain;

		bool canSpendStamina = isFightActive;
		bool canSprintInFight = !IsExhausted;
		bool draining = canSpendStamina && wantSprint && canSprintInFight && isMoving;

		if ( draining )
		{
			// ✅ расход только во время боя
			float drain = DrainPerSecond * StaminaDrainMultiplier;
			CurrentStamina = MathF.Max( 0f, CurrentStamina - drain * Time.Delta );
			_timeSinceSprintEnd = 0f;
		}
		else if ( _timeSinceSprintEnd >= RegenDelayAfterSprint )
		{
			// ✅ реген всегда работает
			float regen = RegenPerSecond * StaminaRegenMultiplier;
			CurrentStamina = MathF.Min( MaxStamina, CurrentStamina + regen * Time.Delta );
		}

		// До боя спринт разрешён бесплатно, даже если стамина пустая.
		bool sprintAllowed = wantSprint && (!isFightActive || !IsExhausted);
		ApplySpeed( sprintAllowed );
	}

	private bool IsFightActive()
	{
		return _gameManager != null
			&& _gameManager.IsValid
			&& _gameManager.CurrentState == GameManager.GameState.Fighting;
	}

	private void ApplySpeed( bool sprintAllowed )
	{
		float walkBase = (_baseWalkSpeed > 0f) ? _baseWalkSpeed : _controller.WalkSpeed;
		float runBase = (_baseRunSpeed > 0f) ? _baseRunSpeed : _controller.RunSpeed;

		// ✅ общий мультипликатор скорости от апгрейда “Скорость бега”
		float moveMult = MoveSpeedMultiplier;
		if ( moveMult < 0.1f ) moveMult = 0.1f;
		if ( moveMult > 10f ) moveMult = 10f;

		float walk = walkBase * moveMult;
		float run = runBase * moveMult;

		float sprintMult = SprintRunSpeedMultiplier;
		if ( sprintMult < 0.1f ) sprintMult = 0.1f;
		if ( sprintMult > 10f ) sprintMult = 10f;

		// Если в бою стамина 0 -> RunSpeed = WalkSpeed.
		float runFinal = sprintAllowed ? (run * sprintMult) : walk;

		_controller.WalkSpeed = walk;
		_controller.RunSpeed = runFinal;
	}

	private float GetMoveSpeed()
	{
		// PlayerController — physics based, обычно как RigidBody.
		// Самое безопасное: если есть Rigidbody рядом — берём его скорость.
		var rb = Components.Get<Rigidbody>( true );
		if ( rb != null && rb.IsValid )
			return rb.Velocity.Length;

		// Если Rigidbody нет — тогда не можем надёжно определить движение.
		// В этом случае лучше выключить RequireMovementToDrain в инспекторе.
		return 0f;
	}

	private static float Clamp01( float v )
	{
		if ( v < 0f ) return 0f;
		if ( v > 1f ) return 1f;
		return v;
	}
}
