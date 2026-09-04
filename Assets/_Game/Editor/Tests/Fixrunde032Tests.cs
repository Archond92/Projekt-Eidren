using Eidren.AI;
using Eidren.Combat;
using Eidren.Data;
using Eidren.Eidra;
using Eidren.Interaction;
using NUnit.Framework;
using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.Tests.EditMode
{
	/// <summary>
	/// Fixrunde v0.3.2: Zielauswahl am Gebäude (F32-001), Lebensmaximum mit
	/// Rüstungsbonus (F32-002) und das Rückenmal am gemeinsamen Gegnertyp
	/// (F32-003/F32-004).
	/// </summary>
	public sealed class Fixrunde032Tests
	{
		/// <summary>
		/// Ziel mit frei setzbarer Position, Reichweite und Rang. Der Körper
		/// ist ein Würfel um die Position; ohne Halbmaß verhält sich das Ziel
		/// wie ein Punkt.
		/// </summary>
		private sealed class Zielattrappe : IInteractable
		{
			public string InteractionId { get; }

			public InteractionType Type => InteractionType.WorldObject;

			public string DisplayText => "TEST";

			public Sprite Icon => null;

			public float InteractionRange { get; }

			public Eidren.Interaction.InteractionMode Mode => Eidren.Interaction.InteractionMode.Instant;

			public float HoldDuration => 0f;

			public int Priority { get; }

			public Vector3 InteractionPosition => InteractionObject.transform.position;

			public GameObject InteractionObject { get; }

			public Zielattrappe(string id, Vector3 position, float range, int priority, float halbmass = 0f)
			{
				InteractionId = id;
				InteractionRange = range;
				Priority = priority;
				InteractionObject = new GameObject("F32_" + id);
				InteractionObject.transform.position = position;
				if (halbmass > 0f)
				{
					BoxCollider körper = InteractionObject.AddComponent<BoxCollider>();
					körper.size = new Vector3(halbmass * 2f, 2f, halbmass * 2f);
				}
			}

			public bool CanInteract(in InteractionContext context, out string blockedReason)
			{
				blockedReason = string.Empty;
				return true;
			}

			public void BeginInteraction(in InteractionContext context)
			{
			}

			public void UpdateInteraction(in InteractionContext context, float normalizedProgress)
			{
			}

			public void CancelInteraction(in InteractionContext context, InteractionCancelReason reason)
			{
			}

			public void CompleteInteraction(in InteractionContext context)
			{
			}

			public void Zerstoeren()
			{
				Object.DestroyImmediate(InteractionObject);
			}
		}

		// ---------------------------------------------------------------
		// F32-001 — Zielauswahl am Gebäude
		// ---------------------------------------------------------------

		/// <summary>
		/// Die Figur berührt die Werkbank (0,98 m zur Zellmitte) und ist um
		/// 45° zur Lagerkiste eine Zelle weiter gedreht. Zuvor stand die
		/// Kiste als Ziel fest. Wer an einem Gebäude steht, muss dieses
		/// Gebäude bekommen.
		/// </summary>
		[Test]
		public void BeruehrtesGebaeude_SchlaegtDenNachbarn()
		{
			GameObject figur = new GameObject("F32001_Figur");
			Zielattrappe werkbank = new Zielattrappe("werkbank", new Vector3(0f, 0f, 0.98f), 2.4f, 80, 0.5f);
			Zielattrappe kiste = new Zielattrappe("kiste", new Vector3(1f, 0f, 0.98f), 2.5f, 85, 0.5f);
			try
			{
				figur.transform.position = Vector3.zero;
				InteractionContext blickfeld = new InteractionContext(figur, figur.transform, new Vector3(0.7071f, 0f, 0.7071f));
				IInteractable gewaehlt = InteractionTargetSelector.SelectBest(
					new IInteractable[2] { werkbank, kiste }, 2, in blickfeld, kiste, 0.12f, 0.3f, out var _, out var _);
				Assert.That(gewaehlt, Is.SameAs(werkbank),
					"Das berührte Gebäude muss den Nachbarn eine Zelle weiter schlagen.");
			}
			finally
			{
				werkbank.Zerstoeren();
				kiste.Zerstoeren();
				Object.DestroyImmediate(figur);
			}
		}

		/// <summary>
		/// OP-02: Alle Ziele verwenden denselben Abstand ab Koerperoberflaeche.
		/// 1,5 m bewahrt die bestaetigte Reichweite von 2,0 m ab Mittelpunkt
		/// fuer eine 1x1-Zelle und 2,5 m fuer den 2x2-Acker.
		/// </summary>
		[Test]
		public void Gebaeudereichweiten_SindAbOberflaecheEinheitlich()
		{
			GameObject werkbank = new GameObject("F32001_Werkbank");
			GameObject kiste = new GameObject("F32001_Kiste");
			GameObject acker = new GameObject("F32001_Acker");
			try
			{
				Assert.That(werkbank.AddComponent<WorkbenchController>().InteractionRange,
					Is.EqualTo(InteractionUtility.StandardSurfaceRange), "Werkbank und Fertigungsstationen");
				Assert.That(kiste.AddComponent<StorageContainer>().InteractionRange,
					Is.EqualTo(InteractionUtility.StandardSurfaceRange), "Lagerkiste");
				Assert.That(acker.AddComponent<FarmPlotController>().InteractionRange,
					Is.EqualTo(InteractionUtility.StandardSurfaceRange), "Acker (2x2)");
			}
			finally
			{
				Object.DestroyImmediate(werkbank);
				Object.DestroyImmediate(kiste);
				Object.DestroyImmediate(acker);
			}
		}

		[Test]
		public void Koerperdistanz_MisstBisZurCollideroberflaeche()
		{
			Zielattrappe ziel = new Zielattrappe("koerperdistanz", new Vector3(0f, 0f, 2f),
				InteractionUtility.StandardSurfaceRange, 80, 0.5f);
			try
			{
				Assert.That(InteractionUtility.FlatDistanceToBody(Vector3.zero, ziel), Is.EqualTo(1.5f).Within(0.001f));
			}
			finally
			{
				ziel.Zerstoeren();
			}
		}

		[Test]
		public void MehrAlsEineinhalbMeterVorDerWand_IstAusserReichweite()
		{
			GameObject figur = new GameObject("F32001_Figur_AusserReichweite");
			Zielattrappe ziel = new Zielattrappe("zu_weit", new Vector3(0f, 0f, 2.01f),
				InteractionUtility.StandardSurfaceRange, 80, 0.5f);
			try
			{
				InteractionContext context = new InteractionContext(figur, figur.transform, Vector3.forward);
				IInteractable gewaehlt = InteractionTargetSelector.SelectBest(
					new IInteractable[1] { ziel }, 1, in context, null, 0.12f, 0.2f, out var _, out var _);
				Assert.That(gewaehlt, Is.Null);
			}
			finally
			{
				ziel.Zerstoeren();
				Object.DestroyImmediate(figur);
			}
		}

		// ---------------------------------------------------------------
		// F32-002 — Lebensmaximum mit Rüstungsbonus
		// ---------------------------------------------------------------

		/// <summary>
		/// Volle Stoffrüstung hebt das Maximum auf 110. Beim Betreten einer
		/// Zone ruft der Spawn <c>Initialize</c> erneut auf — dabei darf der
		/// Bonus nicht verfallen, und aufgefüllt wird auf das echte Maximum.
		/// </summary>
		[Test]
		public void Zonenankunft_FuelltAufDasMaximumMitRuestungsbonus()
		{
			GameObject traeger = new GameObject("F32002_Leben");
			try
			{
				Damageable leben = traeger.AddComponent<Damageable>();
				leben.Initialize(100f);
				leben.SetMaxHealthBonus(10f);
				Assert.That(leben.MaxHealth, Is.EqualTo(110f), "Die Rüstung hebt das Maximum.");
				Assert.That(leben.CurrentHealth, Is.EqualTo(100f), "Anlegen heilt nicht (F31-018).");

				leben.Initialize(100f);

				Assert.That(leben.MaxHealth, Is.EqualTo(110f),
					"Der Rüstungsbonus darf beim Spawn nicht verfallen.");
				Assert.That(leben.CurrentHealth, Is.EqualTo(110f),
					"Die Ankunft füllt auf das echte Maximum, nicht auf den Grundwert.");
			}
			finally
			{
				Object.DestroyImmediate(traeger);
			}
		}

		// ---------------------------------------------------------------
		// F32-003 / F32-004 — Rückenmal am gemeinsamen Gegnertyp
		// ---------------------------------------------------------------

		// ---------------------------------------------------------------
		// F32-005 — Passivwerte der Eidra
		// ---------------------------------------------------------------

		/// <summary>
		/// Das HUD kündigt für jedes Eidra einen Passiveffekt an. Ignivar
		/// stand auf 1,0/1,0 und hatte damit gar keinen — die Ansage war eine
		/// leere Zusage.
		/// </summary>
		[Test]
		public void JedesEidra_TraegtEinenWirksamenPassivwert()
		{
			foreach (string name in new string[3] { "Terrock", "Noctarion", "Ignivar" })
			{
				EidraData eidra = AssetDatabase.LoadAssetAtPath<EidraData>("Assets/_Game/Data/Eidren/" + name + ".asset");
				Assert.That(eidra, Is.Not.Null, "Daten nicht gefunden: " + name);
				bool wirksam = eidra.PassiveStaggerMultiplier != 1f
					|| eidra.PassiveBackDamageMultiplier != 1f
					|| (eidra.PassiveBurnFraction > 0f && eidra.PassiveBurnSeconds > 0);
				Assert.That(wirksam, Is.True,
					name + " hat keinen Passiveffekt — das HUD kündigt aber einen an.");
			}
		}

		/// <summary>
		/// Ignivars Passiv: Ein Treffer entzündet das Ziel, der Nachbrand
		/// verteilt sich über mehrere Sekundentakte und ergibt in Summe
		/// genau den mitgegebenen Schaden.
		/// </summary>
		[Test]
		public void Nachbrand_VerteiltDenGesamtschadenAufDieTakte()
		{
			PassiveBurnState brand = new PassiveBurnState();
			Assert.That(brand.TryTick(1f, out var _), Is.False, "Ohne Treffer brennt nichts.");

			brand.Refresh(8f, 4);
			Assert.That(brand.IsBurning, Is.True);

			float summe = 0f;
			int takte = 0;
			for (int i = 0; i < 10 && brand.IsBurning; i++)
			{
				if (brand.TryTick(1f, out var teil))
				{
					summe += teil;
					takte++;
				}
			}
			Assert.That(takte, Is.EqualTo(4), "Vier Sekunden ergeben vier Takte.");
			Assert.That(summe, Is.EqualTo(8f).Within(0.001f), "In Summe genau der Gesamtschaden.");
			Assert.That(brand.IsBurning, Is.False, "Danach ist der Brand aus.");
		}

		/// <summary>
		/// Schnelle Waffen dürfen den Brand nicht aufsummieren — ein neuer
		/// Treffer ersetzt den laufenden.
		/// </summary>
		[Test]
		public void Nachbrand_StapeltNichtSondernFrischtAuf()
		{
			PassiveBurnState brand = new PassiveBurnState();
			brand.Refresh(8f, 4);
			brand.TryTick(1f, out var _);

			brand.Refresh(4f, 4);

			float summe = 0f;
			for (int i = 0; i < 10 && brand.IsBurning; i++)
			{
				if (brand.TryTick(1f, out var teil))
				{
					summe += teil;
				}
			}
			Assert.That(summe, Is.EqualTo(4f).Within(0.001f),
				"Der neue Treffer ersetzt den Rest des alten, er addiert sich nicht dazu.");
		}

		/// <summary>
		/// Der Passivtext kam aus der ROLLE statt aus den Daten. Dadurch
		/// versprach jedes Angriffs-Eidra „+18 % RÜCKENSCHADEN", auch wenn
		/// seine Zahlen etwas anderes sagten.
		/// </summary>
		[Test]
		public void Passivtext_VersprichtNurWasDieDatenHalten()
		{
			EidraData ohneBonus = ScriptableObject.CreateInstance<EidraData>();
			EidraData mitBonus = ScriptableObject.CreateInstance<EidraData>();
			try
			{
				ohneBonus.Role = EidraRole.Attack;
				ohneBonus.PassiveBackDamageMultiplier = 1f;
				Assert.That(EidraPassiveText.For(ohneBonus), Does.Not.Contain("RÜCKENSCHADEN"),
					"Ohne Bonus in den Daten darf der Text keinen versprechen.");

				mitBonus.Role = EidraRole.Attack;
				mitBonus.PassiveBackDamageMultiplier = 1.25f;
				Assert.That(EidraPassiveText.For(mitBonus), Does.Contain("25"),
					"Der Text muss die Zahl aus den Daten nennen, nicht eine feste.");
			}
			finally
			{
				Object.DestroyImmediate(ohneBonus);
				Object.DestroyImmediate(mitBonus);
			}
		}

		/// <summary>
		/// Das Rückenmal lag am <c>BossController</c> und war damit auf die
		/// zwei Bosse beschränkt. Es gehört an den gemeinsamen Gegnertyp,
		/// sonst kann Noctarions zweite Fähigkeit gegen Feldgegner nichts
		/// bewirken.
		/// </summary>
		[Test]
		public void Rueckenmal_LiegtAmGemeinsamenGegnertyp()
		{
			Assert.That(typeof(EnemyControllerBase).GetMethod("MarkBack", new Type[2] { typeof(float), typeof(float) }),
				Is.Not.Null, "Jeder Gegner muss markierbar sein, nicht nur der Boss.");
			Assert.That(typeof(EnemyControllerBase).GetMethod("ClearBackMark", Type.EmptyTypes),
				Is.Not.Null, "Das Löschen der Markierung gehört an dieselbe Stelle.");
		}
	}
}
