using Eidren.Data;
using Eidren.Presentation;
using UnityEngine;

namespace Eidren.Combat
{
	/// <summary>
	/// 3D-Ersatz fuer PlayerWeaponVisual. PHASE-2 uebersetzt Familie und konkrete
	/// Item-ID in Stance, Materialstufe/Spezialoptik und Mesh-Sichtbarkeit.
	/// </summary>
	public sealed class WandererEquipmentVisual : MonoBehaviour, IPlayerWeaponPresentation
	{
		private MeshActorPresentation _mesh;

		public void Show(WeaponData weapon)
		{
			MeshActorPresentation mesh = ResolveMesh();
			if (mesh == null)
			{
				return;
			}
			WeaponFamily family = (weapon != null) ? weapon.Identity.Family : WeaponFamily.None;
			var (stance, visible) = ResolveStance(family);
			mesh.SetWeaponItem(weapon != null ? weapon.Id : null, stance, visible);
		}

		public void PlayAttack(float duration, int comboIndex, WeaponFamily family)
		{
			// Der Angriffs-Clip laeuft ueber die Stems des PlayerVisualAnimator;
			// hier ist nichts zu tun.
		}

		public static (string stance, bool weaponVisible) ResolveStance(WeaponFamily family)
		{
			switch (family)
			{
			case WeaponFamily.Hammer:
				return (MeshActorPresentation.StanceHammer, true);
			case WeaponFamily.Daggers:
				return (MeshActorPresentation.StanceDaggers, true);
			case WeaponFamily.Spear:
				return (MeshActorPresentation.StanceSpear, true);
			default:
				// Der Mid-Poly-Wanderer besitzt eigene Ruhe-/Laufclips ohne Waffe.
				return (MeshActorPresentation.StanceBare, false);
			}
		}

		private MeshActorPresentation ResolveMesh()
		{
			if (_mesh == null)
			{
				_mesh = base.transform.root.GetComponentInChildren<MeshActorPresentation>(includeInactive: true);
			}
			return _mesh;
		}
	}
}
