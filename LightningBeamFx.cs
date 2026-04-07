using Sandbox;
using System;
using System.Collections.Generic;

public sealed class LightningBeamFx : Component
{
	[Property] public LineRenderer Line { get; set; }

	// “живая” молния (ломаная)
	[Property] public int Segments { get; set; } = 8;
	[Property] public float Jitter { get; set; } = 22f;
	[Property] public float JitterSpeed { get; set; } = 22f;

	private Vector3 _start;
	private Vector3 _end;

	private float _dieAtReal;
	private readonly List<Vector3> _pts = new();

	public void Init( Vector3 start, Vector3 end, float lifetime )
	{
		_start = start;
		_end = end;

		_dieAtReal = RealTime.Now + MathF.Max( 0.02f, lifetime );

		Line ??= Components.Get<LineRenderer>( FindMode.EnabledInSelfAndDescendants );
		if ( Line == null ) return;

		Line.UseVectorPoints = true;

		_pts.Clear();
		int count = Math.Max( 2, Segments + 1 );
		for ( int i = 0; i < count; i++ )
			_pts.Add( start );

		Line.VectorPoints = _pts;

		UpdatePoints(); // сразу выставим
	}

	protected override void OnUpdate()
	{
		if ( RealTime.Now >= _dieAtReal )
		{
			GameObject.Destroy();
			return;
		}

		UpdatePoints();
	}

	private void UpdatePoints()
	{
		if ( Line == null || !Line.IsValid ) return;
		if ( _pts.Count < 2 ) return;

		Vector3 dir = (_end - _start);
		float len = dir.Length;
		if ( len < 0.001f )
		{
			_pts[0] = _start;
			_pts[^1] = _end;
			return;
		}
		dir /= len;

		// два перпендикуляра (чтобы джиттер был “в стороны”)
		Vector3 right = dir.Cross( Vector3.Up );
		if ( right.Length < 0.01f ) right = dir.Cross( Vector3.Right );
		right = right.Normal;
		Vector3 up = right.Cross( dir ).Normal;

		float time = RealTime.Now * JitterSpeed;

		int count = _pts.Count;
		for ( int i = 0; i < count; i++ )
		{
			float t = (float)i / (count - 1);

			Vector3 p = Vector3.Lerp( _start, _end, t );

			// концы не дёргаем, середину дёргаем сильнее
			if ( i != 0 && i != count - 1 && Jitter > 0f )
			{
				float falloff = MathF.Sin( t * MathF.PI ); // 0..1..0
				float a = Jitter * falloff;

				float n1 = MathF.Sin( time + i * 1.37f );
				float n2 = MathF.Cos( time * 1.12f + i * 1.91f );

				p += right * (n1 * a) + up * (n2 * a * 0.65f);
			}

			_pts[i] = p;
		}

		// важно: список уже привязан к Line.VectorPoints, мы его просто меняем
	}
}
