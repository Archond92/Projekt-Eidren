using System;
using UnityEngine.Rendering;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.Interaction
{
	public sealed class ResourceTargetIndicator : MonoBehaviour
	{
		private const int SegmentCount = 48;

		private static readonly Color TargetGreen = new Color(0.2f, 1f, 0.36f, 1f);

		private LineRenderer _ring;

		private Material _material;

		private IInteractable _target;

		private Renderer[] _targetRenderers;
		private bool _combatSuppressed;

		public IInteractable Target => _target;

		public bool IsVisible => _ring != null && _ring.enabled;
		public bool CombatSuppressed => _combatSuppressed;

		public void SetCombatSuppressed(bool suppressed)
		{
			_combatSuppressed = suppressed;
			if (_ring != null) _ring.enabled = !suppressed && _target != null && InteractionUtility.IsActive(_target);
		}

		public void SetTarget(IInteractable target)
		{
			if (target == null || !SupportsIndicator(target.Type) || !InteractionUtility.IsActive(target))
			{
				_target = null;
				_targetRenderers = null;
				EnsureRing();
				_ring.enabled = false;
			}
			else
			{
				_target = target;
				_targetRenderers = target.InteractionObject.GetComponentsInChildren<Renderer>();
				EnsureRing();
				_ring.enabled = !_combatSuppressed;
				RefreshRing();
			}
		}

		private static bool SupportsIndicator(InteractionType type)
		{
			return type == InteractionType.Resource || type == InteractionType.Container || type == InteractionType.Station;
		}

		private void LateUpdate()
		{
			if (_target == null || !InteractionUtility.IsActive(_target))
			{
				SetTarget(null);
			}
			else
			{
				RefreshRing();
			}
		}

		private void RefreshRing()
		{
			Bounds bounds = CalculateBounds();
			Vector3 center = bounds.center;
			// Nachfix zu F31-017: Gelegte Bodenteile reichen bis y = 0,14 —
			// mit 0,065 lag der Ring darunter und war auf gelegten Boeden
			// unsichtbar. Gleiche Freihoehe wie die Bauvorschau (0,2).
			center.y = bounds.min.y + 0.2f;
			float num = Mathf.Clamp(Mathf.Max(bounds.extents.x, bounds.extents.z) * 1.18f, 0.62f, 2.2f);
			for (int i = 0; i <= 48; i++)
			{
				float f = (float)i / 48f * (float)Math.PI * 2f;
				_ring.SetPosition(i, center + new Vector3(Mathf.Cos(f) * num, 0f, Mathf.Sin(f) * num));
			}
		}

		private Bounds CalculateBounds()
		{
			Bounds result = new Bounds(_target.InteractionPosition, Vector3.one);
			bool flag = false;
			if (_targetRenderers != null)
			{
				Renderer[] targetRenderers = _targetRenderers;
				foreach (Renderer renderer in targetRenderers)
				{
					if (!(renderer == null) && renderer.enabled)
					{
						if (!flag)
						{
							result = renderer.bounds;
							flag = true;
						}
						else
						{
							result.Encapsulate(renderer.bounds);
						}
					}
				}
			}
			return result;
		}

		private void EnsureRing()
		{
			if (_ring != null)
			{
				return;
			}
			GameObject gameObject = new GameObject("ResourceTargetRing");
			gameObject.transform.SetParent(base.transform, worldPositionStays: false);
			_ring = gameObject.AddComponent<LineRenderer>();
			_ring.useWorldSpace = true;
			_ring.loop = true;
			_ring.positionCount = 49;
			_ring.widthMultiplier = 0.075f;
			_ring.numCornerVertices = 3;
			_ring.numCapVertices = 3;
			_ring.startColor = TargetGreen;
			_ring.endColor = TargetGreen;
			_ring.shadowCastingMode = ShadowCastingMode.Off;
			_ring.receiveShadows = false;
			Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
			if (shader != null)
			{
				_material = new Material(shader)
				{
					hideFlags = HideFlags.DontSave
				};
				if (_material.HasProperty("_BaseColor"))
				{
					_material.SetColor("_BaseColor", TargetGreen);
				}
				if (_material.HasProperty("_Color"))
				{
					_material.SetColor("_Color", TargetGreen);
				}
				_ring.sharedMaterial = _material;
			}
		}

		private void OnDestroy()
		{
			if (_material != null)
			{
				UnityEngine.Object.Destroy(_material);
			}
		}
	}
}
