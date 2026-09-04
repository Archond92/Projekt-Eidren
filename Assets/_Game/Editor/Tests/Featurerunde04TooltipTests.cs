using Eidren.Data;
using Eidren.Eidra;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.Tests.EditMode
{
	/// <summary>
	/// N04-003: Der Tooltip erklärt eine Fähigkeit aus ihren eigenen Zahlen.
	/// Geschriebene Texte wären schöner formuliert, würden aber bei der
	/// nächsten Balanceänderung still falsch — genau der Fehler, den Ignivars
	/// Passivanzeige gemacht hat (F32-005).
	/// </summary>
	public sealed class Featurerunde04TooltipTests
	{
		private static AbilityData Fabrik(AbilityExecutionType art, string name)
		{
			AbilityData ability = ScriptableObject.CreateInstance<AbilityData>();
			ability.DisplayName = name;
			ability.ExecutionType = art;
			ability.Cooldown = 7f;
			ability.Range = 8f;
			return ability;
		}

		[Test]
		public void OhneFaehigkeit_KeinText()
		{
			Assert.That(EidraAbilityText.For(null), Is.Empty);
		}

		[Test]
		public void JederText_NenntNamenUndAbklingzeit()
		{
			AbilityData ability = Fabrik(AbilityExecutionType.StaggerStrike, "Felsbrecher");
			ability.StaggerAmount = 62f;
			try
			{
				string text = EidraAbilityText.For(ability);
				Assert.That(text, Does.Contain("Felsbrecher"));
				Assert.That(text, Does.Contain("7"), "Die Abklingzeit gehört in den Tooltip.");
			}
			finally
			{
				Object.DestroyImmediate(ability);
			}
		}

		[Test]
		public void Stagger_NenntDenBetragAusDenDaten()
		{
			AbilityData ability = Fabrik(AbilityExecutionType.StaggerStrike, "Felsbrecher");
			ability.StaggerAmount = 62f;
			try
			{
				Assert.That(EidraAbilityText.For(ability), Does.Contain("62"),
					"Der Staggerwert steht in den Daten und gehört in den Text.");
			}
			finally
			{
				Object.DestroyImmediate(ability);
			}
		}

		[Test]
		public void Schild_NenntDieDauerUndKeineReichweite()
		{
			AbilityData ability = Fabrik(AbilityExecutionType.Shield, "Steinhaut");
			ability.Range = 0f;
			ability.EffectDuration = 2.1f;
			try
			{
				string text = EidraAbilityText.For(ability);
				Assert.That(text, Does.Contain("2,1").Or.Contain("2.1"), "Die Wirkdauer gehört hinein.");
				Assert.That(text, Does.Not.Contain("Reichweite"),
					"Der Schild wirkt auf die eigene Figur — eine Reichweite anzugeben wäre irreführend.");
			}
			finally
			{
				Object.DestroyImmediate(ability);
			}
		}

		[Test]
		public void Rueckenmal_NenntFaktorUndDauer()
		{
			AbilityData ability = Fabrik(AbilityExecutionType.BackMark, "Rückenmal");
			ability.BackDamageMultiplier = 1.45f;
			ability.EffectDuration = 6f;
			try
			{
				string text = EidraAbilityText.For(ability);
				Assert.That(text, Does.Contain("1,45").Or.Contain("1.45"));
				Assert.That(text, Does.Contain("6"));
			}
			finally
			{
				Object.DestroyImmediate(ability);
			}
		}
	}
}
