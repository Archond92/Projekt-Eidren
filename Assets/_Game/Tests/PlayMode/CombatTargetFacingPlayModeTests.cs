using System.Collections;
using Eidren.Combat;
using Eidren.Data;
using Eidren.Input;
using Eidren.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Eidren.Tests.PlayMode
{
	public sealed class CombatTargetFacingPlayModeTests
	{
		private sealed class FixedTargetQuery : ICombatTargetQuery
		{
			private readonly CombatTarget _target;
			public FixedTargetQuery(IDamageable target) { _target = new CombatTarget(target); }
			public bool TryFindClosest(Transform attacker, float range, out CombatTarget target)
			{
				target = _target;
				return true;
			}
		}

		[UnityTest]
		public IEnumerator DodgeBeforeAndAfterFacingKeepsInputDirection()
		{
			yield return Testumgebung.LeereWeltBereitstellen();
			GameObject player = new GameObject("HUD100_AutoFacing_Player");
			GameObject cameraObject = new GameObject("HUD100_AutoFacing_Camera");
			GameObject targetObject = new GameObject("HUD100_AutoFacing_Target");
			WeaponData weapon = ScriptableObject.CreateInstance<WeaponData>();
			try
			{
				player.AddComponent<CharacterController>();
				Damageable health = player.AddComponent<Damageable>();
				health.Initialize(100f);
				PlayerInputReader input = player.AddComponent<PlayerInputReader>();
				PlayerMotor motor = player.AddComponent<PlayerMotor>();
				Camera camera = cameraObject.AddComponent<Camera>();
				camera.transform.rotation = Quaternion.identity;
				motor.Initialize(input, camera, health, Vector3.zero, 50f);
				input.SetVirtualMove(Vector2.left);
				yield return null;
				yield return null;

				input.PressDodge();
				Assert.That(motor.IsDodging, Is.True);
				Assert.That(Vector3.Angle(Vector3.left, motor.WorldMoveDirection), Is.LessThan(.1f));
				while (motor.IsDodging) yield return null;

				player.transform.rotation = Quaternion.identity;
				targetObject.transform.position = player.transform.position
					+ Quaternion.Euler(0f, 30f, 0f) * Vector3.forward * 2f;
				Damageable target = targetObject.AddComponent<Damageable>();
				target.Initialize(100f);
				weapon.AttackAssistAngle = 120f;
				weapon.Identity = new WeaponIdentityData { Family = WeaponFamily.Hammer };

				CombatTargetFacing.Apply(player.transform, new FixedTargetQuery(target),
					new AttackStepData { HitboxRange = 3f }, weapon);
				Assert.That(Vector3.Angle(Vector3.forward, player.transform.forward), Is.EqualTo(21f).Within(.1f));
				Assert.That(Vector3.Angle(Vector3.left, motor.LastValidWorldMoveDirection), Is.LessThan(.1f));

				input.PressDodge();
				Assert.That(motor.IsDodging, Is.True);
				Assert.That(Vector3.Angle(Vector3.left, motor.WorldMoveDirection), Is.LessThan(.1f));
			}
			finally
			{
				Object.Destroy(player);
				Object.Destroy(cameraObject);
				Object.Destroy(targetObject);
				Object.Destroy(weapon);
			}
			yield return null;
		}
	}
}
