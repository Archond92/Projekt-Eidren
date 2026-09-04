using Eidren.Core.Services;
using UnityEngine;

namespace Eidren.Interaction
{
	/// <summary>
	/// N04-001: Der Knoten meldet sich selbst bei der Minimap an. Eigene Teildatei,
	/// weil <c>ResourceNode.cs</c> bereits über dem Klassenbudget aus §7 liegt.
	/// </summary>
	public sealed partial class ResourceNode : IMinimapMarker
	{
		MinimapMarkerKind IMinimapMarker.MinimapKind => MinimapMarkerKind.Resource;

		Vector3 IMinimapMarker.MinimapPosition => base.transform.position;

		// Nutzerentscheid 17.08.2026: Ein abgebauter Knoten verschwindet ganz von
		// der Karte, statt blass stehenzubleiben.
		bool IMinimapMarker.ShowsOnMinimap => !IsExhausted;

		string IMinimapMarker.MinimapPaletteId => (definition != null) ? definition.Id : string.Empty;

		private void OnEnable()
		{
			MinimapRegistry.Register(this);
		}

		private void OnDisable()
		{
			MinimapRegistry.Unregister(this);
		}
	}
}
