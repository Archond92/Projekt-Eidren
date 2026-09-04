using System.IO;
using System.Linq;
using Eidren.Editor;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace Eidren.Tests.EditMode
{
	public sealed class CombatHudResponsiveBaselineTests
	{
		private static readonly string[] Actions =
		{
			"AttackAction", "DodgeAction", "WeaponSwitch", "EidraSwitch",
			"EidraSkill1", "EidraSkill2", "PotionAction", "FoodAction", "InteractionAction", "InventoryEdgeAction"
		};

		[TestCase(1920, 1080, 0, 0, 1920, 1080)]
		[TestCase(2340, 1080, 100, 24, 2156, 1032)]
		[TestCase(2400, 1080, 120, 24, 2184, 1032)]
		[TestCase(1920, 1200, 0, 0, 1920, 1200)]
		[TestCase(1280, 720, 48, 16, 1200, 688)]
		public void AuthoredActions_StayInsideSimulatedSafeAreaWithoutOverlap(int width, int height, int x, int y, int safeWidth, int safeHeight)
		{
			var snapshot = CombatHudBaselineAudit.Read(width, height, new Rect(x, y, safeWidth, safeHeight));
			Assert.That(snapshot.scaleMode, Is.EqualTo((int)CanvasScaler.ScaleMode.ScaleWithScreenSize));
			Assert.That(snapshot.referenceResolution, Is.EqualTo(new Vector2(1920, 1080)));
			Assert.That(snapshot.matchWidthOrHeight, Is.EqualTo(1f));
			var controls = Actions.Select(name => snapshot.controls.Single(control => control.name == name)).ToArray();
			for (int i = 0; i < controls.Length; i++)
			{
				Rect rect = controls[i].boundsInSafeArea;
				Rect safe = snapshot.safeAreaLocal;
				Assert.That(rect.width, Is.GreaterThan(0), controls[i].path);
				Assert.That(rect.height, Is.GreaterThan(0), controls[i].path);
				Assert.That(rect.xMin, Is.GreaterThanOrEqualTo(safe.xMin - .01f), controls[i].path);
				Assert.That(rect.yMin, Is.GreaterThanOrEqualTo(safe.yMin - .01f), controls[i].path);
				Assert.That(rect.xMax, Is.LessThanOrEqualTo(safe.xMax + .01f), controls[i].path);
				Assert.That(rect.yMax, Is.LessThanOrEqualTo(safe.yMax + .01f), controls[i].path);
				for (int j = i + 1; j < controls.Length; j++)
					Assert.That(rect.Overlaps(controls[j].boundsInSafeArea), Is.False, controls[i].path + " / " + controls[j].path);
			}
		}

		[Test]
		public void Audit_DoesNotWritePrefabAndRecordsRequiredBindings()
		{
			byte[] before = File.ReadAllBytes(CombatHudBaselineAudit.PrefabPath);
			var snapshot = CombatHudBaselineAudit.Read(1920, 1080, new Rect(0, 0, 1920, 1080));
			Assert.That(File.ReadAllBytes(CombatHudBaselineAudit.PrefabPath), Is.EqualTo(before));
			Assert.That(snapshot.bindings.Where(binding => binding.missing), Is.Empty);
			foreach (string field in new[] { "canvas", "safeArea", "status", "actions", "interactionView", "inputView", "result", "minimap" })
				Assert.That(snapshot.bindings.Single(binding => binding.owner.EndsWith(":CombatHUD") && binding.field == field).target, Is.Not.Empty, field);
			Assert.That(snapshot.hierarchy.Any(node => node.path.EndsWith("/ActionCluster")), Is.True);
		}
	}
}
