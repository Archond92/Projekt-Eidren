using Eidren.Combat;
using Eidren.Core.Services;
using Eidren.Data;
using Eidren.Interaction;
using Eidren.Player;
using Eidren.Presentation;
using UnityEngine;

namespace Eidren.Gameplay.Presentation
{
	public sealed class PlayerVisualAnimator : MonoBehaviour
	{
		private PlayerMotor _motor;

		private PlayerCombatController _combat;

		private Damageable _health;

		private IActorPresentation _presentation;

		private ILocomotionPresentation _locomotion;

		private IAuthoredStatePresentation _authored;

		private IArmorPresentation _armor;

		private MeshActorPresentation _meshPresentation;

		private readonly EightDirectionResolver _directionResolver = new EightDirectionResolver();

		private float _spriteLockedUntil;

		private bool _dead;

		private PlayerHarvestVisual _harvestVisual;

		public PlayerHarvestVisual HarvestVisual => _harvestVisual;

		public void Initialize(PlayerMotor motor, PlayerCombatController combat, Damageable health)
		{
			InitializeExploration(motor, health);
			BindCombat(combat);
		}

		public void BindCombat(PlayerCombatController combat)
		{
			if (!(_combat == combat))
			{
				if (_combat != null)
				{
					_combat.AttackStarted -= OnAttackStarted;
					_combat.WeaponSwitchStarted -= OnWeaponSwitchStarted;
				}
				_combat = combat;
				if (!(_combat == null))
				{
					_combat.AttackStarted += OnAttackStarted;
					_combat.WeaponSwitchStarted += OnWeaponSwitchStarted;
				}
			}
		}

		public void InitializeExploration(PlayerMotor motor, Damageable health)
		{
			_motor = motor;
			_health = health;
			_presentation = ActorPresentationLocator.Find(base.gameObject);
			_locomotion = _presentation as ILocomotionPresentation;
			_authored = _presentation as IAuthoredStatePresentation;
			_armor = _presentation as IArmorPresentation;
			_meshPresentation = _presentation as MeshActorPresentation;
			if (_presentation != null)
			{
				_directionResolver.Reset();
			}
			_authored?.SetAuthoredState("idle", 0f, loop: true, restart: true);
			_health.Damaged += OnDamaged;
			_health.Died += OnDied;
			_motor.DodgeStarted += OnDodgeStarted;
		}

		public void BindInteraction(InteractionController interaction, PlayerInventory inventory)
		{
			_harvestVisual = GetComponent<PlayerHarvestVisual>() ?? base.gameObject.AddComponent<PlayerHarvestVisual>();
			_harvestVisual.Initialize(interaction, inventory, null, _presentation != null);
		}

		private void Update()
		{
			if (!(_presentation != null) || !(_motor != null))
			{
				return;
			}
			Vector3 worldDirection = ResolveFacingDirection();
			_locomotion?.SetLocomotion(_motor.WorldMoveDirection, _motor.IsMoving || _motor.IsDodging);
			_presentation.SetFacing(_directionResolver.Resolve(worldDirection));
			if (!_dead)
			{
				if (_harvestVisual != null && _harvestVisual.IsHarvesting)
				{
					// W-001: Werkzeug-Mesh und Abbau-Clip der 3D-Figur; der Clip
					// laeuft als Schleife (PlayTimed wuerde ihn pro Frame auf
					// t=0 zuruecksetzen). Ohne Werkzeug bleibt der prozedurale
					// Rueckfall der Presentation zustaendig.
					string toolStance = MeshActorPresentation.ToolStanceForItem(_harvestVisual.LastResolvedToolItemId);
					if (toolStance != null)
					{
						_meshPresentation?.SetHarvestToolItem(_harvestVisual.LastResolvedToolItemId, toolStance);
					}
					else
					{
						// F34-008: Abbau von Hand — Waffe weg, leere Haende, prozedurale Abbaupose.
						_meshPresentation?.SetBareHands();
					}
					_authored?.SetAuthoredState("harvest", _harvestVisual.Progress, loop: true);
				}
				else if (_harvestVisual != null && _harvestVisual.IsOpening)
				{
					// W-009: kniende Oeffnungspose mit leeren Haenden; der
					// Oeffnen-Clip laeuft als Schleife.
					_meshPresentation?.SetBareHands();
					_authored?.SetAuthoredState("open", 0f, loop: true);
				}
				else
				{
					_meshPresentation?.ClearHarvestTool();
					if (!(Time.time < _spriteLockedUntil))
					{
						_authored?.SetAuthoredState(_motor.IsMoving ? "move" : "idle", 0f, loop: true);
					}
				}
			}
		}

		/// <summary>
		/// Bestimmt die Richtung, nach der die Figur ausgerichtet wird.
		/// </summary>
		/// <remarks>
		/// Aus dem Visual-Fix-Shim uebernommen (G-000, Stufe 6). PlayerMotor setzt
		/// IsMoving beim Ausweichen auf false und dreht den Transform nur verzoegert
		/// per Slerp; transform.forward laeuft der tatsaechlichen Richtung deshalb
		/// nach. Waehrend Bewegung und Ausweichen zaehlt die Bewegungsrichtung, im
		/// Stillstand die zuletzt gueltige, und nur beim Angriff die Blickrichtung.
		/// </remarks>
		private Vector3 ResolveFacingDirection()
		{
			if (_motor.IsMoving || _motor.IsDodging)
			{
				return (_motor.WorldMoveDirection.sqrMagnitude > 0.0001f)
					? _motor.WorldMoveDirection
					: _motor.LastValidWorldMoveDirection;
			}
			if (_combat != null && _combat.IsAttacking)
			{
				return base.transform.forward;
			}
			return _motor.LastValidWorldMoveDirection;
		}

		private void OnAttackStarted(float duration, int comboIndex, WeaponFamily family)
		{
			base.transform.root.GetComponentInChildren<IPlayerWeaponPresentation>(true)?.PlayAttack(duration, comboIndex, family);
			if (_presentation != null)
			{
				int max = ((family == WeaponFamily.Daggers) ? 4 : 3);
				if (1 == 0)
				{
				}
				string text = family switch
				{
					WeaponFamily.Daggers => "dagger",
					WeaponFamily.Spear => "spear",
					_ => "hammer",
				};
				if (1 == 0)
				{
				}
				string text2 = text;
				string stem = text2 + Mathf.Clamp(comboIndex + 1, 1, max);
				PlayLockedState(stem, duration);
			}
		}

		private void OnWeaponSwitchStarted()
		{
			if (_presentation != null)
			{
				PlayLockedState("idle", 0.16f);
			}
		}

		private void OnDamaged(DamageInfo damage)
		{
			if (_presentation != null)
			{
				PlayLockedState("hit", 0.2f);
			}
		}

		private void OnDodgeStarted()
		{
			if (_presentation != null)
			{
				PlayLockedState("dodge", 0.32f);
			}
		}

		private void OnDied()
		{
			_dead = true;
			if (_presentation != null)
			{
				PlayLockedState("death", 10f);
			}
		}

		public void SetEquipmentSet(bool head, bool chest, bool hands, bool legs)
		{
			if (!(_armor == null))
			{
				_armor.SetAssetVariant("player_base");
				_armor.SetArmorParts(head, chest, hands, legs);
			}
		}

		public void SetEquipmentTiers(int head, int chest, int hands, int legs)
		{
			if (!(_armor == null))
			{
				_armor.SetAssetVariant("player_base");
				_armor.SetArmorTiers(head, chest, hands, legs);
			}
		}

		private void PlayLockedState(string stem, float duration)
		{
			if (!(_presentation == null))
			{
				_authored?.SetAuthoredTimedState(stem, Mathf.Max(0.05f, duration));
				_spriteLockedUntil = Time.time + Mathf.Max(0.05f, duration);
			}
		}

		private void OnDestroy()
		{
			if (_combat != null)
			{
				_combat.AttackStarted -= OnAttackStarted;
				_combat.WeaponSwitchStarted -= OnWeaponSwitchStarted;
			}
			if (_health != null)
			{
				_health.Damaged -= OnDamaged;
				_health.Died -= OnDied;
			}
			if (_motor != null)
			{
				_motor.DodgeStarted -= OnDodgeStarted;
			}
		}
	}
}
