using Eidren.Data;
using Eidren.Presentation;
using System;
using UnityEngine;

namespace Eidren.Combat
{
	public sealed class PlayerWeaponVisual : MonoBehaviour, IPlayerWeaponPresentation
	{
		[SerializeField]
		private GameObject hammerRoot;

		[SerializeField]
		private GameObject daggersRoot;

		[SerializeField]
		private GameObject spearRoot;

		[SerializeField]
		private Sprite sealbreakerSprite;

		[SerializeField]
		private Sprite ashFangsSprite;

		[SerializeField]
		private Sprite emberThornSprite;

		/// <summary>
		/// Einzelner Dolch fuer die beidhaendige Darstellung. Ist das Feld gesetzt,
		/// ersetzen zwei handgefuehrte Dolche die Sammeldarstellung in daggersRoot.
		/// </summary>
		/// <remarks>
		/// Aus dem Visual-Fix-Shim uebernommen (G-000, Stufe 6). Dort wurde das
		/// Sprite zur Laufzeit als Rohdaten aus StreamingAssets geladen, weil die
		/// Quellen nicht erreichbar waren. Ueber die endgueltige Waffenanbindung
		/// entscheidet G-006.
		/// </remarks>
		[SerializeField]
		private Sprite singleDaggerSprite;

		private SpriteActorPresentation _actor;

		private SpriteRenderer _leftHandDagger;

		private SpriteRenderer _rightHandDagger;

		private SpriteRenderer[] _legacyDaggerRenderers;

		private const float SingleDaggerScale = 0.48f;

		private Camera _camera;

		private Vector3 _hammerPosition;

		private Vector3 _daggersPosition;

		private Vector3 _spearPosition;

		private Vector3 _hammerScale = Vector3.one;

		private Vector3 _daggersScale = Vector3.one;

		private Vector3 _spearScale = Vector3.one;

		private Sprite _regularHammerSprite;

		private Sprite _regularDaggersSprite;

		private Sprite _regularSpearSprite;

		private float _spearAttackStartedAt = -1f / 0f;

		private float _spearAttackDuration;

		private int _spearComboIndex;

		public GameObject HammerRoot => hammerRoot;

		public GameObject DaggersRoot => daggersRoot;

		public GameObject SpearRoot => spearRoot;

		public Sprite SealbreakerSprite => sealbreakerSprite;

		public Sprite AshFangsSprite => ashFangsSprite;

		public Sprite EmberThornSprite => emberThornSprite;

		public Sprite CurrentSpearSprite => SpriteOf(spearRoot);

		public void Configure(GameObject hammer, GameObject daggers)
		{
			Configure(hammer, daggers, null);
		}

		public void Configure(GameObject hammer, GameObject daggers, GameObject spear)
		{
			hammerRoot = hammer;
			daggersRoot = daggers;
			spearRoot = spear;
			CaptureBasePose();
			CaptureBaseSprites();
			Show(null);
		}

		public void ConfigureNamedSprites(Sprite sealbreaker, Sprite ashFangs, Sprite emberThorn)
		{
			sealbreakerSprite = sealbreaker;
			ashFangsSprite = ashFangs;
			emberThornSprite = emberThorn;
		}

		private void Awake()
		{
			CaptureBasePose();
			CaptureBaseSprites();
		}

		public void Show(WeaponData weapon)
		{
			WeaponFamily weaponFamily = ((weapon != null) ? weapon.Identity.Family : WeaponFamily.None);
			if (hammerRoot != null)
			{
				hammerRoot.SetActive(weaponFamily == WeaponFamily.Hammer);
			}
			if (daggersRoot != null)
			{
				daggersRoot.SetActive(weaponFamily == WeaponFamily.Daggers);
			}
			if (spearRoot != null)
			{
				spearRoot.SetActive(weaponFamily == WeaponFamily.Spear);
			}
			ApplyVariantSprites(weapon);
		}

		public void PlayAttack(float duration, int comboIndex, WeaponFamily family)
		{
			if (family == WeaponFamily.Spear)
			{
				_spearAttackStartedAt = Time.time;
				_spearAttackDuration = Mathf.Max(0.05f, duration);
				_spearComboIndex = Mathf.Clamp(comboIndex, 0, 2);
			}
		}

		private void LateUpdate()
		{
			if (_actor == null)
			{
				_actor = base.transform.root.GetComponentInChildren<SpriteActorPresentation>(includeInactive: true);
			}
			if (_camera == null)
			{
				_camera = Camera.main;
			}
			if (_camera != null)
			{
				base.transform.rotation = _camera.transform.rotation;
			}
			if (!(_actor == null))
			{
				bool mirror = _actor.Facing == ActorFacing8.W || _actor.Facing == ActorFacing8.NW || _actor.Facing == ActorFacing8.SW;
				bool behindBody = _actor.Facing == ActorFacing8.N || _actor.Facing == ActorFacing8.NE || _actor.Facing == ActorFacing8.NW;
				PoseEquipment(hammerRoot, _hammerPosition, _hammerScale, mirror, behindBody);
				if (singleDaggerSprite != null)
				{
					PoseDaggerHands();
				}
				else
				{
					PoseEquipment(daggersRoot, _daggersPosition, _daggersScale, mirror: false, behindBody);
				}
				PoseEquipment(spearRoot, SpearAttackPosition(), _spearScale, mirror, behindBody);
			}
		}

		/// <summary>
		/// Stellt zwei handgefuehrte Dolche dar und blendet die Sammeldarstellung aus.
		/// </summary>
		/// <remarks>
		/// Positionen, Drehungen und Sortierreihenfolgen stammen unveraendert aus dem
		/// Visual-Fix-Shim (G-000, Stufe 6). G-006 entscheidet ueber die endgueltige
		/// Anbindung; bis dahin bleibt die Darstellung so wie im Build v0.2.
		/// </remarks>
		private void PoseDaggerHands()
		{
			if (_leftHandDagger == null || _rightHandDagger == null)
			{
				CreateDaggerHands();
			}
			if (_legacyDaggerRenderers != null)
			{
				for (int i = 0; i < _legacyDaggerRenderers.Length; i++)
				{
					if (_legacyDaggerRenderers[i] != null)
					{
						_legacyDaggerRenderers[i].enabled = false;
					}
				}
			}

			bool visible = daggersRoot != null && daggersRoot.activeInHierarchy;
			_leftHandDagger.gameObject.SetActive(visible);
			_rightHandDagger.gameObject.SetActive(visible);
			if (!visible)
			{
				return;
			}

			float leftX = -0.23f;
			float rightX = 0.23f;
			float leftY = 1.04f;
			float rightY = 1.04f;
			int leftOrder = 14;
			int rightOrder = 14;

			switch (_actor.Facing)
			{
			case ActorFacing8.N:
				leftY = (rightY = 1.1f);
				leftOrder = (rightOrder = 11);
				break;
			case ActorFacing8.NE:
				leftX = -0.08f;
				rightX = 0.19f;
				leftY = 1.1f;
				rightY = 1.06f;
				leftOrder = 11;
				rightOrder = 14;
				break;
			case ActorFacing8.E:
				leftX = -0.06f;
				rightX = 0.17f;
				leftY = 1.08f;
				rightY = 1.03f;
				leftOrder = 11;
				rightOrder = 14;
				break;
			case ActorFacing8.SE:
				leftX = -0.14f;
				rightX = 0.22f;
				leftY = 1.06f;
				rightY = 1.01f;
				leftOrder = 12;
				rightOrder = 14;
				break;
			case ActorFacing8.SW:
				leftX = -0.22f;
				rightX = 0.14f;
				leftY = 1.01f;
				rightY = 1.06f;
				leftOrder = 14;
				rightOrder = 12;
				break;
			case ActorFacing8.W:
				leftX = -0.17f;
				rightX = 0.06f;
				leftY = 1.03f;
				rightY = 1.08f;
				leftOrder = 14;
				rightOrder = 11;
				break;
			case ActorFacing8.NW:
				leftX = -0.19f;
				rightX = 0.08f;
				leftY = 1.06f;
				rightY = 1.1f;
				leftOrder = 14;
				rightOrder = 11;
				break;
			}

			PoseDagger(_leftHandDagger, new Vector3(leftX, leftY, 0f), 90f, 0f - SingleDaggerScale, leftOrder);
			PoseDagger(_rightHandDagger, new Vector3(rightX, rightY, 0f), -90f, SingleDaggerScale, rightOrder);
		}

		private void CreateDaggerHands()
		{
			if (daggersRoot != null && _legacyDaggerRenderers == null)
			{
				_legacyDaggerRenderers = daggersRoot.GetComponentsInChildren<SpriteRenderer>(includeInactive: true);
			}
			Material shared = (_legacyDaggerRenderers != null && _legacyDaggerRenderers.Length > 0)
				? _legacyDaggerRenderers[0].sharedMaterial
				: null;
			_leftHandDagger = CreateDaggerHand("Left Hand Dagger", shared);
			_rightHandDagger = CreateDaggerHand("Right Hand Dagger", shared);
		}

		private SpriteRenderer CreateDaggerHand(string objectName, Material shared)
		{
			var host = new GameObject(objectName);
			host.transform.SetParent(base.transform, worldPositionStays: false);
			SpriteRenderer renderer = host.AddComponent<SpriteRenderer>();
			renderer.sprite = singleDaggerSprite;
			renderer.sharedMaterial = shared;
			return renderer;
		}

		private static void PoseDagger(SpriteRenderer renderer, Vector3 localPosition, float angle, float horizontalScale, int sortingOrder)
		{
			renderer.transform.localPosition = localPosition;
			renderer.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
			renderer.transform.localScale = new Vector3(horizontalScale, SingleDaggerScale, SingleDaggerScale);
			renderer.sortingOrder = sortingOrder;
		}

		private Vector3 SpearAttackPosition()
		{
			float num = Time.time - _spearAttackStartedAt;
			if (num < 0f || num >= _spearAttackDuration)
			{
				return _spearPosition;
			}
			float num2 = num / _spearAttackDuration;
			float num3 = Mathf.Sin(num2 * (float)Math.PI);
			float num4 = 0.18f + (float)_spearComboIndex * 0.07f;
			return _spearPosition + Vector3.right * num3 * num4;
		}

		private void ApplyVariantSprites(WeaponData weapon)
		{
			SetSprite(hammerRoot, (weapon != null && weapon.Id == "sealbreaker") ? sealbreakerSprite : _regularHammerSprite);
			SetSprite(daggersRoot, (weapon != null && weapon.Id == "ash_fangs") ? ashFangsSprite : _regularDaggersSprite);
			SetSprite(spearRoot, (weapon != null && weapon.Id == "ember_thorn") ? emberThornSprite : _regularSpearSprite);
		}

		private static void SetSprite(GameObject root, Sprite sprite)
		{
			if (!(root == null) && !(sprite == null))
			{
				SpriteRenderer component = root.GetComponent<SpriteRenderer>();
				if (component != null)
				{
					component.sprite = sprite;
				}
			}
		}

		private static void PoseEquipment(GameObject equipment, Vector3 basePosition, Vector3 baseScale, bool mirror, bool behindBody)
		{
			if (!(equipment == null))
			{
				Transform transform = equipment.transform;
				Vector3 localPosition = basePosition;
				localPosition.z = (behindBody ? 0.08f : (-0.12f));
				transform.localPosition = localPosition;
				transform.localScale = new Vector3(Mathf.Abs(baseScale.x) * (mirror ? (-1f) : 1f), baseScale.y, baseScale.z);
				SpriteRenderer[] componentsInChildren = equipment.GetComponentsInChildren<SpriteRenderer>(includeInactive: true);
				foreach (SpriteRenderer spriteRenderer in componentsInChildren)
				{
					spriteRenderer.sortingOrder = (behindBody ? 11 : 14);
				}
			}
		}

		private void CaptureBasePose()
		{
			if (hammerRoot != null)
			{
				_hammerPosition = hammerRoot.transform.localPosition;
				_hammerScale = hammerRoot.transform.localScale;
			}
			if (daggersRoot != null)
			{
				_daggersPosition = daggersRoot.transform.localPosition;
				_daggersScale = daggersRoot.transform.localScale;
			}
			if (spearRoot != null)
			{
				_spearPosition = spearRoot.transform.localPosition;
				_spearScale = spearRoot.transform.localScale;
			}
		}

		private void CaptureBaseSprites()
		{
			_regularHammerSprite = SpriteOf(hammerRoot) ?? _regularHammerSprite;
			_regularDaggersSprite = SpriteOf(daggersRoot) ?? _regularDaggersSprite;
			_regularSpearSprite = SpriteOf(spearRoot) ?? _regularSpearSprite;
		}

		private static Sprite SpriteOf(GameObject root)
		{
			return (!(root != null)) ? null : root.GetComponent<SpriteRenderer>()?.sprite;
		}
	}
}
