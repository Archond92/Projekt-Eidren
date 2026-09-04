using Eidren.Core.Services;
using Eidren.Data;
using NUnit.Framework;
using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Regeln des Formwalls im Einbruch (SCHMIEDE_ENTWURF.md Abschnitt 6).
/// Reine Spiellogik, keine Szene.
/// </summary>
public sealed class EidraForgeFormwallTests
{
	[Test]
	public void Bauplatz_IstGesperrtSolangeDasNestLebt()
	{
		EidraForgeDungeonService dienst = LaufenderDienst();
		Assert.That(dienst.IstBauplatzFrei(), Is.False,
			"Solange die vier AshRunner leben, darf nicht gebaut werden.");
		Assert.That(dienst.TryBaueFormwall(VollerBeutel(), out _), Is.False);
		Assert.That(dienst.IstFormwallGebaut, Is.False);
	}

	[Test]
	public void Formwall_BrauchtDasVolleMaterial()
	{
		EidraForgeDungeonService dienst = NestGeraeumt();
		PlayerInventory knapp = new PlayerInventory(); knapp.Configure(_inhalt);
		knapp.Add("stone_block", 24);
		knapp.Add("plank", 16);
		knapp.Add("copper_bar", 10);
		knapp.Add("smithing_fitting", 5);
		Assert.That(dienst.TryBaueFormwall(knapp, out _), Is.False, "Fuenf Beschlaege reichen nicht.");
		Assert.That(dienst.IstFormwallGebaut, Is.False);
	}

	[Test]
	public void Formwall_WirdGebautUndZiehtDasMaterialAb()
	{
		EidraForgeDungeonService dienst = NestGeraeumt();
		PlayerInventory beutel = VollerBeutel();
		Assert.That(dienst.TryBaueFormwall(beutel, out _), Is.True);
		Assert.That(dienst.IstFormwallGebaut, Is.True);
		Assert.That(beutel.GetTotalAmount("smithing_fitting"), Is.EqualTo(2),
			"Von acht Beschlaegen muessen sechs abgezogen sein.");
	}

	[Test]
	public void Formwall_LaesstSichNichtZweimalBauen()
	{
		EidraForgeDungeonService dienst = NestGeraeumt();
		Assert.That(dienst.TryBaueFormwall(VollerBeutel(), out _), Is.True);
		Assert.That(dienst.TryBaueFormwall(VollerBeutel(), out _), Is.False);
	}

	[Test]
	public void ImBauLauf_GibtEsNochNichtsAbzuholen()
	{
		EidraForgeDungeonService dienst = NestGeraeumt();
		Assert.That(dienst.TryBaueFormwall(VollerBeutel(), out _), Is.True);
		Assert.That(dienst.IstFormwallErtragOffen, Is.False,
			"Sonst kaemen die investierten Beschlaege sofort zurueck.");
		Assert.That(dienst.TryErnteFormwall(LeererBeutel()), Is.False);
	}

	[Test]
	public void Ertrag_KommtNurEinmalProLauf()
	{
		EidraForgeDungeonService dienst = MitFormwallImNeuenLauf();
		PlayerInventory beutel = new PlayerInventory(); beutel.Configure(_inhalt);
		Assert.That(dienst.TryErnteFormwall(beutel), Is.True);
		Assert.That(beutel.GetTotalAmount("smithing_fitting"), Is.EqualTo(2));
		Assert.That(dienst.TryErnteFormwall(beutel), Is.False, "Nur einmal pro Lauf.");
		Assert.That(beutel.GetTotalAmount("smithing_fitting"), Is.EqualTo(2));
	}

	/// <summary>
	/// Der Ausbau ueberlebt den bezahlten Reset, der Ertrag steht danach wieder
	/// bereit - aber er sammelt sich nicht an: nach zwei ausgelassenen Laeufen
	/// gibt es trotzdem nur zwei Beschlaege.
	/// </summary>
	[Test]
	public void Ausbau_UeberlebtResets_ErtragSammeltSichNichtAn()
	{
		EidraForgeDungeonService dienst = MitFormwallImNeuenLauf();
		for (int lauf = 0; lauf < 2; lauf++)
		{
			Assert.That(dienst.TryComplete(DateTime.UtcNow, out _), Is.True);
			NeuenLaufErzwingen(dienst, 10 + lauf);
			Assert.That(dienst.IstFormwallGebaut, Is.True, "Der Ausbau muss den Reset ueberleben.");
		}
		PlayerInventory beutel = new PlayerInventory(); beutel.Configure(_inhalt);
		Assert.That(dienst.TryErnteFormwall(beutel), Is.True);
		Assert.That(beutel.GetTotalAmount("smithing_fitting"), Is.EqualTo(2),
			"Verpasste Laeufe duerfen sich nicht aufsummieren.");
	}

	[Test]
	public void Formwall_UeberstehtSpeichernUndLaden()
	{
		EidraForgeDungeonService dienst = NestGeraeumt();
		Assert.That(dienst.TryBaueFormwall(VollerBeutel(), out _), Is.True);

		SaveEidraForgeData gespeichert = SaveEidraForgeMapper.ToSave(Zustand(dienst));
		EidraForgeRunState geladen = SaveEidraForgeMapper.ToRuntime(gespeichert, _inhalt, null);

		EidraForgeDungeonService neu = new EidraForgeDungeonService();
		Wiederherstellen(neu, geladen);
		Assert.That(neu.IstFormwallGebaut, Is.True, "Der Ausbau muss den Programmstart ueberleben.");
		Assert.That(neu.IstFormwallErtragOffen, Is.False, "Der Erntestand muss mitgespeichert werden.");
	}

	/// <summary>
	/// Ein Spielstand aus der Zeit vor dieser Etappe kennt die beiden Felder
	/// nicht. Er muss laden, als waere der Formwall nicht gebaut - der
	/// Standardwert false ist inhaltlich richtig, aber das gehoert belegt und
	/// nicht angenommen.
	/// </summary>
	[Test]
	public void Altstand_OhneFormwallfelder_LaedtAlsNichtGebaut()
	{
		SaveEidraForgeData alt = new SaveEidraForgeData
		{
			status = (int)EidraForgeRunStatus.InProgress,
			runId = "eidra_forge.alt"
		};
		EidraForgeRunState geladen = SaveEidraForgeMapper.ToRuntime(alt, _inhalt, null);
		EidraForgeDungeonService dienst = new EidraForgeDungeonService();
		Wiederherstellen(dienst, geladen);
		Assert.That(dienst.IstFormwallGebaut, Is.False);
		Assert.That(dienst.IstFormwallErtragOffen, Is.False);
	}

	/* Capture und Restore sind intern; der Weg ueber SaveGameMapper ist der
	   gleiche, den das Spiel geht. Fuer den Test genuegt die Spiegelung ueber
	   Reflection - so bleibt die oeffentliche Schnittstelle unveraendert. */
	private static EidraForgeRunState Zustand(EidraForgeDungeonService dienst)
	{
		return (EidraForgeRunState)typeof(EidraForgeDungeonService)
			.GetMethod("Capture", BindingFlags.Instance | BindingFlags.NonPublic)
			.Invoke(dienst, null);
	}

	private static void Wiederherstellen(EidraForgeDungeonService dienst, EidraForgeRunState zustand)
	{
		typeof(EidraForgeDungeonService)
			.GetMethod("Restore", BindingFlags.Instance | BindingFlags.NonPublic)
			.Invoke(dienst, new object[] { zustand });
	}

	private static EidraForgeDungeonService LaufenderDienst()
	{
		EidraForgeDungeonService dienst = new EidraForgeDungeonService();
		Assert.That(dienst.TryBeginFirstRun(1), Is.True);
		return dienst;
	}

	/* TryRecordDefeat legt Beute ab und braucht dafuer eine ContentDatabase.
	   Aufbau wie in EidraForgeDungeonTests.ContentFixture - kein eigener Weg,
	   sondern der vorhandene. */
	private static ContentDatabase _inhalt;

	private static GameObject _inhaltWurzel;

	[OneTimeSetUp]
	public void InhaltAufbauen()
	{
		_inhaltWurzel = new GameObject("FormwallInhalt");
		ContentDatabase datenbank = _inhaltWurzel.AddComponent<ContentDatabase>();
		ItemDefinition[] gegenstaende = AssetDatabase
			.FindAssets("t:ItemDefinition", new[] { "Assets/_Game/Data/Items" })
			.Select(AssetDatabase.GUIDToAssetPath)
			.Select(AssetDatabase.LoadAssetAtPath<ItemDefinition>)
			.Where(wert => wert != null)
			.ToArray();
		datenbank.ConfigureItems(gegenstaende);
		_inhalt = datenbank;
	}

	[OneTimeTearDown]
	public void InhaltAbbauen()
	{
		if (_inhaltWurzel != null)
		{
			UnityEngine.Object.DestroyImmediate(_inhaltWurzel);
		}
	}

	private static EidraForgeDungeonService NestGeraeumt()
	{
		EidraForgeDungeonService dienst = LaufenderDienst();
		EidraForgeEnemyService gegner = new EidraForgeEnemyService(dienst);
		foreach (ForgeEnemyState zustand in gegner.GetAll())
		{
			if (zustand.SpawnId.Contains("ash_runner.0") && zustand.SpawnId[zustand.SpawnId.Length - 1] >= '3')
			{
				gegner.TryRecordDefeat(zustand.SpawnId, Vector3.zero, _inhalt, out _);
			}
		}
		Assert.That(dienst.IstBauplatzFrei(), Is.True, "Das Nest sollte geraeumt sein.");
		return dienst;
	}

	private static EidraForgeDungeonService MitFormwallImNeuenLauf()
	{
		EidraForgeDungeonService dienst = NestGeraeumt();
		Assert.That(dienst.TryBaueFormwall(VollerBeutel(), out _), Is.True);
		Assert.That(dienst.TryComplete(DateTime.UtcNow, out _), Is.True);
		NeuenLaufErzwingen(dienst, 7);
		return dienst;
	}

	private static void NeuenLaufErzwingen(EidraForgeDungeonService dienst, int saat)
	{
		/* Der bezahlte Reset verlangt abgelaufene Abklingzeit. Statt sie zu
		   verstellen, wird ein spaeterer Zeitpunkt uebergeben. */
		DateTime spaeter = DateTime.UtcNow.AddHours(EidraForgeDungeonService.CooldownHours + 1);
		Assert.That(dienst.TryPaidReset(ResetBeutel(), spaeter, saat, playerInside: false,
			Array.Empty<ItemStack>(), out _), Is.True);
	}

	private static PlayerInventory LeererBeutel()
	{
		PlayerInventory beutel = new PlayerInventory();
		beutel.Configure(_inhalt);
		return beutel;
	}

	private static PlayerInventory VollerBeutel()
	{
		PlayerInventory beutel = new PlayerInventory(); beutel.Configure(_inhalt);
		beutel.Add("stone_block", 40);
		beutel.Add("plank", 40);
		beutel.Add("copper_bar", 40);
		beutel.Add("smithing_fitting", 8);
		return beutel;
	}

	private static PlayerInventory ResetBeutel()
	{
		PlayerInventory beutel = new PlayerInventory(); beutel.Configure(_inhalt);
		beutel.Add("plank", 20);
		beutel.Add("stone_block", 20);
		beutel.Add("copper_bar", 20);
		return beutel;
	}
}
