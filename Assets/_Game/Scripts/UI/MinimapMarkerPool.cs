using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Eidren.UI
{
	/// <summary>
	/// Wiederverwendbare Kartenmarker nach dem Muster von
	/// <c>BuildGridMarkPool</c>: Im Kartentakt wird nichts erzeugt und nichts
	/// zerstört, überzählige Marker werden nur abgeschaltet (§8).
	/// </summary>
	internal sealed class MinimapMarkerPool
	{
		private readonly List<Image> _markers = new List<Image>();

		private readonly Image _template;

		private readonly RectTransform _parent;

		private int _used;

		public MinimapMarkerPool(Image template, RectTransform parent)
		{
			if (!(template != null))
			{
				throw new ArgumentNullException("template");
			}
			if (!(parent != null))
			{
				throw new ArgumentNullException("parent");
			}
			_template = template;
			_parent = parent;
		}

		public void Begin()
		{
			_used = 0;
		}

		public Image Take()
		{
			if (_used == _markers.Count)
			{
				Image image = Object.Instantiate(_template, _parent, worldPositionStays: false);
				image.gameObject.name = $"Marker_{_markers.Count:000}";
				RectTransform rect = image.rectTransform;
				rect.anchorMin = new Vector2(0.5f, 0.5f);
				rect.anchorMax = new Vector2(0.5f, 0.5f);
				rect.pivot = new Vector2(0.5f, 0.5f);
				image.raycastTarget = false;
				_markers.Add(image);
			}
			Image marker = _markers[_used++];
			marker.gameObject.SetActive(value: true);
			return marker;
		}

		public void End()
		{
			for (int i = _used; i < _markers.Count; i++)
			{
				if (_markers[i] != null)
				{
					_markers[i].gameObject.SetActive(value: false);
				}
			}
		}
	}
}
