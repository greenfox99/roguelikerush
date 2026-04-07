using Sandbox;
using Sandbox.Rendering;
using System;
using System.Collections.Generic;

public sealed class LightningLineFx : Component
{
	[Property, Group( "Line" )] public Material LineMaterial { get; set; }
	[Property, Group( "Line" )] public Color Tint { get; set; } = Color.Cyan;

	[Property, Group( "Line" )] public float Lifetime { get; set; } = 0.25f;
	[Property, Group( "Line" )] public float Width { get; set; } = 10f;

	[Property, Group( "Line" )] public bool UseCylinder { get; set; } = true;
	[Property, Group( "Line" ), Range( 3, 32 )] public int CylinderSegments { get; set; } = 8;

	[Property, Group( "Lightning" )] public int Segments { get; set; } = 12;
	[Property, Group( "Lightning" )] public float Jitter { get; set; } = 18f;
	[Property, Group( "Lightning" )] public float RefreshRate { get; set; } = 0.03f;

	[Property, Group( "Texture" )] public bool WorldSpaceUv { get; set; } = false;
	[Property, Group( "Texture" )] public float UnitsPerTexture { get; set; } = 256f;
	[Property, Group( "Texture" )] public float TextureScale { get; set; } = 1f;
	[Property, Group( "Texture" )] public float TextureOffset { get; set; } = 0f;

	// ✅ скорость скролла делаем сами, потому что в TrailTextureConfig нет ScrollSpeed
	[Property, Group( "Texture" )] public float TextureScrollSpeed { get; set; } = 35f;
	[Property, Group( "Texture" )] public FilterMode FilterMode { get; set; } = FilterMode.Trilinear;
	[Property, Group( "Texture" )] public TextureAddressMode TextureAddressMode { get; set; } = TextureAddressMode.Wrap;
	[Property, Group( "Texture" )] public bool Clamp { get; set; } = false;

	[Property, Group( "Render" )] public bool Additive { get; set; } = true;
	[Property, Group( "Render" )] public bool Lighting { get; set; } = false;
	[Property, Group( "Render" )] public bool Opaque { get; set; } = false;
	[Property, Group( "Render" )] public float DepthFeather { get; set; } = 128f;

	[Property, Group( "Debug" )] public bool DebugLog { get; set; } = false;

	private LineRenderer _line;
	private Vector3 _startWorld;
	private Vector3 _endWorld;

	private float _dieAt;
	private float _nextRebuildAt;
	private float _scroll;
	private Random _rng;

	public void Setup( Vector3 startWorld, Vector3 endWorld, int seed )
	{
		_startWorld = startWorld;
		_endWorld = endWorld;
		_rng = new Random( seed );

		// GO ставим в старт, а точки будем рисовать в ЛОКАЛЕ
		Transform.Position = _startWorld;
		Transform.Rotation = Rotation.Identity;

		_dieAt = RealTime.Now + MathF.Max( 0.01f, Lifetime );
		_nextRebuildAt = RealTime.Now; // сразу перестроим

		if ( _line == null || !_line.IsValid )
		{
			_line = Components.Get<LineRenderer>();
			if ( _line == null ) _line = Components.Create<LineRenderer>();
		}

		ApplyRendererSettings();
		RebuildPoints();
	}

	protected override void OnStart()
	{
		_line = Components.Get<LineRenderer>();
		if ( _line == null ) _line = Components.Create<LineRenderer>();

		_line.Enabled = true;
	}

	protected override void OnUpdate()
	{
		if ( _line == null || !_line.IsValid )
			return;

		float now = RealTime.Now;

		// перестраиваем "рваность" молнии
		if ( RefreshRate > 0f && now >= _nextRebuildAt )
		{
			_nextRebuildAt = now + RefreshRate;
			RebuildPoints();
		}

		// ✅ скроллим текстуру сами через Scroll
		_scroll += TextureScrollSpeed * RealTime.Delta;

		var tx = _line.Texturing;
		tx.Material = LineMaterial;
		tx.WorldSpace = WorldSpaceUv;
		tx.UnitsPerTexture = MathF.Max( 1f, UnitsPerTexture );
		tx.Scale = TextureScale;
		tx.Offset = TextureOffset;
		tx.Scroll = _scroll;
		tx.FilterMode = FilterMode;
		tx.TextureAddressMode = TextureAddressMode;
		tx.Clamp = Clamp;
		_line.Texturing = tx;

		// умираем
		if ( now >= _dieAt )
			GameObject.Destroy();
	}

	private void ApplyRendererSettings()
	{
		_line.UseVectorPoints = true;

		_line.Face = UseCylinder ? SceneLineObject.FaceMode.Cylinder : SceneLineObject.FaceMode.Camera;
		_line.CylinderSegments = Math.Max( 3, CylinderSegments );

		_line.Additive = Additive;
		_line.Lighting = Lighting;
		_line.Opaque = Opaque;
		_line.DepthFeather = DepthFeather;

		// цвет
		var g = new Gradient();
		g.AddColor( 0f, Tint );
		g.AddColor( 1f, Tint );
		g.AddAlpha( 0f, 1f );
		g.AddAlpha( 1f, 1f );
		_line.Color = g;

		// толщина
		var w = new Curve();
		w.AddPoint( 0f, Width );
		w.AddPoint( 1f, Width );
		_line.Width = w;

		_line.StartCap = SceneLineObject.CapStyle.None;
		_line.EndCap = SceneLineObject.CapStyle.None;

		if ( DebugLog )
			Log.Info( $"[LightningLineFx] ApplySettings ok. Face={_line.Face} Width={Width} Mat={(LineMaterial != null ? LineMaterial.Name : "NULL")}" );
	}

	private void RebuildPoints()
	{
		var deltaWorld = _endWorld - _startWorld;
		float len = deltaWorld.Length;

		if ( len < 1f )
		{
			_line.VectorPoints = new List<Vector3> { Vector3.Zero, deltaWorld };
			return;
		}

		var fwd = deltaWorld.Normal;

		var right = fwd.Cross( Vector3.Up );
		if ( right.Length < 0.01f )
			right = fwd.Cross( Vector3.Right );
		right = right.Normal;

		var up = right.Cross( fwd ).Normal;

		int seg = Math.Max( 2, Segments );
		var pts = new List<Vector3>( seg + 1 );

		for ( int i = 0; i <= seg; i++ )
		{
			float t = i / (float)seg;

			// локальная линия (от 0 до deltaWorld)
			var p = deltaWorld * t;

			// рваность: на концах 0, в середине максимум
			float amp = Jitter * MathF.Sin( t * MathF.PI );

			float rx = (float)(_rng.NextDouble() * 2.0 - 1.0);
			float ry = (float)(_rng.NextDouble() * 2.0 - 1.0);

			var off = (right * rx + up * ry) * amp;

			pts.Add( p + off );
		}

		_line.VectorPoints = pts;
	}
}
