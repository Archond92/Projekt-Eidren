using Eidren.Data;
using Eidren.Interaction;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Eidren.Tests.EditMode
{
	/// <summary>
	/// W-005: Eine abgebaute Kupferader behaelt ihre aktive Optik — nur die
	/// Interaktion endet. Andere Knoten wechseln weiterhin auf die
	/// Abgebaut-Darstellung.
	/// </summary>
	public sealed class ResourceNodeAppearanceTests
	{
		[Test]
		public void MarkAlreadyHarvested_MitErhaltensFlagBehaeltDieAktiveOptik()
		{
			RunVisualStateCase(keepAppearance: true, expectedActive: true, expectedExhausted: false);
		}

		[Test]
		public void MarkAlreadyHarvested_OhneFlagWechseltZurAbgebautenOptik()
		{
			RunVisualStateCase(keepAppearance: false, expectedActive: false, expectedExhausted: true);
		}

		[Test]
		public void CopperVeinDefinition_WechseltWiederZurAbgebautOptik()
		{
			// W-005-Ruecknahme (15.08.2026): Der Nutzer will nach dem Spieltest
			// doch den sichtbaren Wechsel zur Abgebaut-Darstellung. Der
			// Mechanismus (Flag + ApplyVisualState) bleibt fuer kuenftige Faelle
			// bestehen, ist aber an keiner Ressource mehr gesetzt.
			ResourceNodeDefinition definition = AssetDatabase.LoadAssetAtPath<ResourceNodeDefinition>("Assets/_Game/Data/Resources/CopperVein.asset");
			Assert.That(definition, Is.Not.Null);
			Assert.That(definition.KeepsAppearanceWhenExhausted, Is.False,
				"Die Kupferader soll wieder sichtbar zur Abgebaut-Optik wechseln (W-005-Ruecknahme)");
		}

		[Test]
		public void AndereRessourcenBehaltenDenOptikwechsel()
		{
			// Der Wunsch betrifft nur die Kupferader; Baum & Co. wechseln weiter.
			foreach (string path in new[]
			{
				"Assets/_Game/Data/Resources/Tree.asset",
				"Assets/_Game/Data/Resources/StoneDeposit.asset",
				"Assets/_Game/Data/Resources/FiberPlant.asset"
			})
			{
				ResourceNodeDefinition definition = AssetDatabase.LoadAssetAtPath<ResourceNodeDefinition>(path);
				Assert.That(definition, Is.Not.Null, path);
				Assert.That(definition.KeepsAppearanceWhenExhausted, Is.False, path);
			}
		}

		private static void RunVisualStateCase(bool keepAppearance, bool expectedActive, bool expectedExhausted)
		{
			GameObject holder = new GameObject("ResourceNodeAppearanceTest");
			ResourceNodeDefinition definition = ScriptableObject.CreateInstance<ResourceNodeDefinition>();
			try
			{
				SerializedObject serialized = new SerializedObject(definition);
				SerializedProperty property = serialized.FindProperty("keepsAppearanceWhenExhausted");
				Assert.That(property, Is.Not.Null, "Feld keepsAppearanceWhenExhausted fehlt");
				property.boolValue = keepAppearance;
				serialized.ApplyModifiedPropertiesWithoutUndo();

				GameObject active = new GameObject("ActiveVisual");
				active.transform.SetParent(holder.transform, worldPositionStays: false);
				GameObject exhausted = new GameObject("ExhaustedVisual");
				exhausted.transform.SetParent(holder.transform, worldPositionStays: false);
				exhausted.SetActive(false);
				SphereCollider trigger = holder.AddComponent<SphereCollider>();
				ResourceNode node = holder.AddComponent<ResourceNode>();
				node.Configure(definition, "test.appearance.1", active, exhausted, trigger, null);

				node.MarkAlreadyHarvested();

				Assert.That(node.IsExhausted, Is.True);
				Assert.That(active.activeSelf, Is.EqualTo(expectedActive), "ActiveVisual");
				Assert.That(exhausted.activeSelf, Is.EqualTo(expectedExhausted), "ExhaustedVisual");
			}
			finally
			{
				UnityEngine.Object.DestroyImmediate(holder);
				UnityEngine.Object.DestroyImmediate(definition);
			}
		}
	}
}
