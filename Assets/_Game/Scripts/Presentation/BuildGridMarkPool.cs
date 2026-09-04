using System.Collections.Generic;
using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.Presentation
{
	internal sealed class BuildGridMarkPool
	{
		private readonly List<BuildGridMark> _marks = new List<BuildGridMark>();

		private readonly GameObject _prefab;

		private readonly Transform _parent;

		private int _used;

		public Vector3 AuthoredScale => _prefab.transform.localScale;

		public BuildGridMarkPool(GameObject prefab, Transform parent)
		{
			if (!(prefab != null))
			{
				throw new ArgumentNullException("prefab");
			}
			_prefab = prefab;
			if (!(parent != null))
			{
				throw new ArgumentNullException("parent");
			}
			_parent = parent;
			if (prefab.GetComponent<BuildGridMark>() == null)
			{
				throw new ArgumentException("Rasterprefab '" + prefab.name + "' hat keine BuildGridMark-Komponente.", "prefab");
			}
		}

		public void Begin()
		{
			_used = 0;
		}

		public BuildGridMark Take()
		{
			if (_used == _marks.Count)
			{
				GameObject gameObject = UnityEngine.Object.Instantiate(_prefab, _parent, worldPositionStays: false);
				gameObject.name = $"{_prefab.name}_{_marks.Count:000}";
				_marks.Add(gameObject.GetComponent<BuildGridMark>());
			}
			BuildGridMark buildGridMark = _marks[_used++];
			buildGridMark.gameObject.SetActive(value: true);
			return buildGridMark;
		}

		public void End()
		{
			for (int i = _used; i < _marks.Count; i++)
			{
				if (_marks[i] != null)
				{
					_marks[i].gameObject.SetActive(value: false);
				}
			}
		}

		public void HideAll()
		{
			Begin();
			End();
		}
	}
}
