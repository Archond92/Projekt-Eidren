using Eidren.AI;
using Eidren.Composition;
using System.Collections.Generic;
using System;
using UnityEngine.SceneManagement;
using UnityEngine;

namespace Eidren.Tests.PlayMode
{
	/// <summary>
	/// Gemeinsamer Testaufbau für PlayMode-Tests.
	///
	/// Anlass (19./20.08.2026): In einer einzigen Runde sind DREI Tests
	/// ordnungsabhängig geworden — gefiltert grün, im Voll-Lauf rot. Jedes Mal
	/// überlebte etwas Globales den einzelnen Test:
	///
	/// 1. <c>CombatTargetRegistry</c> hielt Gegner aus früheren Tests, und eine
	///    Fähigkeit traf einen davon statt der eigenen Kreatur — ohne
	///    Fehlermeldung, nur ohne Wirkung.
	/// 2. <c>Camera.main</c> zeigte auf eine Kamera aus einer früher geladenen
	///    Szene, sodass der Sichtstrahl von der falschen Stelle lief.
	/// 3. Die Zonen werden pro Lauf neu gewürfelt (ungesetzte Saat), weshalb ein
	///    echter Regressionsfund wie Zufall aussah.
	///
	/// Statt in jedem Test eine eigene Reinigung zu erfinden, gibt es hier eine.
	/// Der Rückgabewert stellt beim Verwerfen den Vorzustand wieder her.
	///
	/// NICHT abgedeckt und weiterhin Sache des einzelnen Tests: Wartebudgets
	/// gehören in BILDER oder in dieselbe Zeitbasis wie der geprüfte Code —
	/// <c>Time.deltaTime</c> (gedeckelt) und <c>Time.unscaledDeltaTime</c>
	/// (sehr klein) laufen im Batchmodus weit auseinander.
	/// </summary>
	public static class Testumgebung
	{
		/// <summary>Feste Saat, damit Zonen in jedem Lauf gleich gewürfelt werden.</summary>
		public const int Saat = 20260820;

		private static readonly string[] Spielszenen = { "Bootstrap", "MainMenu", "WorldMap", "HomeBase", "EidraForge" };

		private static int _leereWelten;

		public sealed class Sitzung : IDisposable
		{
			private readonly List<GameObject> _enttaggteKameras = new List<GameObject>();

			private bool _verworfen;

			internal void KameraMerken(GameObject kamera)
			{
				_enttaggteKameras.Add(kamera);
			}

			public void Dispose()
			{
				if (_verworfen)
				{
					return;
				}
				_verworfen = true;
				foreach (GameObject kamera in _enttaggteKameras)
				{
					if (kamera != null)
					{
						kamera.tag = "MainCamera";
					}
				}
			}
		}

		/// <summary>
		/// Räumt die globalen Zustände ab, die zwischen Tests überleben.
		/// </summary>
		/// <param name="fremdeGegnerAbschalten">
		/// Schaltet vorhandene Gegner ab — sie tragen sich damit aus der
		/// Kampfziel-Liste aus. Nur für Tests, die ihre eigene Kreatur stellen.
		/// </param>
		/// <param name="eigeneHauptkamera">
		/// Wird diese Kamera übergeben, verlieren alle anderen Hauptkameras
		/// vorübergehend ihr Tag, damit <c>Camera.main</c> eindeutig ist.
		/// </param>
		public static Sitzung Bereitstellen(bool fremdeGegnerAbschalten = false, GameObject eigeneHauptkamera = null)
		{
			Sitzung sitzung = new Sitzung();
			SaatFestlegen();
			if (fremdeGegnerAbschalten)
			{
				foreach (EnemyControllerBase gegner in UnityEngine.Object.FindObjectsByType<EnemyControllerBase>(FindObjectsInactive.Include, FindObjectsSortMode.None))
				{
					if (gegner != null)
					{
						gegner.gameObject.SetActive(value: false);
					}
				}
			}
			if (eigeneHauptkamera != null)
			{
				foreach (Camera kamera in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
				{
					if (kamera != null && kamera.gameObject != eigeneHauptkamera && kamera.CompareTag("MainCamera"))
					{
						sitzung.KameraMerken(kamera.gameObject);
						kamera.tag = "Untagged";
					}
				}
				eigeneHauptkamera.tag = "MainCamera";
			}
			return sitzung;
		}

		/// <summary>
		/// Laedt eine Zone ab und hinterlaesst eine leere Welt.
		///
		/// Wer eine Zone laedt, raeumt sie ab: Mehrere Tests bauen ihre
		/// Objekte am WELT-URSPRUNG auf (der Tuertest etwa Boden, Tuer und
		/// Figur) und laden keine eigene Szene. Bleibt eine Zone liegen,
		/// stehen sie mitten in deren Geometrie — gefiltert gruen, im
		/// Voll-Lauf rot. Genau so ist der Tuertest am 20.08.2026
		/// umgefallen, nachdem ein neuer Test eine Zone liegen liess.
		/// </summary>
		// Voll qualifiziert: Mit „using System.Collections.Generic" loest
		// IEnumerator sonst auf die generische Fassung auf (CS0305).
		public static System.Collections.IEnumerator LeereWeltHinterlassen(string zonenName)
		{
			Scene leer = NeueLeereWelt();
			Scene zone = SceneManager.GetSceneByName(zonenName);
			if (zone.IsValid() && zone.isLoaded && zone != leer)
			{
				yield return SceneManager.UnloadSceneAsync(zone);
			}
			yield return null;
		}

		/// <summary>
		/// Sorgt VOR dem eigenen Aufbau fuer eine leere Welt und laedt jede
		/// Spielszene ab, die noch offen ist.
		///
		/// Das Gegenstueck zu <see cref="LeereWeltHinterlassen"/> — und das
		/// belastbarere Ende: Wer am Weltursprung baut, verlaesst sich damit
		/// nicht mehr darauf, dass alle anderen Tests hinter sich aufraeumen.
		/// Ein neuer zonenladender Test kann diese Tests dann nicht mehr
		/// umwerfen, auch wenn sein Autor die Regel nicht kennt.
		/// </summary>
		public static System.Collections.IEnumerator LeereWeltBereitstellen()
		{
			Scene leer = NeueLeereWelt();
			for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
			{
				Scene szene = SceneManager.GetSceneAt(i);
				if (!szene.isLoaded || szene == leer || !IstAbraeumbar(szene.name))
				{
					continue;
				}
				yield return SceneManager.UnloadSceneAsync(szene);
			}
			yield return null;
		}

		private static Scene NeueLeereWelt()
		{
			// Fortlaufender Name: Zwei Szenen desselben Namens sind eine
			// Fehlerquelle, und beide Wege hier koennen im selben Lauf
			// mehrfach aufgerufen werden.
			_leereWelten++;
			Scene leer = SceneManager.CreateScene("Testumgebung_Leer_" + _leereWelten);
			SceneManager.SetActiveScene(leer);
			return leer;
		}

		/// <summary>
		/// Nur echte Spielszenen und die eigenen Leerszenen. Die Szene des
		/// Testrunners bleibt unangetastet — sie abzuladen wuerde den Lauf
		/// abbrechen.
		/// </summary>
		private static bool IstAbraeumbar(string name)
		{
			if (name.StartsWith("Zone_", StringComparison.Ordinal)
				|| name.StartsWith("Testumgebung_Leer_", StringComparison.Ordinal))
			{
				return true;
			}
			foreach (string spielszene in Spielszenen)
			{
				if (string.Equals(name, spielszene, StringComparison.Ordinal))
				{
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Nagelt die Zonensaat fest. Ohne das würfelt jeder Lauf die Zonen neu,
		/// und ein grüner Wiederholungslauf beweist nichts.
		/// </summary>
		public static void SaatFestlegen(int saat = Saat)
		{
			EidrenServiceRoot dienste = EidrenServiceRoot.Instance;
			dienste?.GameSession?.ZoneStates?.Reset(saat);
		}
	}
}
