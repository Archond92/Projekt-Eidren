using System.Collections.Generic;
using System;
using UnityEngine;

namespace Eidren.Data
{
	[CreateAssetMenu(menuName = "Eidren/Presentation/Visual Scale Table")]
	public sealed class VisualScaleTable : ScriptableObject
	{
		public const string ResourcePath = "Data/VisualScaleTable";

		public const float CameraPitchDegrees = 52f;

		[SerializeField]
		private VisualScaleEntry[] entries = Array.Empty<VisualScaleEntry>();

		private Dictionary<string, VisualScaleEntry> _lookup;

		public IReadOnlyList<VisualScaleEntry> Entries => entries ?? Array.Empty<VisualScaleEntry>();

		public bool TryGet(string id, out VisualScaleEntry entry)
		{
			BuildLookup();
			return _lookup.TryGetValue(id ?? string.Empty, out entry);
		}

		public VisualScaleEntry Require(string id)
		{
			if (TryGet(id, out var entry))
			{
				return entry;
			}
			throw new KeyNotFoundException("Die Groessentabelle hat keine Zeile '" + id + "'. Werte werden nicht geraten (M10.2).");
		}

		public void SetEntries(VisualScaleEntry[] replacement)
		{
			entries = replacement ?? Array.Empty<VisualScaleEntry>();
			_lookup = null;
		}

		public string[] GetValidationErrors()
		{
			List<string> list = new List<string>();
			HashSet<string> hashSet = new HashSet<string>(StringComparer.Ordinal);
			foreach (VisualScaleEntry entry in Entries)
			{
				if (entry == null)
				{
					list.Add("Die Tabelle enthaelt eine leere Zeile.");
					continue;
				}
				string id = entry.Id;
				if (string.IsNullOrWhiteSpace(id))
				{
					list.Add("Eine Zeile hat keine Kennung (§12).");
				}
				else if (!hashSet.Add(id))
				{
					list.Add("Kennung '" + id + "' kommt doppelt vor (§6).");
				}
				if (entry.IsGroundPlane)
				{
					if (entry.Height > 0f)
					{
						list.Add("'" + id + "' liegt in der Bodenebene und traegt trotzdem eine Hoehe. Dann steckt das Objekt in der falschen Klasse (M10.4).");
					}
				}
				else if (entry.Height <= 0f)
				{
					list.Add("'" + id + "' ist ein Billboard ohne Hoehe (M10.3).");
				}
				if (entry.WidthBudget <= 0f)
				{
					list.Add("'" + id + "' hat kein Breitenbudget. Ohne Budget ist die Hoehe nur die halbe Angabe (M10.7).");
				}
				if (entry.ColliderSize.x < 0f || entry.ColliderSize.y < 0f || entry.ColliderSize.z < 0f)
				{
					list.Add("'" + id + "' hat ein negatives Collider-Mass.");
				}
			}
			return list.ToArray();
		}

		private void BuildLookup()
		{
			if (_lookup != null)
			{
				return;
			}
			_lookup = new Dictionary<string, VisualScaleEntry>(StringComparer.Ordinal);
			foreach (VisualScaleEntry entry in Entries)
			{
				if (entry != null && !string.IsNullOrWhiteSpace(entry.Id))
				{
					_lookup[entry.Id] = entry;
				}
			}
		}

		private void OnValidate()
		{
			_lookup = null;
		}
	}

	[Serializable]
	public sealed class VisualScaleEntry
	{
		[SerializeField]
		private string id;

		[SerializeField]
		private VisualPlacement placement;

		[SerializeField]
		[Min(0f)]
		private float height;

		[SerializeField]
		[Min(0f)]
		private float widthBudget;

		[SerializeField]
		private Vector3 colliderSize;

		[SerializeField]
		private bool artworkMissing;

		[SerializeField]
		private string note;

		[SerializeField]
		private float frameHeight;

		[SerializeField]
		private float pivotY;

		public string Id => id ?? string.Empty;

		public VisualPlacement Placement => placement;

		public bool IsGroundPlane => placement == VisualPlacement.GroundPlane;

		public float Height => height;

		public float WidthBudget => widthBudget;

		public Vector3 ColliderSize => colliderSize;

		public bool ArtworkMissing => artworkMissing;

		public string Note => note ?? string.Empty;

		public float FrameHeight => frameHeight;

		public float PivotY => pivotY;

		public VisualScaleEntry(string id, VisualPlacement placement, float height, float widthBudget, Vector3 colliderSize, bool artworkMissing, string note)
		{
			this.id = id;
			this.placement = placement;
			this.height = height;
			this.widthBudget = widthBudget;
			this.colliderSize = colliderSize;
			this.artworkMissing = artworkMissing;
			this.note = note;
		}

		public void SetMeasuredArtFrame(float measuredHeight, float measuredPivotY)
		{
			frameHeight = measuredHeight;
			pivotY = measuredPivotY;
		}
	}
}
