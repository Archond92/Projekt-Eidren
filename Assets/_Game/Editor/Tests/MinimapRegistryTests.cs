using Eidren.Core.Services;
using NUnit.Framework;
using UnityEngine;

namespace Eidren.Tests.EditMode
{
	public sealed class MinimapRegistryTests
	{
		private sealed class TestMarker : IMinimapMarker
		{
			public MinimapMarkerKind MinimapKind { get; set; }

			public Vector3 MinimapPosition { get; set; }

			public bool ShowsOnMinimap { get; set; } = true;

			public string MinimapPaletteId { get; set; } = string.Empty;
		}

		[Test]
		public void AngemeldeterMarker_StehtInDerListe()
		{
			TestMarker marker = new TestMarker { MinimapKind = MinimapMarkerKind.Resource };
			try
			{
				MinimapRegistry.Register(marker);

				Assert.That(MinimapRegistry.Markers, Contains.Item(marker), "Ein angemeldeter Marker muss abrufbar sein");
			}
			finally
			{
				MinimapRegistry.Unregister(marker);
			}
		}

		[Test]
		public void ZweifacheAnmeldung_ErzeugtKeinenZweitenEintrag()
		{
			TestMarker marker = new TestMarker();
			try
			{
				MinimapRegistry.Register(marker);
				MinimapRegistry.Register(marker);

				int treffer = 0;
				foreach (IMinimapMarker eintrag in MinimapRegistry.Markers)
				{
					if (eintrag == marker)
					{
						treffer++;
					}
				}
				Assert.That(treffer, Is.EqualTo(1), "Doppelte Anmeldung darf den Marker nicht zweimal zeichnen lassen");
			}
			finally
			{
				MinimapRegistry.Unregister(marker);
			}
		}

		[Test]
		public void AbgemeldeterMarker_VerschwindetAusDerListe()
		{
			TestMarker marker = new TestMarker();
			MinimapRegistry.Register(marker);

			MinimapRegistry.Unregister(marker);

			Assert.That(MinimapRegistry.Markers, Has.No.Member(marker), "Ein zerstoerter Gegner darf nicht auf der Karte stehenbleiben");
		}

		[Test]
		public void Nullanmeldung_WirdStillIgnoriert()
		{
			int vorher = MinimapRegistry.Markers.Count;

			MinimapRegistry.Register(null);

			Assert.That(MinimapRegistry.Markers.Count, Is.EqualTo(vorher), "Null darf die Liste nicht verlaengern");
		}
	}
}
