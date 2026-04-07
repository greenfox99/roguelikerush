using Sandbox;
using System;
using System.Linq;

public sealed class CarHUD : Component
{
	// *** НАСТРОЙКИ ОТОБРАЖЕНИЯ ***
	[Property] public bool ShowHUD { get; set; } = true;
	[Property] public Vector2 Position { get; set; } = new Vector2( 20, 20 );
	[Property] public float Width { get; set; } = 250f;
	[Property] public float Height { get; set; } = 100f;
	[Property] public float TextSize { get; set; } = 24f;
	[Property] public float LabelSize { get; set; } = 14f;

	// *** НАСТРОЙКИ ЦВЕТОВ ПО СКОРОСТИ ***
	[Property] public bool UseSpeedColors { get; set; } = true;
	[Property] public float YellowSpeed { get; set; } = 200f;
	[Property] public float OrangeSpeed { get; set; } = 400f;
	[Property] public float RedSpeed { get; set; } = 600f;

	// *** НАСТРОЙКИ ПОЛОСКИ СКОРОСТИ ***
	[Property] public bool ShowSpeedBar { get; set; } = true;
	[Property] public float BarWidth { get; set; } = 200f;
	[Property] public float BarHeight { get; set; } = 10f;

	private Car _currentCar;
	private float _currentSpeed;
	private float _maxSpeed = 6000f;

	protected override void OnUpdate()
	{
		if ( !ShowHUD ) return;

		FindCurrentCar();

		if ( _currentCar != null )
		{
			UpdateSpeed();
			DrawSpeedometer();
		}
	}

	private void FindCurrentCar()
	{
		var cars = Scene.GetAllComponents<Car>();
		foreach ( var car in cars )
		{
			// Прямой доступ к публичному свойству HasDriver
			if ( car.HasDriver )
			{
				_currentCar = car;
				return;
			}
		}
		_currentCar = null;
	}

	private void UpdateSpeed()
	{
		if ( _currentCar == null ) return;

		// Используем публичный метод GetSpeed() если он есть
		_currentSpeed = _currentCar.GetSpeed();

		// MaxSpeed теперь публичное свойство
		_maxSpeed = _currentCar.MaxSpeed;
	}

	private void DrawSpeedometer()
	{
		if ( Scene.Camera == null ) return;

		var hud = Scene.Camera.Hud;

		int speedKmh = (int)(_currentSpeed * 0.1f);

		// Определяем цвет
		Color currentColor = Color.White;
		if ( UseSpeedColors )
		{
			if ( speedKmh > RedSpeed ) currentColor = Color.Red;
			else if ( speedKmh > OrangeSpeed ) currentColor = new Color( 1, 0.5f, 0 );
			else if ( speedKmh > YellowSpeed ) currentColor = Color.Yellow;
		}

		float x = Position.x;
		float y = Position.y;

		// Фон
		hud.DrawRect( new Rect( x, y, Width, Height ), new Color( 0, 0, 0, 0.7f ) );

		// Текст "СКОРОСТЬ"
		var labelScope = new TextRendering.Scope( "СКОРОСТЬ", Color.White, LabelSize );
		hud.DrawText( labelScope, new Vector2( x + 10, y + 5 ) );

		// Значение скорости
		var valueScope = new TextRendering.Scope( $"{speedKmh} км/ч", currentColor, TextSize );
		hud.DrawText( valueScope, new Vector2( x + 10, y + 30 ) );

		// Полоска скорости
		if ( ShowSpeedBar )
		{
			float barX = x + 10;
			float barY = y + Height - BarHeight - 5;

			float speedPercent = 0f;
			if ( _maxSpeed > 0.001f )
			{
				speedPercent = Math.Clamp( _currentSpeed / _maxSpeed, 0f, 1f );
			}

			// Фон полоски
			hud.DrawRect( new Rect( barX, barY, BarWidth, BarHeight ), new Color( 0.3f, 0.3f, 0.3f, 0.5f ) );

			// Заполнение
			hud.DrawRect( new Rect( barX, barY, BarWidth * speedPercent, BarHeight ), currentColor );
		}
	}
}
