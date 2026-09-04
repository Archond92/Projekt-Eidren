using System;
using UnityEngine;

namespace Eidren.Presentation
{
	/// <summary>
	/// Dritter Teil von MeshActorPresentation (partial): was die Figur in den
	/// Haenden haelt. Waffenwechsel (Stance + Waffenmesh), der Werkzeug-
	/// Override waehrend des Abbaus und die leeren Haende beim Kistenoeffnen
	/// teilen sich dieselbe Mesh-Umschaltung.
	/// </summary>
	public sealed partial class MeshActorPresentation
	{
		// ------------------------------------------------------------------
		// Waffen und Meshes
		// ------------------------------------------------------------------

		/// <summary>
		/// Stellt den Waffen-Clip-Satz und die Sichtbarkeit des Waffenmeshes um.
		/// Unbewaffnet werden die eigenen Ohne-Clips ohne Mesh verwendet.
		/// </summary>
		public void SetWeaponStance(string stance, bool showWeaponMesh)
		{
			SetWeaponItem(null, stance, showWeaponMesh);
		}

		/// <summary>
		/// PHASE-2: Stellt neben der Waffenfamilie auch deren konkrete Material-
		/// oder Spezialvariante dar. Fehlt sie im geladenen Modell, wird sicher
		/// auf den bisherigen Familienknoten zurueckgefallen.
		/// </summary>
		public void SetWeaponItem(string itemId, string stance, bool showWeaponMesh)
		{
			_weaponStance = string.IsNullOrEmpty(stance)
				? (showWeaponMesh ? StanceDaggers : StanceBare)
				: stance;
			_weaponItemId = itemId;
			_weaponVisible = showWeaponMesh;
			// Ein Waffenwechsel beendet einen laufenden Werkzeug-Override —
			// der Abbau ist dann ohnehin abgebrochen.
			_harvestTool = null;
			_harvestToolItemId = null;
			_stance = _weaponStance;
			ApplyHandMeshes();
			RefreshLocomotionClip();
		}

		/// <summary>
		/// Werkzeug-Override fuer den Abbau (W-001): blendet das Werkzeug-Mesh
		/// statt der Waffe ein und schaltet die Clip-Aufloesung auf den
		/// Werkzeug-Traeger (Abbau_/Ruhe_/Laufen_-Varianten).
		/// </summary>
		public void SetHarvestTool(string toolStance)
		{
			SetHarvestToolItem(null, toolStance);
		}

		/// <summary>PHASE-2: Aktiviert die konkrete Werkzeugstufe.</summary>
		public void SetHarvestToolItem(string itemId, string toolStance)
		{
			if (string.IsNullOrEmpty(toolStance)
				|| (string.Equals(_harvestTool, toolStance, StringComparison.Ordinal)
					&& string.Equals(_harvestToolItemId, itemId, StringComparison.Ordinal)))
			{
				return;
			}
			_harvestTool = toolStance;
			_harvestToolItemId = itemId;
			_stance = toolStance;
			ApplyHandMeshes();
			RefreshLocomotionClip();
		}

		/// <summary>
		/// Beendet den Werkzeug-Override und kehrt zur ausgeruesteten Waffe
		/// zurueck.
		/// </summary>
		public void ClearHarvestTool()
		{
			if (_harvestTool == null)
			{
				return;
			}
			_harvestTool = null;
			_harvestToolItemId = null;
			_stance = _weaponStance;
			ApplyHandMeshes();
			RefreshLocomotionClip();
		}

		/// <summary>
		/// Leere-Haende-Override fuer die Kistenoeffnung (W-009): blendet
		/// Waffe und Werkzeuge aus; ClearHarvestTool stellt die Waffe wieder her.
		/// </summary>
		public void SetBareHands()
		{
			if (_harvestTool != null && _harvestTool.Length == 0)
			{
				return;
			}
			_harvestTool = string.Empty;
			_harvestToolItemId = null;
			_stance = _weaponStance;
			ApplyHandMeshes();
			RefreshLocomotionClip();
		}

		public static string ToolStanceForItem(string itemId)
		{
			if (string.IsNullOrEmpty(itemId))
			{
				return null;
			}
			// W-008: Suffix-Vergleich, damit auch copper_/iron_-Werkzeuge ihren
			// Traeger finden. Reihenfolge wichtig: "…pickaxe" endet auf "…axe".
			if (itemId.EndsWith("pickaxe", StringComparison.Ordinal))
			{
				return StancePickaxe;
			}
			if (itemId.EndsWith("axe", StringComparison.Ordinal))
			{
				return StanceAxe;
			}
			if (itemId.EndsWith("scythe", StringComparison.Ordinal))
			{
				return StanceScythe;
			}
			return null;
		}

		private void ApplyHandMeshes()
		{
			if (_namedNodes.Count == 0)
			{
				CacheHierarchy();
			}
			// _harvestTool: null = kein Override, "" = leere Haende (Oeffnen),
			// sonst der Werkzeug-Traeger.
			string generic = (_harvestTool != null)
				? ((_harvestTool.Length > 0) ? ("Waffe_" + _harvestTool) : null)
				: (_weaponVisible ? ("Waffe_" + _weaponStance) : null);
			string itemId = _harvestTool != null ? _harvestToolItemId : _weaponItemId;
			string variant = ItemVisualModule(itemId);
			string active = !string.IsNullOrEmpty(variant) && _namedNodes.ContainsKey(variant) ? variant : generic;
			foreach (string meshName in WeaponMeshNames)
			{
				SetNodeActive(meshName, meshName == active);
			}
			foreach (string meshName in WeaponVariantMeshNames)
			{
				SetNodeActive(meshName, meshName == active);
			}
			foreach (string meshName in ToolMeshNames)
			{
				SetNodeActive(meshName, meshName == active);
			}
			foreach (string meshName in ToolVariantMeshNames)
			{
				SetNodeActive(meshName, meshName == active);
			}
		}

		public static string ItemVisualModule(string itemId)
		{
			switch (itemId)
			{
			case "hammer": return "Waffe_Hammer_Base";
			case "copper_hammer": return "Waffe_Hammer_Kupfer";
			case "iron_hammer": return "Waffe_Hammer_Eisen";
			case "sealbreaker": return "Waffe_Hammer_Sealbreaker";
			case "daggers": return "Waffe_Dolche_Base";
			case "copper_daggers": return "Waffe_Dolche_Kupfer";
			case "iron_daggers": return "Waffe_Dolche_Eisen";
			case "ash_fangs": return "Waffe_Dolche_AshFangs";
			case "copper_spear": return "Waffe_Speer_Kupfer";
			case "iron_spear": return "Waffe_Speer_Eisen";
			case "ember_thorn": return "Waffe_Speer_EmberThorn";
			case "axe": return "Waffe_Axt_Base";
			case "copper_axe": return "Waffe_Axt_Kupfer";
			case "iron_axe": return "Waffe_Axt_Eisen";
			case "pickaxe": return "Waffe_Spitzhacke_Base";
			case "copper_pickaxe": return "Waffe_Spitzhacke_Kupfer";
			case "iron_pickaxe": return "Waffe_Spitzhacke_Eisen";
			case "scythe": return "Waffe_Sense_Base";
			case "copper_scythe": return "Waffe_Sense_Kupfer";
			case "iron_scythe": return "Waffe_Sense_Eisen";
			default: return null;
			}
		}

		private void RefreshLocomotionClip()
		{
			if (VisualState == ActorVisualState.Idle || VisualState == ActorVisualState.Move)
			{
				SetVisualState(VisualState);
			}
		}
	}
}
