using System;
using UnityEngine;

namespace Eidren.Presentation
{
	[CreateAssetMenu(menuName = "Eidren/Presentation/Style Proof Library")]
	public sealed class StyleProofVisualLibrary : ScriptableObject
	{
		private const string ResourcePath = "StyleProof/StyleProofVisualLibrary";

		[SerializeField]
		private Sprite[] styleCueSprites = Array.Empty<Sprite>();

		[SerializeField]
		private GameObject[] forestPropPrefabs = Array.Empty<GameObject>();

		private static StyleProofVisualLibrary _instance;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetCache()
		{
			_instance = null;
		}

		public static Sprite GetCueSprite(StyleCue cue)
		{
			if (_instance == null)
			{
				_instance = Resources.Load<StyleProofVisualLibrary>("StyleProof/StyleProofVisualLibrary");
			}
			return (_instance != null && _instance.styleCueSprites != null && cue >= StyleCue.HammerHit && (int)cue < _instance.styleCueSprites.Length) ? _instance.styleCueSprites[(int)cue] : null;
		}

		public static GameObject GetForestProp(int index)
		{
			if (_instance == null)
			{
				_instance = Resources.Load<StyleProofVisualLibrary>("StyleProof/StyleProofVisualLibrary");
			}
			return (_instance != null && _instance.forestPropPrefabs != null && index >= 0 && index < _instance.forestPropPrefabs.Length) ? _instance.forestPropPrefabs[index] : null;
		}
	}
}
