using Eidren.Combat;
using Eidren.Data;
using Eidren.Input;
using Eidren.Player;
using Eidren.UI;
using NUnit.Framework;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Eidren.Tests.EditMode
{
	/// <summary>
	/// W-004: Angriffs- und Wechselbutton zeigen die Icons der tatsaechlichen
	/// Waffenfamilien. Der Wechselbutton bietet ohne verfuegbare Zweitwaffe
	/// keinen Phantom-Wechsel an.
	/// </summary>
	public sealed class CombatHudWeaponIconTests
	{
		[Test]
		public void AlternateWeapon_LiefertDieVerfuegbareZweitwaffeOderNull()
		{
			GameObject player = new GameObject("AlternateWeapon_Player");
			try
			{
				PlayerInputReader input = player.AddComponent<PlayerInputReader>();
				PlayerMotor motor = player.AddComponent<PlayerMotor>();
				MeleeWeaponHitbox hitbox = player.AddComponent<MeleeWeaponHitbox>();
				PlayerCombatController combat = player.AddComponent<PlayerCombatController>();
				GameObject weaponObject = new GameObject("WeaponDriver");
				weaponObject.transform.SetParent(player.transform, worldPositionStays: false);
				WeaponData hammer = LoadWeapon("Hammer");
				WeaponData daggers = LoadWeapon("Daggers");
				WeaponData copperHammer = LoadWeapon("CopperHammer");
				WeaponData copperSpear = LoadWeapon("CopperSpear");
				combat.Initialize(input, motor, new SceneCombatTargetQuery(), hitbox, null, hammer, daggers, weaponObject.transform);

				combat.SetEquippedWeapons(copperSpear, null, string.Empty);
				Assert.That(combat.ActiveWeapon, Is.SameAs(copperSpear));
				Assert.That(combat.AlternateWeapon, Is.Null, "Ohne Zweitwaffe darf es keine Alternative geben");

				// SetEquippedWeapons behaelt die aktive Waffe bei, wenn sie
				// verfuegbar bleibt — daher die bevorzugte Waffe explizit setzen.
				combat.SetEquippedWeapons(copperHammer, copperSpear, copperHammer.Id);
				Assert.That(combat.ActiveWeapon, Is.SameAs(copperHammer));
				Assert.That(combat.AlternateWeapon, Is.SameAs(copperSpear));

				Assert.That(combat.TrySelectWeapon("copper_spear"), Is.True);
				Assert.That(combat.AlternateWeapon, Is.SameAs(copperHammer));
			}
			finally
			{
				UnityEngine.Object.DestroyImmediate(player);
			}
		}

		[Test]
		public void RefreshWeapon_SpeerZeigtSpeerIconUndSperrtWechselOhneZweitwaffe()
		{
			GameObject root = new GameObject("WeaponIcon_Hud");
			try
			{
				CombatHudActionPresenter presenter = BuildPresenter(root, out Sprite[] icons);
				PlayerCombatController combat = BuildCombat(root, out WeaponData copperSpear, out _, out _);
				combat.SetEquippedWeapons(copperSpear, null, string.Empty);
				SetCombat(presenter, combat);

				InvokeRefreshWeapon(presenter, copperSpear);

				Assert.That(presenter.Attack.Icon.sprite, Is.SameAs(icons[9]), "Speer muss das Speer-Icon zeigen, nicht den Hammer");
				Assert.That(LockMark(presenter.WeaponSwitch).activeSelf, Is.True, "Ohne Zweitwaffe muss der Wechselbutton gesperrt sein");
				Assert.That(presenter.WeaponSwitch.Icon.enabled, Is.False, "Ohne Zweitwaffe darf kein Phantom-Icon erscheinen");
			}
			finally
			{
				UnityEngine.Object.DestroyImmediate(root);
			}
		}

		[Test]
		public void RefreshWeapon_ZweitwaffeErscheintAufDemWechselbutton()
		{
			GameObject root = new GameObject("WeaponIcon_Hud2");
			try
			{
				CombatHudActionPresenter presenter = BuildPresenter(root, out Sprite[] icons);
				PlayerCombatController combat = BuildCombat(root, out _, out WeaponData copperHammer, out WeaponData daggersWeapon);
				combat.SetEquippedWeapons(copperHammer, daggersWeapon, string.Empty);
				SetCombat(presenter, combat);

				InvokeRefreshWeapon(presenter, copperHammer);

				Assert.That(presenter.Attack.Icon.sprite, Is.SameAs(icons[0]), "Hammerfamilie zeigt das Hammer-Icon");
				Assert.That(presenter.WeaponSwitch.Icon.sprite, Is.SameAs(icons[1]), "Wechselbutton zeigt die tatsaechliche Zweitwaffe (Dolche)");
				Assert.That(presenter.WeaponSwitch.Icon.enabled, Is.True);
				Assert.That(LockMark(presenter.WeaponSwitch).activeSelf, Is.False, "Mit Zweitwaffe ist der Wechsel verfuegbar");
			}
			finally
			{
				UnityEngine.Object.DestroyImmediate(root);
			}
		}

		[Test]
		public void CombatHudPrefab_TraegtDasSpeerIcon()
		{
			GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Resources/UI/CombatHUD.prefab");
			Assert.That(prefab, Is.Not.Null);
			CombatHudActionPresenter presenter = prefab.GetComponentInChildren<CombatHudActionPresenter>(true);
			Assert.That(presenter, Is.Not.Null);
			FieldInfo field = typeof(CombatHudActionPresenter).GetField("spearIcon", BindingFlags.NonPublic | BindingFlags.Instance);
			Assert.That(field, Is.Not.Null, "Feld spearIcon fehlt am Presenter");
			Sprite sprite = field.GetValue(presenter) as Sprite;
			Assert.That(sprite != null, Is.True, "spearIcon ist im CombatHUD.prefab nicht verdrahtet");
		}

		// ------------------------------------------------------------------
		// Fixture
		// ------------------------------------------------------------------

		private static CombatHudActionPresenter BuildPresenter(GameObject root, out Sprite[] icons)
		{
			CombatHudActionPresenter presenter = root.AddComponent<CombatHudActionPresenter>();
			CanvasGroup cluster = root.AddComponent<CanvasGroup>();
			icons = new Sprite[10];
			for (int index = 0; index < icons.Length; index++)
			{
				icons[index] = MakeSprite("Icon_" + index);
			}
			presenter.ConfigureReferences(cluster,
				BuildSlot(root.transform, "Attack"), BuildSlot(root.transform, "Dodge"),
				BuildSlot(root.transform, "Skill1"), BuildSlot(root.transform, "Skill2"),
				BuildSlot(root.transform, "WeaponSwitch"), BuildSlot(root.transform, "EidraSwitch"),
				BuildSlot(root.transform, "Potion"), BuildSlot(root.transform, "Food"),
				new GameObject("Inventory").AddComponent<Button>(),
				new GameObject("Feedback").AddComponent<Text>(), icons);
			return presenter;
		}

		private static PlayerCombatController BuildCombat(GameObject root, out WeaponData copperSpear, out WeaponData copperHammer, out WeaponData daggersWeapon)
		{
			GameObject player = new GameObject("WeaponIcon_Player");
			player.transform.SetParent(root.transform, worldPositionStays: false);
			PlayerInputReader input = player.AddComponent<PlayerInputReader>();
			PlayerMotor motor = player.AddComponent<PlayerMotor>();
			MeleeWeaponHitbox hitbox = player.AddComponent<MeleeWeaponHitbox>();
			PlayerCombatController combat = player.AddComponent<PlayerCombatController>();
			GameObject weaponObject = new GameObject("WeaponDriver");
			weaponObject.transform.SetParent(player.transform, worldPositionStays: false);
			WeaponData hammer = LoadWeapon("Hammer");
			daggersWeapon = LoadWeapon("Daggers");
			copperSpear = LoadWeapon("CopperSpear");
			copperHammer = LoadWeapon("CopperHammer");
			combat.Initialize(input, motor, new SceneCombatTargetQuery(), hitbox, null, hammer, daggersWeapon, weaponObject.transform);
			return combat;
		}

		private static void SetCombat(CombatHudActionPresenter presenter, PlayerCombatController combat)
		{
			typeof(CombatHudActionPresenter)
				.GetField("_combat", BindingFlags.NonPublic | BindingFlags.Instance)
				.SetValue(presenter, combat);
		}

		private static void InvokeRefreshWeapon(CombatHudActionPresenter presenter, WeaponData current)
		{
			typeof(CombatHudActionPresenter)
				.GetMethod("RefreshWeapon", BindingFlags.NonPublic | BindingFlags.Instance)
				.Invoke(presenter, new object[] { current, null });
		}

		private static CombatHudActionSlot BuildSlot(Transform parent, string name)
		{
			GameObject holder = new GameObject(name);
			holder.transform.SetParent(parent, worldPositionStays: false);
			Button button = holder.AddComponent<Button>();
			CanvasGroup group = holder.AddComponent<CanvasGroup>();
			Image frame = NewImage(holder.transform, "Frame");
			Image icon = NewImage(holder.transform, "Icon");
			Image cooldown = NewImage(holder.transform, "Cooldown");
			GameObject lockMark = new GameObject("Lock");
			lockMark.transform.SetParent(holder.transform, worldPositionStays: false);
			Text value = new GameObject("Value").AddComponent<Text>();
			value.transform.SetParent(holder.transform, worldPositionStays: false);
			return new CombatHudActionSlot(button, group, frame, icon, cooldown, lockMark, value);
		}

		private static Image NewImage(Transform parent, string name)
		{
			GameObject holder = new GameObject(name);
			holder.transform.SetParent(parent, worldPositionStays: false);
			return holder.AddComponent<Image>();
		}

		private static Sprite MakeSprite(string name)
		{
			Texture2D texture = new Texture2D(4, 4);
			Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f));
			sprite.name = name;
			return sprite;
		}

		private static GameObject LockMark(CombatHudActionSlot slot)
		{
			return (GameObject)typeof(CombatHudActionSlot)
				.GetField("lockMark", BindingFlags.NonPublic | BindingFlags.Instance)
				.GetValue(slot);
		}

		private static WeaponData LoadWeapon(string name)
		{
			WeaponData weapon = AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/_Game/Data/Weapons/" + name + ".asset");
			Assert.That(weapon, Is.Not.Null, name);
			return weapon;
		}
	}
}
