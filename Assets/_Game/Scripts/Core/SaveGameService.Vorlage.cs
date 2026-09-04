using System;
using UnityEngine;

namespace Eidren.Core.Services
{
	/// <summary>
	/// Zweiter Teil von SaveGameService (partial): Vorlage-Spielstaende des
	/// Tester-Pakets. Entweder einmalig als Datei angelegt (nur wenn noch kein
	/// eigener Spielstand existiert) oder direkt in die laufende Sitzung
	/// uebernommen ("Neues Spiel" der Testfassung).
	/// </summary>
	public sealed partial class SaveGameService
	{
		/// <summary>
		/// Legt einen mitgelieferten Vorlage-Spielstand an (Tester-Paket mit
		/// Voll-Ausbau-Stand). Ein bereits vorhandener Spielstand wird niemals
		/// ueberschrieben — der Fortschritt des Testers bleibt unangetastet.
		/// </summary>
		public bool TrySeedFromTemplate(string json, out string error)
		{
			EnsureInitialized();
			if (string.IsNullOrWhiteSpace(json))
			{
				error = "Vorlage-Spielstand ist leer.";
				return false;
			}
			if (HasAnySaveFile())
			{
				error = "Es existiert bereits ein Spielstand.";
				return false;
			}
			SaveGameData data;
			try
			{
				data = JsonUtility.FromJson<SaveGameData>(json);
			}
			catch (Exception exception)
			{
				error = "Vorlage-Spielstand ist unlesbar: " + exception.Message;
				return false;
			}
			if (data == null || data.saveVersion <= 0 || data.player == null || data.progression == null)
			{
				error = "Vorlage-Spielstand ist unvollstaendig.";
				return false;
			}
			try
			{
				_files.CreateDirectory(_rootPath);
				_files.WriteAllText(MainPath, json);
			}
			catch (Exception exception2)
			{
				error = "Vorlage-Spielstand liess sich nicht schreiben: " + exception2.Message;
				return false;
			}
			error = string.Empty;
			this.AvailabilityChanged?.Invoke(HasRecoverableSave());
			return true;
		}

		/// <summary>
		/// Uebernimmt einen Vorlage-Spielstand direkt in die laufende Sitzung
		/// (Tester-Paket: „Neues Spiel" startet im Voll-Ausbau-Stand). Die
		/// Spielstanddatei bleibt unberuehrt, bis regulaer gespeichert wird.
		/// </summary>
		public bool TryApplyTemplate(string json, out string error)
		{
			EnsureInitialized();
			if (string.IsNullOrWhiteSpace(json))
			{
				error = "Vorlage-Spielstand ist leer.";
				return false;
			}
			SaveGameData data;
			try
			{
				data = JsonUtility.FromJson<SaveGameData>(json);
			}
			catch (Exception exception)
			{
				error = "Vorlage-Spielstand ist unlesbar: " + exception.Message;
				return false;
			}
			if (data == null)
			{
				error = "Vorlage-Spielstand ist unvollstaendig.";
				return false;
			}
			return TryRestoreFrom(data, out error);
		}
	}
}
