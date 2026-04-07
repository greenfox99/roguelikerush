using Sandbox;

public sealed class Player : Component
{
	public Car CurrentCar => _currentCar;

	private Car _currentCar;
	private bool _inCar = false;

	// Настройки камер
	[Property] public float CameraDistance { get; set; } = 500f;    // Для 3-го лица
	[Property] public float CameraHeight { get; set; } = 200f;
	[Property] public float CameraSide { get; set; } = 0f;
	[Property] public float LookAtHeight { get; set; } = 50f;
	[Property] public float CameraSmoothness { get; set; } = 15f;

	[Property] public Vector3 FirstPersonBaseOffset { get; set; } = new Vector3( 50, 0, 150 ); // Базовая позиция камеры

	// Эффекты для 1-го лица
	[Property] public float TurnLean { get; set; } = 20f;           // Насколько камера уходит в сторону при повороте
	[Property] public float AccelLean { get; set; } = 30f;          // Насколько камера отходит назад при разгоне
	[Property] public float LeanSmoothness { get; set; } = 5f;      // Плавность эффектов

	[Property] public float ExitOffset { get; set; } = 200f;

	private Vector3 _currentCameraPos;
	private Rotation _currentCameraRot;
	private Vector3 _hidePosition = new Vector3( 0, 0, -10000 );

	private bool _firstPerson = false;

	// Для эффектов камеры
	private float _currentTurnOffset = 0f;
	private float _currentAccelOffset = 0f;

	protected override void OnStart()
	{
		var cam = Scene.Camera;
		if ( cam != null )
		{
			_currentCameraPos = cam.Transform.Position;
			_currentCameraRot = cam.Transform.Rotation;
		}

		Log.Info( "=== ИГРОК ЗАГРУЖЕН ===" );
	}

	protected override void OnUpdate()
	{
		if ( _inCar )
		{
			if ( _currentCar != null )
			{
				float gas = 0, turn = 0;
				if ( Input.Down( "Forward" ) ) gas = 1f;
				if ( Input.Down( "Backward" ) ) gas = -1f;
				if ( Input.Down( "Left" ) ) turn = -1f;
				if ( Input.Down( "Right" ) ) turn = 1f;
				_currentCar.SetInput( gas, turn );

				// Обновляем эффекты камеры
				UpdateCameraEffects( gas, turn );
			}

			if ( Input.Pressed( "View" ) )
			{
				_firstPerson = !_firstPerson;
				Log.Info( $"Камера переключена: {(_firstPerson ? "1-е лицо" : "3-е лицо")}" );
			}

			UpdateCamera();

			if ( Input.Pressed( "Use" ) ) ExitCar();
			return;
		}

		if ( Input.Pressed( "Use" ) ) CheckForCar();
	}

	private void UpdateCameraEffects( float throttle, float steer )
	{
		// Эффект поворота (камера уходит в сторону поворота)
		float targetTurnOffset = steer * TurnLean;
		_currentTurnOffset = MathX.Lerp( _currentTurnOffset, targetTurnOffset, Time.Delta * LeanSmoothness );

		// Эффект разгона (камера отходит назад)
		float targetAccelOffset = 0f;
		if ( throttle > 0.1f )
		{
			targetAccelOffset = -throttle * AccelLean; // Отрицательное значение = назад
		}
		_currentAccelOffset = MathX.Lerp( _currentAccelOffset, targetAccelOffset, Time.Delta * LeanSmoothness );
	}

	private void UpdateCamera()
	{
		if ( _currentCar == null ) return;

		var cam = Scene.Camera;
		if ( cam == null ) return;

		Vector3 carPos = _currentCar.Transform.Position;
		Rotation carRot = _currentCar.Transform.Rotation;

		if ( _firstPerson )
		{
			// ===== 1-е ЛИЦО С ЭФФЕКТАМИ =====

			// Базовая позиция
			Vector3 offset = new Vector3(
				FirstPersonBaseOffset.x + _currentAccelOffset, // Назад при разгоне
				FirstPersonBaseOffset.y + _currentTurnOffset,  // В сторону при повороте
				FirstPersonBaseOffset.z
			);

			Vector3 targetPos = carPos + carRot.Forward * offset.x +
									   carRot.Left * offset.y +
									   Vector3.Up * offset.z;

			// Мгновенно устанавливаем позицию
			cam.Transform.Position = targetPos;
			cam.Transform.Rotation = carRot;
		}
		else
		{
			// ===== 3-е ЛИЦО =====
			Vector3 offset = -carRot.Forward * CameraDistance +
							 carRot.Left * CameraSide +
							 Vector3.Up * CameraHeight;

			Vector3 targetPos = carPos + offset;
			Vector3 lookAt = carPos + Vector3.Up * LookAtHeight;
			Rotation targetRot = Rotation.LookAt( lookAt - targetPos );

			_currentCameraPos = Vector3.Lerp( _currentCameraPos, targetPos, Time.Delta * CameraSmoothness );
			_currentCameraRot = Rotation.Slerp( _currentCameraRot, targetRot, Time.Delta * 8f );

			cam.Transform.Position = _currentCameraPos;
			cam.Transform.Rotation = _currentCameraRot;
		}
	}

	private void CheckForCar()
	{
		var cam = Scene.Camera;
		if ( cam == null ) return;

		var start = cam.Transform.Position;
		var dir = cam.Transform.Rotation.Forward;
		var end = start + dir * 300f;

		var tr = Scene.Trace.Ray( start, end )
			.IgnoreGameObject( GameObject )
			.Run();

		if ( tr.Hit && tr.GameObject != null )
		{
			var car = tr.GameObject.Components.Get<Car>();
			if ( car != null && !car.HasDriver )
			{
				EnterCar( car );
			}
		}
	}

	private void EnterCar( Car car )
	{
		_currentCar = car;
		_currentCar.HasDriver = true;

		var cam = Scene.Camera;
		if ( cam != null )
		{
			_currentCameraPos = cam.Transform.Position;
			_currentCameraRot = cam.Transform.Rotation;
		}

		GameObject.Transform.Position = _hidePosition;

		var controller = Components.Get<CharacterController>();
		if ( controller != null ) controller.Enabled = false;

		_firstPerson = false;
		_currentTurnOffset = 0f;
		_currentAccelOffset = 0f;

		_inCar = true;
		Log.Info( "🚗 В машине (C - переключение камеры)" );
	}

	private void ExitCar()
	{
		if ( _currentCar != null )
		{
			Vector3 carPos = _currentCar.Transform.Position;
			Rotation carRot = _currentCar.Transform.Rotation;

			GameObject.Transform.Position = carPos + carRot.Left * ExitOffset + Vector3.Up * 50f;

			_currentCar.HasDriver = false;
			_currentCar = null;
		}

		var controller = Components.Get<CharacterController>();
		if ( controller != null ) controller.Enabled = true;

		_inCar = false;
		Log.Info( "🚶 Вышел из машины" );
	}
}
