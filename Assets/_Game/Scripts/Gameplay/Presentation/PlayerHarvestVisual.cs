using Eidren.Core.Services;
using Eidren.Data;
using Eidren.Interaction;
using System;
using UnityEngine;

namespace Eidren.Gameplay.Presentation
{
	public sealed class PlayerHarvestVisual : MonoBehaviour
	{
		[Serializable]
		private struct ToolSpriteBinding
		{
			public string ItemId;

			public Sprite Sprite;
		}

		private InteractionController _interaction;

		private PlayerInventory _inventory;

		private Animator _animator;

		private GameObject _toolRoot;

		[SerializeField]
		private SpriteRenderer toolRenderer;

		[SerializeField]
		private ToolSpriteBinding[] toolSprites = Array.Empty<ToolSpriteBinding>();

		private Quaternion _restRotation;

		private float _progress;

		private bool _authoredSpriteMode;

		public string ActiveToolItemId { get; private set; } = string.Empty;

		public bool ToolVisible => _toolRoot != null && _toolRoot.activeSelf;

		public bool IsHarvesting { get; private set; }

	// W-009: Zeitgebundene Kistenoeffnung laeuft — die Figur kniet und
	// arbeitet mit leeren Haenden am Deckel.
	public bool IsOpening { get; private set; }

		public float Progress => _progress;

		public int SnapshotCount { get; private set; }

		public InteractionState LastSnapshotState { get; private set; }

		public string LastResolvedToolItemId { get; private set; } = string.Empty;

		public bool PresentationConfigured => toolRenderer != null;

		public bool HasPresentationFor(string itemId)
		{
			return ResolveSprite(itemId) != null;
		}

		public void ConfigurePresentation(SpriteRenderer renderer, string[] itemIds, Sprite[] sprites)
		{
			toolRenderer = renderer;
			_toolRoot = ((renderer != null) ? renderer.gameObject : null);
			int num = Mathf.Min((itemIds != null) ? itemIds.Length : 0, (sprites != null) ? sprites.Length : 0);
			toolSprites = new ToolSpriteBinding[num];
			for (int i = 0; i < num; i++)
			{
				toolSprites[i] = new ToolSpriteBinding
				{
					ItemId = itemIds[i],
					Sprite = sprites[i]
				};
			}
			HideTool();
		}

		public void Initialize(InteractionController interaction, PlayerInventory inventory, Animator animator, bool authoredSpriteMode = false)
		{
			Unbind();
			_interaction = interaction;
			_inventory = inventory;
			_animator = animator;
			_authoredSpriteMode = authoredSpriteMode;
			if (toolRenderer != null)
			{
				_toolRoot = toolRenderer.gameObject;
			}
			if (_interaction != null)
			{
				_interaction.SnapshotChanged += HandleSnapshot;
			}
			HideTool();
		}

		private void Update()
		{
			if (ToolVisible)
			{
				float num = Mathf.Sin(Mathf.Clamp01(_progress) * (float)Math.PI * 4f);
				_toolRoot.transform.localRotation = _restRotation * Quaternion.Euler(0f, 0f, -38f + num * 72f);
			}
		}

		private void HandleSnapshot(InteractionSnapshot snapshot)
		{
			SnapshotCount++;
			LastSnapshotState = snapshot.State;
			bool active = snapshot.State == InteractionState.Starting || snapshot.State == InteractionState.Holding;
			// W-009: Kistenoeffnung (zeitgebundene Container-Interaktion).
			IsOpening = snapshot.Type == InteractionType.Container && snapshot.Mode == InteractionMode.Timed && active;
			bool flag = (IsHarvesting = snapshot.Type == InteractionType.Resource && snapshot.Mode == InteractionMode.Timed && active);
			_progress = (flag ? snapshot.Progress : 0f);
			if (!flag)
			{
				HideTool();
				return;
			}
			if (!(_interaction.CurrentTarget is ResourceNode resourceNode))
			{
				HideTool();
				return;
			}
			ItemDefinition itemDefinition = resourceNode.ResolveTool(_inventory);
			if (itemDefinition == null)
			{
				// F34-008: Abbau von Hand (z. B. Holz der Stufe 0 ohne Axt) ist
				// erlaubt. Frueher schaltete HideTool hier IsHarvesting ab, sodass
				// die Figur waehrend des Abbaus schlicht die Ruhe-/Laufschleife
				// spielte. Jetzt bleibt der Abbauzustand erhalten; ohne Werkzeug
				// uebernimmt der prozedurale Rueckfall der Presentation.
				LastResolvedToolItemId = string.Empty;
				if (_toolRoot != null)
				{
					_toolRoot.SetActive(value: false);
				}
				ActiveToolItemId = string.Empty;
				return;
			}
			LastResolvedToolItemId = itemDefinition.Id;
			if (_authoredSpriteMode)
			{
				// W-001: Die 3D-Figur zeigt das Werkzeug als Mesh aus der
				// Wanderer.glb samt Abbau-Clip; das Sprite-Billboard entfaellt.
				// IsHarvesting und Progress bleiben fuer den Animator erhalten.
				if (_toolRoot != null)
				{
					_toolRoot.SetActive(value: false);
				}
				ActiveToolItemId = string.Empty;
				return;
			}
			if (!string.Equals(ActiveToolItemId, itemDefinition.Id, StringComparison.Ordinal))
			{
				BuildTool(itemDefinition.Id);
			}
			if (_toolRoot == null || toolRenderer == null || toolRenderer.sprite == null)
			{
				HideTool();
			}
			else
			{
				_toolRoot.SetActive(value: true);
			}
		}

		private void BuildTool(string itemId)
		{
			bool handAnchor;
			Transform parent = FindAnchor(out handAnchor);
			if (!(_toolRoot == null) && !(toolRenderer == null))
			{
				_toolRoot.transform.SetParent(parent, worldPositionStays: false);
				_toolRoot.transform.localPosition = (handAnchor ? new Vector3(0.02f, 0.08f, 0.03f) : (_authoredSpriteMode ? new Vector3(0.38f, 1.02f, 0.08f) : new Vector3(0.48f, 1.12f, 0.12f)));
				_toolRoot.transform.localRotation = Quaternion.Euler(handAnchor ? 20f : 0f, 0f, handAnchor ? 82f : (_authoredSpriteMode ? (-20f) : (-12f)));
				_restRotation = _toolRoot.transform.localRotation;
				toolRenderer.sprite = ResolveSprite(itemId);
				ActiveToolItemId = itemId ?? string.Empty;
			}
		}

		private Transform FindAnchor(out bool handAnchor)
		{
			Transform transform = ((_animator != null && _animator.isHuman) ? _animator.GetBoneTransform(HumanBodyBones.RightHand) : null);
			handAnchor = transform != null;
			return transform ?? base.transform;
		}

		private Sprite ResolveSprite(string itemId)
		{
			ToolSpriteBinding[] array = toolSprites;
			for (int i = 0; i < array.Length; i++)
			{
				ToolSpriteBinding toolSpriteBinding = array[i];
				if (string.Equals(toolSpriteBinding.ItemId, itemId, StringComparison.Ordinal))
				{
					return toolSpriteBinding.Sprite;
				}
			}
			return null;
		}

		private void HideTool()
		{
			_progress = 0f;
			IsHarvesting = false;
			if (_toolRoot != null)
			{
				_toolRoot.SetActive(value: false);
			}
			ActiveToolItemId = string.Empty;
		}

		private void Unbind()
		{
			if (_interaction != null)
			{
				_interaction.SnapshotChanged -= HandleSnapshot;
			}
			_interaction = null;
			IsOpening = false;
		}

		private void OnDestroy()
		{
			Unbind();
		}
	}
}
