using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Eidren.Presentation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Eidren.Tests.EditMode
{
	/// <summary>
	/// Prueft die Clipaufloesung der Kreaturenschicht und — das ist der
	/// eigentliche Zweck — dass die Namenstabelle zu den Clips passt, die in
	/// den GLB-Dateien tatsaechlich stehen. Ein Tippfehler in der Tabelle waere
	/// sonst erst im Spiel aufgefallen, als stumm stehende Figur.
	/// </summary>
	public sealed class CreatureMeshPresentationTests
	{
		private const string ActorRoot = "Assets/_Game/Art/Actors";

		[TestCase(ActorVisualState.Idle, "Ruhe")]
		[TestCase(ActorVisualState.Move, "Gehen")]
		[TestCase(ActorVisualState.Ability1, "Telegraph")]
		[TestCase(ActorVisualState.Ability2, "Angriff")]
		[TestCase(ActorVisualState.Hit, "Treffer")]
		[TestCase(ActorVisualState.Stagger, "Taumeln")]
		[TestCase(ActorVisualState.Death, "Tod")]
		[TestCase(ActorVisualState.Appear, "Erscheinen")]
		public void ResolveClipName_DecktAlleZustaendeAb(ActorVisualState state, string expected)
		{
			Assert.That(CreatureMeshPresentation.ResolveClipName(state), Is.EqualTo(expected));
		}

		[TestCase("idle", "Ruhe")]
		[TestCase("move", "Gehen")]
		[TestCase("attack", "Angriff")]
		[TestCase("wildattack", "Wildangriff")]
		[TestCase("flee", "Flucht")]
		[TestCase("shadowstep", "Schattenschritt")]
		[TestCase("backmark", "Schattenmal")]
		[TestCase("rockbreaker", "Felsbrecher")]
		[TestCase("stonehide", "Steinhaut")]
		[TestCase("front", "Frontschlag")]
		[TestCase("spin", "Wirbel")]
		[TestCase("charge", "Ansturm")]
		[TestCase("return", "Rueckkehr")]
		public void StemToClip_UebersetztAlleSpriteZustaende(string stem, string expected)
		{
			Assert.That(CreatureMeshPresentation.StemToClip(stem), Is.EqualTo(expected));
		}

		[TestCase("")]
		[TestCase(null)]
		[TestCase("gibtesnicht")]
		public void StemToClip_LiefertNullStattErsatz(string stem)
		{
			// Kein prozeduraler Rueckfall: ein unbekannter Stamm ist ein
			// Datenfehler und soll auffallen.
			Assert.That(CreatureMeshPresentation.StemToClip(stem), Is.Null);
		}

		/// <summary>
		/// Jede gebaute Kreatur samt der Clips, die ihre GLB laut README traegt.
		/// </summary>
		private static readonly (string Figur, string[] Clips)[] Kreaturen =
		{
			("Riftling",      new[] { "Ruhe", "Gehen", "Telegraph", "Angriff", "Treffer", "Taumeln", "Tod", "Erscheinen" }),
			("MoorThrower",   new[] { "Ruhe", "Gehen", "Telegraph", "Angriff", "Treffer", "Taumeln", "Tod", "Erscheinen" }),
			("RiftGuardian",  new[] { "Ruhe", "Gehen", "Telegraph", "Angriff", "Treffer", "Taumeln", "Tod", "Erscheinen" }),
			("ForgeGuardian", new[] { "Ruhe", "Gehen", "Telegraph", "Angriff", "Treffer", "Taumeln", "Tod", "Erscheinen" }),
			("SealGuardian",  new[] { "Ruhe", "Gehen", "Telegraph", "Angriff", "Treffer", "Taumeln", "Tod", "Erscheinen" }),
			("CoreGuardian",  new[] { "Ruhe", "Gehen", "Telegraph", "Angriff", "Treffer", "Taumeln", "Tod", "Erscheinen" }),
			("EmberEater",    new[] { "Ruhe", "Gehen", "Telegraph", "Angriff", "Treffer", "Taumeln", "Tod", "Erscheinen" }),
			("RootCharger",   new[] { "Ruhe", "Gehen", "Telegraph", "Angriff", "Treffer", "Taumeln", "Tod", "Erscheinen" }),
			("GraniteShell",  new[] { "Ruhe", "Gehen", "Telegraph", "Angriff", "Treffer", "Taumeln", "Tod", "Erscheinen" }),
			("AshRunner",     new[] { "Ruhe", "Gehen", "Telegraph", "Angriff", "Treffer", "Taumeln", "Tod", "Erscheinen" }),
			("Ignivar",       new[] { "Ruhe", "Gehen", "Telegraph", "Angriff", "Treffer", "Taumeln", "Tod", "Erscheinen" }),
			("Noctarion",     new[] { "Ruhe", "Gehen", "Telegraph", "Wildangriff", "Treffer", "Taumeln", "Erscheinen", "Flucht", "Schattenschritt", "Schattenmal" }),
			("Terrock",       new[] { "Ruhe", "Gehen", "Telegraph", "Wildangriff", "Treffer", "Taumeln", "Erscheinen", "Flucht", "Felsbrecher", "Steinhaut" }),
			("Garon",         new[] { "Ruhe", "Gehen", "Frontschlag", "Wirbel", "Ansturm", "Rueckkehr", "Treffer", "Taumeln", "Tod" }),
			// Ur-Figur, nachtraeglich in die Serie geholt (13.08.2026):
			// Erscheinen wurde aus riftling.html portiert.
			("Wildling",      new[] { "Ruhe", "Gehen", "Telegraph", "Angriff", "Treffer", "Erscheinen", "Taumeln", "Tod" })
		};

		[Test]
		public void JedeProduktiveKreatur_TraegtGenauDieErwartetenClipsUndLegacyIstArchiviert()
		{
			List<string> fehler = new List<string>();
			foreach ((string figur, string[] erwartet) in Kreaturen)
			{
				string legacyPfad = ActorRoot + "/" + figur + "/" + figur + "3D/" + figur + ".glb";
				if (File.Exists(legacyPfad)) fehler.Add(figur + ": Legacy-GLB liegt noch im Runtime-Baum (" + legacyPfad + ")");
				if (!File.Exists("Documentation/Etappen/MidPoly/LegacyArchive/" + legacyPfad))
					fehler.Add(figur + ": Legacy-GLB fehlt im Archiv (" + legacyPfad + ")");

				string prefabPfad = "Assets/_Game/Prefabs/Actors/3D/" + figur + "_3D.prefab";
				GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPfad);
				if (prefab == null)
				{
					fehler.Add(figur + ": produktives Prefab fehlt (" + prefabPfad + ")");
					continue;
				}
				if (prefab.GetComponent<LODGroup>() == null || prefab.GetComponent<LODGroup>().GetLODs().Length != 3)
					fehler.Add(figur + ": produktiver LOD0/1/2-Vertrag fehlt");
				if (prefab.GetComponentInChildren<SkinnedMeshRenderer>(true) == null)
					fehler.Add(figur + ": produktives SkinnedMesh fehlt");
				string[] tatsaechlich = AssetDatabase.GetDependencies(prefabPfad, true)
					.SelectMany(AssetDatabase.LoadAllAssetsAtPath).OfType<AnimationClip>()
					.Where(clip => !clip.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase))
					.Select(clip => clip.name).Distinct(StringComparer.Ordinal).OrderBy(name => name, StringComparer.Ordinal).ToArray();
				foreach (string clip in erwartet)
				{
					if (Array.IndexOf(tatsaechlich, clip) < 0)
					{
						fehler.Add(figur + ": Clip \"" + clip + "\" fehlt im produktiven Prefab");
					}
				}
				if (tatsaechlich.Length != erwartet.Length)
				{
					fehler.Add(figur + ": " + tatsaechlich.Length + " Clips in der GLB, "
						+ erwartet.Length + " erwartet (" + string.Join(", ", tatsaechlich) + ")");
				}
			}
			Assert.That(fehler, Is.Empty, string.Join("\n", fehler));
		}

		[Test]
		public void JederClipnameDerKreaturen_IstUeberEinenStammErreichbar()
		{
			// Gegenprobe zur Tabelle: was in einer GLB steht, muss die Schicht
			// auch ansteuern koennen — sonst waere der Clip totes Gewicht.
			HashSet<string> erreichbar = new HashSet<string>(StringComparer.Ordinal);
			foreach (string stem in new[]
			{
				"idle", "move", "hit", "stagger", "death", "appear", "telegraph", "attack",
				"wildattack", "flee", "shadowstep", "backmark", "rockbreaker", "stonehide",
				"front", "spin", "charge", "return"
			})
			{
				string clip = CreatureMeshPresentation.StemToClip(stem);
				if (clip != null)
				{
					erreichbar.Add(clip);
				}
			}
			List<string> fehler = new List<string>();
			foreach ((string figur, string[] clips) in Kreaturen)
			{
				foreach (string clip in clips)
				{
					if (!erreichbar.Contains(clip))
					{
						fehler.Add(figur + ": Clip \"" + clip + "\" ist ueber keinen Stamm erreichbar");
					}
				}
			}
			Assert.That(fehler, Is.Empty, string.Join("\n", fehler));
		}

		/// <summary>Sollhoehen wie in CreatureMeshBuilder.Creatures.</summary>
		private static readonly Dictionary<string, float> Sollhoehen =
			new Dictionary<string, float>(StringComparer.Ordinal)
			{
				{ "Riftling", 1.70f }, { "MoorThrower", 1.90f }, { "RiftGuardian", 3.00f },
				{ "ForgeGuardian", 2.50f }, { "SealGuardian", 3.00f }, { "CoreGuardian", 4.50f },
				{ "EmberEater", 1.60f }, { "RootCharger", 2.20f }, { "GraniteShell", 2.40f },
				{ "AshRunner", 1.80f }, { "Ignivar", 1.00f }, { "Noctarion", 1.00f },
				{ "Terrock", 1.00f }, { "Garon", 4.50f }, { "Wildling", 1.90f }
			};

		[Test]
		public void JedesPrefab_TraegtSchichtHoeheUndAlleClips()
		{
			// Abnahme des Einbaus. Prueft das Ergebnis von
			// CreatureMeshBuilder.BuildAll(), nicht den Builder selbst.
			List<string> fehler = new List<string>();
			foreach ((string figur, string[] erwartet) in Kreaturen)
			{
				string pfad = "Assets/_Game/Prefabs/Actors/3D/" + figur + "_3D.prefab";
				var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(pfad);
				if (prefab == null)
				{
					fehler.Add(figur + ": Prefab fehlt (" + pfad + ")");
					continue;
				}

				var schicht = prefab.GetComponent<CreatureMeshPresentation>();
				if (schicht == null)
				{
					fehler.Add(figur + ": CreatureMeshPresentation fehlt am Prefab");
					continue;
				}
				float soll = Sollhoehen[figur];
				if (UnityEngine.Mathf.Abs(schicht.WorldHeight - soll) > 0.001f)
				{
					fehler.Add(figur + ": Sollhoehe " + schicht.WorldHeight
						+ " statt " + soll);
				}

				var animation = prefab.GetComponentInChildren<UnityEngine.Animation>(true);
				if (animation == null)
				{
					fehler.Add(figur + ": Animation-Komponente fehlt — steht der Import auf Legacy?");
					continue;
				}
				var vorhanden = new List<string>();
				foreach (UnityEngine.AnimationState zustand in animation)
				{
					vorhanden.Add(zustand.name);
				}
				foreach (string clip in erwartet)
				{
					if (!vorhanden.Contains(clip))
					{
						fehler.Add(figur + ": Clip \"" + clip + "\" fehlt am Prefab (vorhanden: "
							+ string.Join(", ", vorhanden) + ")");
					}
				}
			}
			Assert.That(fehler, Is.Empty, string.Join("\n", fehler));
		}

		/// <summary>
		/// Bisher auf 3D umgestellte Gegner. Die Liste steht ausdruecklich hier
		/// und nicht als "alles, was einen Mesh3D-Knoten hat" — sonst wuerde ein
		/// versehentlich NICHT umgebauter Gegner stillschweigend durchrutschen.
		/// Ignivar, Noctarion und Terrock fehlen: sie haben kein eigenes
		/// Gegner-Prefab, ihre Darstellung laedt EidraWildController zur
		/// Laufzeit aus Resources.
		/// </summary>
		private static readonly string[] UmgebauteGegner =
		{
			"Riftling", "MoorThrower", "RiftGuardian", "RootCharger", "GraniteShell",
			"ForgeGuardian", "SealGuardian", "CoreGuardian", "EmberEater", "AshRunner",
			"Garon"
		};

		/// <summary>
		/// Knoten am Visual, die den Umbau ueberleben muessen. Sie haengen nicht
		/// am Aktorbild: der Bodenschatten bringt einen eigenen Renderer mit und
		/// zielt auf die Wurzel, die Anker sind reine Transformationen.
		/// </summary>
		private static readonly string[] UnberuehrteKnoten =
			{ "GroundShadow", "AbilityAnchor", "BodyAnchor", "FootAnchor" };

		[Test]
		public void UmgebauteGegner_ZeigenAufDieKreaturenschicht()
		{
			// Abnahme des Gegner-Umbaus. Prueft das Ergebnis von
			// CreatureEnemyRewire, nicht das Werkzeug selbst.
			List<string> fehler = new List<string>();
			foreach (string figur in UmgebauteGegner)
			{
				string pfad = Eidren.Editor.CreatureEnemyRewire.EnemyPrefabPath(figur);
				var gegner = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(pfad);
				if (gegner == null)
				{
					fehler.Add(figur + ": Gegner-Prefab fehlt (" + pfad + ")");
					continue;
				}

				// 1. Keine Sprite-Darstellung mehr. ActorPresentationLocator.Find
				//    sucht mit includeInactive: true und nimmt die erste Treffer
				//    im Baum — bliebe die Sprite-Darstellung stehen, entschiede
				//    die Kindreihenfolge, welche Darstellung der Gegner ansteuert.
				var sprites = gegner.GetComponentsInChildren<SpriteActorPresentation>(true);
				if (sprites.Length > 0)
				{
					fehler.Add(figur + ": " + sprites.Length
						+ "x SpriteActorPresentation noch vorhanden");
				}

				// Beim Boss heisst der Knoten "Garon_Visual" — gesucht wird wie
				// im Umbau-Werkzeug: erst der Name, sonst der Animator-Traeger.
				var visual = gegner.transform.Find(Eidren.Editor.CreatureEnemyRewire.VisualNodeName);
				if (visual == null)
				{
					var traeger = gegner
						.GetComponentInChildren<Eidren.Gameplay.Presentation.SpriteActorAnimator>(true);
					visual = (traeger != null) ? traeger.transform : null;
				}
				if (visual == null)
				{
					fehler.Add(figur + ": Visual-Knoten fehlt");
					continue;
				}
				var mesh = visual.Find(Eidren.Editor.CreatureEnemyRewire.MeshNodeName);
				if (mesh == null)
				{
					fehler.Add(figur + ": Knoten \""
						+ Eidren.Editor.CreatureEnemyRewire.MeshNodeName + "\" fehlt unter Visual");
					continue;
				}
				var schicht = mesh.GetComponentInChildren<CreatureMeshPresentation>(true);
				if (schicht == null)
				{
					fehler.Add(figur + ": CreatureMeshPresentation fehlt unter dem Modellknoten");
					continue;
				}

				// 2. Der Animator zeigt ausdruecklich auf die Kreaturenschicht.
				//    Er arbeitet trotz seines Namens nur gegen IActorPresentation
				//    und IAuthoredStatePresentation — beide traegt sie.
				var animator = gegner
					.GetComponentInChildren<Eidren.Gameplay.Presentation.SpriteActorAnimator>(true);
				if (animator == null)
				{
					fehler.Add(figur + ": SpriteActorAnimator fehlt");
				}
				else
				{
					// Ueber SerializedObject gelesen, weil _presentation erst im
					// Awake gesetzt wird und EditMode keine Lebenszyklusaufrufe
					// ausfuehrt — der Getter waere hier immer null.
					var feld = new UnityEditor.SerializedObject(animator)
						.FindProperty("presentationBehaviour");
					var zeigtAuf = (feld == null) ? null : feld.objectReferenceValue;
					if (zeigtAuf != schicht)
					{
						fehler.Add(figur + ": presentationBehaviour zeigt auf "
							+ ((zeigtAuf == null) ? "nichts" : zeigtAuf.GetType().Name)
							+ " statt auf die Kreaturenschicht");
					}
				}

				// 3. Der Locator liefert sie. Das ist der Zweck des Umbaus.
				var gefunden = ActorPresentationLocator.Find(gegner);
				if (!(gefunden is CreatureMeshPresentation))
				{
					fehler.Add(figur + ": Locator liefert "
						+ ((gefunden == null) ? "gar nichts" : gefunden.GetType().Name)
						+ " statt CreatureMeshPresentation");
				}

				// 4. Was stehen bleiben sollte, steht auch. Rekursiv gesucht:
				//    die Forge-Gegner tragen Anker und Schatten eine Ebene
				//    tiefer, im eingebetteten 2D-Knoten.
				foreach (string noetig in UnberuehrteKnoten)
				{
					if (FindeKnoten(gegner.transform, noetig) == null)
					{
						fehler.Add(figur + ": Knoten \"" + noetig
							+ "\" ist beim Umbau verlorengegangen");
					}
				}

				// 5. Genau ein Animator und ein Sprite-VFX — die Forge-Gegner
				//    trugen beides doppelt (eingebettetes 2D-Prefab); nach dem
				//    Umbau wuerden zwei Animatoren dieselbe Schicht ansteuern.
				int animatoren = gegner
					.GetComponentsInChildren<Eidren.Gameplay.Presentation.SpriteActorAnimator>(true).Length;
				if (animatoren != 1)
				{
					fehler.Add(figur + ": " + animatoren + " SpriteActorAnimator statt 1");
				}
			}
			Assert.That(fehler, Is.Empty, string.Join("\n", fehler));
		}

		/// <summary>Tiefensuche nach einem Knotennamen.</summary>
		private static UnityEngine.Transform FindeKnoten(UnityEngine.Transform wurzel, string name)
		{
			if (wurzel.name == name)
			{
				return wurzel;
			}
			for (int i = 0; i < wurzel.childCount; i++)
			{
				var treffer = FindeKnoten(wurzel.GetChild(i), name);
				if (treffer != null)
				{
					return treffer;
				}
			}
			return null;
		}

		/// <summary>
		/// Liest die Animationsnamen direkt aus dem JSON-Teil der GLB. Bewusst
		/// ohne Unity-Import: der Test soll auch dann gruen sein, wenn die GLB
		/// im Projekt noch nicht importiert ist.
		/// </summary>
		private static string[] ClipNamenAusGlb(string pfad)
		{
			byte[] daten = File.ReadAllBytes(pfad);
			int jsonLaenge = BitConverter.ToInt32(daten, 12);
			string json = System.Text.Encoding.UTF8.GetString(daten, 20, jsonLaenge);
			int start = json.IndexOf("\"animations\"", StringComparison.Ordinal);
			if (start < 0)
			{
				return Array.Empty<string>();
			}
			// Das Ende der Liste ueber die Klammerbilanz suchen, nicht ueber das
			// erste "],": jede Animation enthaelt selbst Arrays ("samplers",
			// "channels"), sonst braeche der Abschnitt nach dem ersten Clip ab.
			int auf = json.IndexOf('[', start);
			if (auf < 0)
			{
				return Array.Empty<string>();
			}
			int tiefe = 0, ende = json.Length;
			for (int k = auf; k < json.Length; k++)
			{
				if (json[k] == '[')
				{
					tiefe++;
				}
				else if (json[k] == ']')
				{
					tiefe--;
					if (tiefe == 0)
					{
						ende = k + 1;
						break;
					}
				}
			}
			string abschnitt = json.Substring(auf, ende - auf);
			List<string> namen = new List<string>();
			const string marke = "\"name\":\"";
			int i = abschnitt.IndexOf(marke, StringComparison.Ordinal);
			while (i >= 0)
			{
				int von = i + marke.Length;
				int bis = abschnitt.IndexOf('"', von);
				if (bis < 0)
				{
					break;
				}
				namen.Add(abschnitt.Substring(von, bis - von));
				i = abschnitt.IndexOf(marke, bis, StringComparison.Ordinal);
			}
			return namen.ToArray();
		}
	}
}
