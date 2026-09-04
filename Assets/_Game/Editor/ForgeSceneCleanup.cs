using Eidren.Composition;
using System.Text;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Eidren.Editor
{
	/// <summary>
	/// Die Schmiedeszene wurde seinerzeit als Kopie von Zone_EmberRuins.unity
	/// angelegt und trug deshalb noch das komplette Aussengelaende: Bodenplatte,
	/// Felsen, Bewuchs - und eine Kopie des Eingangsportals, das eigentlich in den
	/// Emberruinen steht. Weil das NavMesh mit CollectObjects.All gebacken wird,
	/// gehoerte dieses Gelaende zur begehbaren Flaeche.
	/// </summary>
	public static class ForgeSceneCleanup
	{
		/// <summary>Ausdehnung des Grundrisses laut EidraForgeLayout: X -36..36, Z -38..50.</summary>
		private static readonly Vector3 BodenMitte = new Vector3(0f, -0.5f, 6f);

		private static readonly Vector3 BodenGroesse = new Vector3(72f, 1f, 88f);

		/// <summary>
		/// Entfernt die geerbten Aussenobjekte. Laeuft als fester Teil von
		/// EidraForgeSceneBuilder.Build, damit ein Neubau der Szene sie nicht
		/// zurueckholt.
		/// </summary>
		public static void Aufraeumen(Scene szene, ZoneController zone)
		{
			foreach (GameObject wurzel in szene.GetRootGameObjects())
			{
				/* Das Portal gehoert nach Zone_EmberRuins. In der Schmiede selbst
				   waere es ein zweiter Eingang mitten im Verlies. */
				if (wurzel.name == "EidraForgeEntrance")
				{
					Object.DestroyImmediate(wurzel);
					break;
				}
			}

			if (zone.EnvironmentRoot != null)
			{
				Transform areaArt = zone.EnvironmentRoot.Find("AreaArt");
				if (areaArt != null)
				{
					Object.DestroyImmediate(areaArt.gameObject);
				}
			}

			/* WalkableGround bleibt stehen: ZoneController verlangt die Referenz
			   (Require in GetValidationErrors), und LootDropPositionResolver sowie
			   BuildingPlacementSupport lesen daraus die Bodenhoehe. Sichtbar sein
			   muss sie aber nicht, und ihre Ausdehnung wird auf den Grundriss
			   gestutzt, damit Beute nicht ausserhalb des Verlieses landet. */
			if (zone.WalkableGround != null)
			{
				Renderer[] renderer = zone.WalkableGround.GetComponentsInChildren<Renderer>(includeInactive: true);
				foreach (Renderer einzeln in renderer)
				{
					einzeln.enabled = false;
				}
				/* Aus dem NavMesh nehmen. Die Flaeche spannt sich als geschlossener
				   Kasten ueber den ganzen Grundriss, ihre Oberkante liegt auf 0 -
				   gebacken wuerde daraus eine flache Laufebene ueber Grube und
				   Rampen, und der Abstieg in die Essenkammer waere unerreichbar.
				   Der Collider bleibt, weil ZoneController ihn verlangt und
				   LootDropPositionResolver darauf raycastet. */
				NavMeshModifier ausnahme = zone.WalkableGround.GetComponent<NavMeshModifier>();
				if (ausnahme == null)
				{
					ausnahme = zone.WalkableGround.gameObject.AddComponent<NavMeshModifier>();
				}
				ausnahme.ignoreFromBuild = true;
				if (zone.WalkableGround is BoxCollider kasten)
				{
					kasten.transform.position = Vector3.zero;
					kasten.transform.localScale = Vector3.one;
					kasten.center = BodenMitte;
					kasten.size = BodenGroesse;
				}
				else
				{
					Debug.LogWarning("[SchmiedeAufraeumen] WalkableGround ist kein BoxCollider - "
						+ "Ausdehnung wurde nicht angepasst.");
				}
			}
		}

		/// <summary>
		/// Diagnose fuer den Abstieg in die Grube: tastet die Westtreppe Meter fuer
		/// Meter ab und meldet, wo Bodenkacheln liegen und wo das NavMesh sie
		/// tatsaechlich traegt. Ohne diese Messung bleibt jede Korrektur geraten.
		/// </summary>
		[MenuItem("Eidren/V0.2/Schmiede/Treppe vermessen")]
		public static void TreppeVermessen()
		{
			EditorSceneManager.OpenScene(EidraForgeSceneBuilder.ScenePath, OpenSceneMode.Single);
			StringBuilder bericht = new StringBuilder();
			bericht.AppendLine("[Treppe] Westtreppe entlang X bei z=28:");
			for (float x = -21f; x <= -8f; x += 1f)
			{
				EidraForgeLayout.TryFinde(x, 28f, out ForgeFlaeche flaeche);
				bool boden = UnityEngine.AI.NavMesh.SamplePosition(new Vector3(x, 2f, 28f),
					out UnityEngine.AI.NavMeshHit treffer, 10f, UnityEngine.AI.NavMesh.AllAreas);
				bericht.AppendLine(string.Format(
					"  x={0,6:0.0}  Flaeche={1,-12}  NavMesh={2,-5}  y={3,6:0.00}",
					x, flaeche.Name ?? "-", boden, boden ? treffer.position.y : 0f));
			}
			Debug.Log(bericht.ToString());
		}

		[MenuItem("Eidren/V0.2/Schmiede/Szenenbestand melden")]
		public static void Bestand()
		{
			Scene szene = EditorSceneManager.OpenScene(EidraForgeSceneBuilder.ScenePath, OpenSceneMode.Single);
			StringBuilder bericht = new StringBuilder();
			bericht.AppendLine("[SchmiedeBestand] Wurzelobjekte der Szene:");
			foreach (GameObject wurzel in szene.GetRootGameObjects())
			{
				Renderer[] renderer = wurzel.GetComponentsInChildren<Renderer>(includeInactive: true);
				bericht.AppendLine(
					$"  - {wurzel.name} (aktiv={wurzel.activeSelf}, Kinder={wurzel.transform.childCount}, Renderer={renderer.Length})");
				if (wurzel.name == "EidraForgeAuthoredLayout")
				{
					continue;
				}
				Melde(bericht, wurzel.transform, 3, tiefe: 1);
			}
			Debug.Log(bericht.ToString());
		}

		private static void Melde(StringBuilder bericht, Transform knoten, int maxTiefe, int tiefe)
		{
			if (tiefe > maxTiefe)
			{
				return;
			}
			for (int index = 0; index < knoten.childCount; index++)
			{
				Transform kind = knoten.GetChild(index);
				int gesamt = kind.GetComponentsInChildren<Renderer>(includeInactive: true).Length;
				bericht.AppendLine(
					$"{new string(' ', tiefe * 4 + 4)}* {kind.name} (Kinder={kind.childCount}, Renderer={gesamt})");
				Melde(bericht, kind, maxTiefe, tiefe + 1);
			}
		}
	}
}
