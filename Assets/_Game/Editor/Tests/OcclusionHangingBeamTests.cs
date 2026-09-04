using Eidren.Presentation;
using NUnit.Framework;
using UnityEngine;

namespace Eidren.Tests.EditMode
{
	/// <summary>
	/// Nach-Release-Fix (18.08.2026): Im Verlies verdecken BALKEN und
	/// Galerien die Figur, ohne zu faden — die Hoehenregel (Bounds-Hoehe
	/// >= 1,35) sortiert flache, hoch haengende Objekte aus. Neu zaehlt
	/// auch, wessen Unterkante ueber Kopfhoehe des Akteurs liegt.
	/// </summary>
	public sealed class OcclusionHangingBeamTests
	{
		[Test]
		public void HaengenderBalken_ZaehltAlsVerdecker()
		{
			// Balken: 0,4 hoch, Unterkante auf 3,2 m — ueber dem Kopf (1,6).
			Bounds balken = new Bounds(new Vector3(0f, 3.4f, 0f), new Vector3(4f, 0.4f, 0.6f));
			Assert.That(ActorOcclusionTransparency.IsOccluderFor(balken, 1.6f), Is.True,
				"Ein Balken ueber Kopfhoehe verdeckt von oben und muss faden.");
		}

		[Test]
		public void Bodenkiste_ZaehltWeiterhinNicht()
		{
			Bounds kiste = new Bounds(new Vector3(0f, 0.5f, 0f), new Vector3(1f, 1f, 1f));
			Assert.That(ActorOcclusionTransparency.IsOccluderFor(kiste, 1.6f), Is.False,
				"Niedrige Bodenobjekte bleiben vom Fade ausgenommen.");
		}

		[Test]
		public void HoheWand_ZaehltWeiterhin()
		{
			Bounds wand = new Bounds(new Vector3(0f, 2.75f, 0f), new Vector3(4f, 5.5f, 0.4f));
			Assert.That(ActorOcclusionTransparency.IsOccluderFor(wand, 1.6f), Is.True);
		}
	}
}
