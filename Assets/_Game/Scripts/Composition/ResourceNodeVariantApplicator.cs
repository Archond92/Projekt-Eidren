using Eidren.Data;
using Eidren.Interaction;
using UnityEngine;

namespace Eidren.Composition
{
	public static class ResourceNodeVariantApplicator
	{
		public static bool Apply(ResourceNode node, ZoneAreaArtDefinition areaArt)
		{
			if (node == null || node.Definition == null || areaArt == null || !areaArt.TryGetResourceVariant(node.Definition.Id, out var variant) || variant.ActiveVisualPrefab == null || variant.ExhaustedVisualPrefab == null)
			{
				return false;
			}
			Transform transform = node.transform;
			Transform transform2 = transform.Find("ActiveVisual");
			Transform transform3 = transform.Find("ExhaustedVisual");
			if (transform2 != null)
			{
				transform2.gameObject.SetActive(value: false);
			}
			if (transform3 != null)
			{
				transform3.gameObject.SetActive(value: false);
			}
			GameObject gameObject = Object.Instantiate(variant.ActiveVisualPrefab, transform);
			gameObject.name = "ActiveVisual";
			GameObject gameObject2 = Object.Instantiate(variant.ExhaustedVisualPrefab, transform);
			gameObject2.name = "ExhaustedVisual";
			node.Configure(node.Definition, node.InstanceId, gameObject, gameObject2, node.InteractionTrigger, node.BlockingCollider);
			DestroyOld(transform2);
			DestroyOld(transform3);
			return true;
		}

		private static void DestroyOld(Transform oldVisual)
		{
			if (!(oldVisual == null))
			{
				if (Application.isPlaying)
				{
					Object.Destroy(oldVisual.gameObject);
				}
				else
				{
					Object.DestroyImmediate(oldVisual.gameObject);
				}
			}
		}
	}
}
