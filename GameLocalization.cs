using Sandbox;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace YourGame.UI;

public static class GameLocalization
{
	private sealed class SaveData
	{
		public string LanguageCode { get; set; } = "ru";
	}

	private const string SavePath = "settings/localization.json";

	private static readonly Dictionary<string, Dictionary<string, string>> _translations =
		new( StringComparer.OrdinalIgnoreCase );

	private static bool _isLoaded;

	public static event Action Changed;

	public static string CurrentCode { get; private set; } = "ru";
	public static bool HasSavedLanguageSelection { get; private set; }

	public static void EnsureLoaded()
	{
		if ( _isLoaded )
			return;

		LoadLanguage( "en" );
		LoadLanguage( "ru" );
		LoadSavedLanguage();

		_isLoaded = true;
	}

	public static void SetLanguage( string code )
	{
		EnsureLoaded();

		code = NormalizeLanguageCode( code );
		bool hadSavedSelection = HasSavedLanguageSelection;
		bool changed = !string.Equals( CurrentCode, code, StringComparison.OrdinalIgnoreCase );

		CurrentCode = code;
		SaveLanguage();

		if ( changed || !hadSavedSelection )
			Changed?.Invoke();
	}

	public static bool IsLanguage( string code )
	{
		EnsureLoaded();
		return string.Equals( CurrentCode, NormalizeLanguageCode( code ), StringComparison.OrdinalIgnoreCase );
	}

	public static void Reload()
	{
		_translations.Clear();
		_isLoaded = false;
		EnsureLoaded();
		Changed?.Invoke();
	}

	public static string T( string token, string fallback = null )
	{
		EnsureLoaded();

		if ( string.IsNullOrWhiteSpace( token ) )
			return fallback ?? string.Empty;

		var key = token.StartsWith( "#", StringComparison.Ordinal ) ? token[1..] : token;

		if ( TryGet( CurrentCode, key, out var value ) )
			return value;

		if ( TryGet( "en", key, out value ) )
			return value;

		return fallback ?? token;
	}

	private static void LoadSavedLanguage()
	{
		HasSavedLanguageSelection = false;

		try
		{
			if ( FileSystem.Data.FileExists( SavePath ) )
			{
				var data = FileSystem.Data.ReadJsonOrDefault<SaveData>( SavePath, new SaveData() );
				CurrentCode = NormalizeLanguageCode( data?.LanguageCode );
				HasSavedLanguageSelection = true;
				return;
			}
		}
		catch ( Exception ex )
		{
			Log.Warning( $"Localization load failed: {ex.Message}" );
		}

		CurrentCode = "ru";
	}

	private static void SaveLanguage()
	{
		try
		{
			FileSystem.Data.CreateDirectory( "settings" );
			FileSystem.Data.WriteJson( SavePath, new SaveData { LanguageCode = CurrentCode } );
			HasSavedLanguageSelection = true;
		}
		catch ( Exception ex )
		{
			Log.Warning( $"Localization save failed: {ex.Message}" );
		}
	}

	private static void LoadLanguage( string languageCode )
	{
		languageCode = NormalizeLanguageCode( languageCode );

		if ( _translations.ContainsKey( languageCode ) )
			return;

		var map = new Dictionary<string, string>( StringComparer.OrdinalIgnoreCase );
		var folder = $"Localization/{languageCode}";

		try
		{
			foreach ( var path in FileSystem.Mounted.FindFile( folder, "*.json", recursive: true ) )
			{
				try
				{
					var json = FileSystem.Mounted.ReadAllText( path );
					var data = JsonSerializer.Deserialize<Dictionary<string, string>>( json );

					if ( data == null )
						continue;

					foreach ( var pair in data )
					{
						if ( string.IsNullOrWhiteSpace( pair.Key ) )
							continue;

						map[pair.Key] = pair.Value ?? string.Empty;
					}
				}
				catch ( Exception ex )
				{
					Log.Warning( $"Localization file parse failed '{path}': {ex.Message}" );
				}
			}
		}
		catch ( Exception ex )
		{
			Log.Warning( $"Localization folder scan failed '{folder}': {ex.Message}" );
		}

		_translations[languageCode] = map;
	}

	private static bool TryGet( string languageCode, string key, out string value )
	{
		value = null;

		if ( !_translations.TryGetValue( languageCode, out var map ) )
			return false;

		return map.TryGetValue( key, out value );
	}

	private static string NormalizeLanguageCode( string code )
	{
		if ( string.IsNullOrWhiteSpace( code ) )
			return "en";

		code = code.Trim().ToLowerInvariant();

		return code switch
		{
			"ru" => "ru",
			"en" => "en",
			_ => "en"
		};
	}
}
