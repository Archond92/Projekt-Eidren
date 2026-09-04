using Eidren.Core.Services;
using NUnit.Framework;
using UnityEngine;

namespace Eidren.Tests.EditMode
{
	public sealed class MinimapProjectionTests
	{
		private const float Yaw = 45f;

		[Test]
		public void Figurenposition_LandetInDerKartenmitte()
		{
			Vector3 spieler = new Vector3(12f, 3f, -7f);

			bool sichtbar = MinimapProjection.TryProject(spieler, spieler, 20f, 100f, Yaw, out Vector2 karte);

			Assert.That(sichtbar, Is.True, "Die Figur selbst muss immer auf der Karte liegen");
			Assert.That(karte.x, Is.EqualTo(0f).Within(0.001f), "Die Figur gehört waagerecht in die Kartenmitte");
			Assert.That(karte.y, Is.EqualTo(0f).Within(0.001f), "Die Figur gehört senkrecht in die Kartenmitte");
		}

		[Test]
		public void Bildschirmoben_EntsprichtDerWeltdiagonale()
		{
			// Die Kamera steht fest auf 52°/45° (IsometricCamera). "Oben auf dem
			// Bildschirm" ist damit die Weltrichtung (1,0,1) — nicht Welt-Nord.
			Vector3 spieler = Vector3.zero;
			Vector3 ziel = new Vector3(7.071f, 0f, 7.071f);

			MinimapProjection.TryProject(ziel, spieler, 20f, 100f, Yaw, out Vector2 karte);

			Assert.That(karte.x, Is.EqualTo(0f).Within(0.01f), "Die Bildschirmdiagonale darf nicht seitlich auswandern");
			Assert.That(karte.y, Is.GreaterThan(0f), "Ein Ziel in Blickrichtung gehört nach oben auf die Karte");
		}

		[Test]
		public void HalberWeltradius_LiegtAufHalbemKartenradius()
		{
			Vector3 spieler = Vector3.zero;
			Vector3 ziel = new Vector3(7.071f, 0f, 7.071f);

			MinimapProjection.TryProject(ziel, spieler, 20f, 100f, Yaw, out Vector2 karte);

			Assert.That(karte.magnitude, Is.EqualTo(50f).Within(0.1f), "10 von 20 Metern müssen 50 von 100 Kartenpunkten ergeben");
		}

		[Test]
		public void ZielAusserhalbDesRadius_WirdNichtGezeichnet()
		{
			Vector3 spieler = Vector3.zero;
			Vector3 ziel = new Vector3(0f, 0f, 25f);

			bool sichtbar = MinimapProjection.TryProject(ziel, spieler, 20f, 100f, Yaw, out Vector2 karte);

			Assert.That(sichtbar, Is.False, "Jenseits des Kartenradius darf nichts gezeichnet werden");
			Assert.That(karte, Is.EqualTo(Vector2.zero), "Ein verworfenes Ziel darf keine Position zurückgeben");
		}

		[Test]
		public void Hoehenunterschied_AendertDieKartenposition_Nicht()
		{
			// Die Karte ist eine Draufsicht: Wer auf einem Felsen steht, rutscht
			// darauf nicht zur Seite.
			Vector3 spieler = new Vector3(0f, 0f, 0f);
			Vector3 flach = new Vector3(5f, 0f, 0f);
			Vector3 hoch = new Vector3(5f, 6f, 0f);

			MinimapProjection.TryProject(flach, spieler, 20f, 100f, Yaw, out Vector2 karteFlach);
			MinimapProjection.TryProject(hoch, spieler, 20f, 100f, Yaw, out Vector2 karteHoch);

			Assert.That(karteHoch, Is.EqualTo(karteFlach), "Die Höhe darf die Draufsicht nicht verschieben");
		}

		[Test]
		public void Project_GibtAuchJenseitsDesRadiusEinePositionZurueck()
		{
			// Der Kartenrand ist eine Strecke, die den Ausschnitt kreuzt: Ihre
			// Endpunkte liegen fast immer ausserhalb und muessen trotzdem eine
			// Position haben, sonst laesst sich die Linie nicht zeichnen.
			Vector3 spieler = Vector3.zero;
			Vector3 weitDraussen = new Vector3(0f, 0f, 60f);

			Vector2 karte = MinimapProjection.Project(weitDraussen, spieler, 20f, 100f, Yaw);

			Assert.That(karte.magnitude, Is.EqualTo(300f).Within(0.5f), "60 von 20 Metern ergeben 300 von 100 Kartenpunkten");
		}

		[Test]
		public void AbstandZurStrecke_MisstDenLotrechtenAbstand()
		{
			Vector3 punkt = new Vector3(0f, 0f, 0f);
			Vector3 a = new Vector3(-10f, 0f, 5f);
			Vector3 b = new Vector3(10f, 0f, 5f);

			float abstand = MinimapProjection.DistanceToSegment(punkt, a, b);

			Assert.That(abstand, Is.EqualTo(5f).Within(0.01f), "Vor der Strecke zaehlt der lotrechte Abstand");
		}

		[Test]
		public void AbstandZurStrecke_MisstAmEndpunktWennDasLotDanebenFaellt()
		{
			Vector3 punkt = new Vector3(20f, 0f, 0f);
			Vector3 a = new Vector3(-10f, 0f, 0f);
			Vector3 b = new Vector3(10f, 0f, 0f);

			float abstand = MinimapProjection.DistanceToSegment(punkt, a, b);

			Assert.That(abstand, Is.EqualTo(10f).Within(0.01f), "Jenseits des Endpunkts zaehlt der Abstand zum Endpunkt");
		}
	}
}
