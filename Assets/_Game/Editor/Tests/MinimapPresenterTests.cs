using Eidren.Core.Services;
using Eidren.Player;
using Eidren.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace Eidren.Tests.EditMode
{
	/// <summary>
	/// N04-001: Was die Karte aus den angemeldeten Markern macht. EditMode führt
	/// kein Update aus — deshalb ist <c>Refresh</c> öffentlich und wird hier
	/// direkt aufgerufen.
	/// </summary>
	public sealed class MinimapPresenterTests
	{
		private sealed class TestMarker : IMinimapMarker
		{
			public MinimapMarkerKind MinimapKind { get; set; }

			public Vector3 MinimapPosition { get; set; }

			public bool ShowsOnMinimap { get; set; } = true;

			public string MinimapPaletteId { get; set; } = string.Empty;
		}

		private sealed class TestChest : IMinimapDiscoverable
		{
			private bool _seen;

			public MinimapMarkerKind MinimapKind => MinimapMarkerKind.Chest;

			public Vector3 MinimapPosition { get; set; }

			public bool ShowsOnMinimap => _seen;

			public string MinimapPaletteId => string.Empty;

			public void MarkSeenOnMinimap()
			{
				_seen = true;
			}
		}

		private sealed class Rig
		{
			public GameObject Root;

			public MinimapPresenter Presenter;

			public RectTransform MarkerRoot;

			public RectTransform EdgeRoot;

			public PlayerMotor Motor;

			public GameObject PlayerObject;

			public GameObject CameraObject;

			public Sprite BossSymbol;

			public Sprite ChestSymbol;

			public Sprite LineSymbol;

			public MinimapStyle Style;
		}

		private const float Radius = 22f;

		[Test]
		public void GegnerImUmkreis_WirdMitDerGegnerfarbeGezeichnet()
		{
			Rig rig = NewRig();
			TestMarker gegner = new TestMarker { MinimapKind = MinimapMarkerKind.Enemy, MinimapPosition = new Vector3(5f, 0f, 5f) };
			MinimapRegistry.Register(gegner);
			try
			{
				rig.Presenter.Refresh();

				Image[] gezeichnet = ActiveMarkers(rig);
				Assert.That(gezeichnet.Length, Is.EqualTo(1), "Genau ein Gegner muss gezeichnet sein");
				Assert.That(gezeichnet[0].color, Is.EqualTo(rig.Style.enemyColor), "Gegner tragen die Gegnerfarbe");
			}
			finally
			{
				MinimapRegistry.Unregister(gegner);
				Cleanup(rig);
			}
		}

		[Test]
		public void ZielAusserhalbDesRadius_WirdNichtGezeichnet()
		{
			Rig rig = NewRig();
			TestMarker fern = new TestMarker { MinimapKind = MinimapMarkerKind.Enemy, MinimapPosition = new Vector3(0f, 0f, Radius + 5f) };
			MinimapRegistry.Register(fern);
			try
			{
				rig.Presenter.Refresh();

				Assert.That(ActiveMarkers(rig).Length, Is.EqualTo(0), "Jenseits des Kartenradius wird nichts gezeichnet");
			}
			finally
			{
				MinimapRegistry.Unregister(fern);
				Cleanup(rig);
			}
		}

		[Test]
		public void Boss_BekommtDasBosssymbolInSeinerGroesse()
		{
			Rig rig = NewRig();
			TestMarker boss = new TestMarker { MinimapKind = MinimapMarkerKind.Boss, MinimapPosition = new Vector3(2f, 0f, 2f) };
			MinimapRegistry.Register(boss);
			try
			{
				rig.Presenter.Refresh();

				Image[] gezeichnet = ActiveMarkers(rig);
				Assert.That(gezeichnet.Length, Is.EqualTo(1), "Der Boss muss gezeichnet werden");
				Assert.That(gezeichnet[0].sprite, Is.SameAs(rig.BossSymbol), "Der Boss braucht sein eigenes Symbol, keinen Punkt");
				Assert.That(gezeichnet[0].rectTransform.sizeDelta.x, Is.EqualTo(rig.Style.bossSize).Within(0.01f), "Das Bosssymbol ist groesser als ein Gegnerpunkt");
			}
			finally
			{
				MinimapRegistry.Unregister(boss);
				Cleanup(rig);
			}
		}

		[Test]
		public void AbgebauterKnoten_WirdNichtMehrGezeichnet()
		{
			Rig rig = NewRig();
			// Nutzerentscheid 17.08.2026: abgebaute Knoten verschwinden ganz,
			// statt blass stehenzubleiben.
			TestMarker knoten = new TestMarker { MinimapKind = MinimapMarkerKind.Resource, MinimapPosition = new Vector3(3f, 0f, 0f), MinimapPaletteId = "resource.tree", ShowsOnMinimap = false };
			MinimapRegistry.Register(knoten);
			try
			{
				rig.Presenter.Refresh();

				Assert.That(ActiveMarkers(rig).Length, Is.EqualTo(0), "Ein abgebauter Knoten gehoert nicht mehr auf die Karte");
			}
			finally
			{
				MinimapRegistry.Unregister(knoten);
				Cleanup(rig);
			}
		}

		[Test]
		public void KupferaderUndBaum_BekommenVerschiedeneFarben()
		{
			Rig rig = NewRig();
			TestMarker baum = new TestMarker { MinimapKind = MinimapMarkerKind.Resource, MinimapPosition = new Vector3(3f, 0f, 0f), MinimapPaletteId = "resource.tree" };
			TestMarker ader = new TestMarker { MinimapKind = MinimapMarkerKind.Resource, MinimapPosition = new Vector3(-3f, 0f, 0f), MinimapPaletteId = "resource.copper_vein" };
			MinimapRegistry.Register(baum);
			MinimapRegistry.Register(ader);
			try
			{
				rig.Presenter.Refresh();

				Image[] gezeichnet = ActiveMarkers(rig);
				Assert.That(gezeichnet.Length, Is.EqualTo(2), "Beide Knoten gehoeren auf die Karte");
				Assert.That(gezeichnet[0].color, Is.Not.EqualTo(gezeichnet[1].color), "Holz und Kupfer duerfen nicht dieselbe Farbe tragen");
			}
			finally
			{
				MinimapRegistry.Unregister(baum);
				MinimapRegistry.Unregister(ader);
				Cleanup(rig);
			}
		}

		[Test]
		public void UngeseheneKiste_ErscheintErstWennSieImKamerabildLiegt()
		{
			Rig rig = NewRig();
			// Die Kamera blickt entlang +Z. Hinter ihr ist die Kiste im Radius,
			// aber ungesehen.
			TestChest truhe = new TestChest { MinimapPosition = new Vector3(0f, 0f, -8f) };
			MinimapRegistry.Register(truhe);
			try
			{
				rig.Presenter.Refresh();
				Assert.That(ActiveMarkers(rig).Length, Is.EqualTo(0), "Eine ungesehene Kiste darf die Karte nicht verraten");

				truhe.MinimapPosition = new Vector3(0f, 0f, 8f);
				rig.Presenter.Refresh();

				Image[] gezeichnet = ActiveMarkers(rig);
				Assert.That(gezeichnet.Length, Is.EqualTo(1), "Im Bild gesehen gehoert die Kiste auf die Karte");
				Assert.That(gezeichnet[0].sprite, Is.SameAs(rig.ChestSymbol), "Kisten tragen das Kistensymbol");
			}
			finally
			{
				MinimapRegistry.Unregister(truhe);
				Cleanup(rig);
			}
		}

		[Test]
		public void Marker_WerdenWiederverwendetStattJedesMalNeuErzeugt()
		{
			Rig rig = NewRig();
			TestMarker a = new TestMarker { MinimapKind = MinimapMarkerKind.Enemy, MinimapPosition = new Vector3(2f, 0f, 0f) };
			TestMarker b = new TestMarker { MinimapKind = MinimapMarkerKind.Enemy, MinimapPosition = new Vector3(-2f, 0f, 0f) };
			MinimapRegistry.Register(a);
			MinimapRegistry.Register(b);
			try
			{
				rig.Presenter.Refresh();
				int nachZwei = rig.MarkerRoot.childCount;

				MinimapRegistry.Unregister(b);
				rig.Presenter.Refresh();
				rig.Presenter.Refresh();

				Assert.That(rig.MarkerRoot.childCount, Is.EqualTo(nachZwei), "Der Kartentakt darf keine neuen Objekte erzeugen — §8");
				Assert.That(ActiveMarkers(rig).Length, Is.EqualTo(1), "Der entfernte Gegner muss verschwinden, sein Marker aber liegenbleiben");
			}
			finally
			{
				MinimapRegistry.Unregister(a);
				Cleanup(rig);
			}
		}

		[Test]
		public void Kartenrand_WirdAlsLinieGezeichnet()
		{
			Rig rig = NewRig();
			// Zonenrechteck 40 x 40 um den Ursprung: Die Kante liegt 20 m entfernt
			// und damit innerhalb des Kartenradius von 22 m. Die Quelle ist die
			// Zonengeometrie (SetMapEdge), nicht die Bewegungsgrenze — die ist in
			// allen echten Zonen Unbounded.
			rig.Presenter.SetMapEdge(PlayerMovementBoundary.Rectangle(Vector3.zero, new Vector2(40f, 40f)));
			try
			{
				rig.Presenter.Refresh();

				Image[] linien = ActiveEdges(rig);
				Assert.That(linien.Length, Is.GreaterThan(0), "Der Kartenrand muss sichtbar sein, wenn er im Ausschnitt liegt");
				Assert.That(linien[0].sprite, Is.SameAs(rig.LineSymbol), "Der Rand wird als Linie gezeichnet, nicht als Punkt");
			}
			finally
			{
				Cleanup(rig);
			}
		}

		[Test]
		public void OhneHarteGrenze_WirdKeinKartenrandGezeichnet()
		{
			Rig rig = NewRig();
			rig.Presenter.SetMapEdge(PlayerMovementBoundary.Unbounded());
			try
			{
				rig.Presenter.Refresh();

				Assert.That(ActiveEdges(rig).Length, Is.EqualTo(0), "Ohne Kartenrand gibt es kein Ende der Karte zu zeigen");
			}
			finally
			{
				Cleanup(rig);
			}
		}

		[Test]
		public void MittenInDerZone_BleibtDerKartenrandUnsichtbar()
		{
			Rig rig = NewRig();
			// 200 x 200: Jede Kante ist 100 m entfernt, weit jenseits der 22 m.
			rig.Presenter.SetMapEdge(PlayerMovementBoundary.Rectangle(Vector3.zero, new Vector2(200f, 200f)));
			try
			{
				rig.Presenter.Refresh();

				Assert.That(ActiveEdges(rig).Length, Is.EqualTo(0), "Weit von der Grenze darf keine Linie im Bild liegen");
			}
			finally
			{
				Cleanup(rig);
			}
		}

		private static void Cleanup(Rig rig)
		{
			Object.DestroyImmediate(rig.Root);
			Object.DestroyImmediate(rig.PlayerObject);
			Object.DestroyImmediate(rig.CameraObject);
		}

		private static Image[] ActiveMarkers(Rig rig)
		{
			return rig.MarkerRoot.GetComponentsInChildren<Image>(includeInactive: false);
		}

		private static Image[] ActiveEdges(Rig rig)
		{
			return rig.EdgeRoot.GetComponentsInChildren<Image>(includeInactive: false);
		}

		private static Rig NewRig()
		{
			GameObject root = new GameObject("MinimapTest_Root", typeof(RectTransform));
			GameObject field = NewRect(root, "Field");
			GameObject markerRoot = NewRect(field, "Marker");
			GameObject edgeRoot = NewRect(field, "Rand");
			GameObject player = new GameObject("MinimapTest_Player", typeof(CharacterController));
			GameObject cameraObject = new GameObject("MinimapTest_Camera");

			Camera view = cameraObject.AddComponent<Camera>();
			view.orthographic = true;
			view.orthographicSize = 12f;
			// Dicht am Ursprung, damit ein Ziel bei z = -8 wirklich hinter der
			// Kamera liegt und nicht bloss weit vor ihr.
			cameraObject.transform.position = new Vector3(0f, 0f, -2f);
			cameraObject.transform.rotation = Quaternion.identity;

			GameObject markerPrefab = NewRect(root, "MarkerVorlage");
			Image prefabImage = markerPrefab.AddComponent<Image>();
			markerPrefab.SetActive(value: false);

			PlayerMotor motor = player.AddComponent<PlayerMotor>();
			motor.ConfigureBoundary(PlayerMovementBoundary.Unbounded());

			Sprite dotSymbol = NewSprite();
			Sprite lineSymbol = NewSprite();
			Sprite bossSymbol = NewSprite();
			Sprite chestSymbol = NewSprite();

			MinimapPresenter presenter = root.AddComponent<MinimapPresenter>();
			presenter.ConfigureReferences((RectTransform)field.transform, (RectTransform)markerRoot.transform, (RectTransform)edgeRoot.transform, prefabImage, dotSymbol, bossSymbol, chestSymbol, lineSymbol);
			MinimapStyle style = MinimapStyle.Default;
			presenter.ConfigureStyle(style, new MinimapResourceColor[2]
			{
				new MinimapResourceColor { paletteId = "resource.tree", color = new Color(0.4f, 0.7f, 0.35f, 1f) },
				new MinimapResourceColor { paletteId = "resource.copper_vein", color = new Color(0.85f, 0.5f, 0.25f, 1f) }
			});
			presenter.ConfigureRange(Radius, 45f, 110f);
			presenter.Bind(motor, view);

			return new Rig
			{
				Root = root,
				Presenter = presenter,
				MarkerRoot = (RectTransform)markerRoot.transform,
				EdgeRoot = (RectTransform)edgeRoot.transform,
				Motor = motor,
				PlayerObject = player,
				CameraObject = cameraObject,
				BossSymbol = bossSymbol,
				ChestSymbol = chestSymbol,
				LineSymbol = lineSymbol,
				Style = style
			};
		}

		private static GameObject NewRect(GameObject parent, string childName)
		{
			GameObject child = new GameObject(childName, typeof(RectTransform));
			child.transform.SetParent(parent.transform, worldPositionStays: false);
			return child;
		}

		private static Sprite NewSprite()
		{
			Texture2D texture = new Texture2D(4, 4);
			return Sprite.Create(texture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f));
		}
	}
}
