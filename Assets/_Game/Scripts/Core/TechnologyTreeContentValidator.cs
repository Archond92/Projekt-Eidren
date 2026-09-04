using Eidren.Data;
using System.Collections.Generic;

namespace Eidren.Core.Services
{
	public static class TechnologyTreeContentValidator
	{
		public static string[] GetErrors(TechnologyTreeDefinition tree, ContentDatabase content, ProgressionCurveDefinition progression)
		{
			List<string> list = new List<string>();
			if (tree == null)
			{
				return new string[1] { "Technology tree data is missing." };
			}
			if (content == null)
			{
				return new string[1] { "Content database is missing." };
			}
			if (progression == null)
			{
				return new string[1] { "Progression curve is missing." };
			}
			list.AddRange(tree.GetValidationErrors());
			Dictionary<int, int> dictionary = new Dictionary<int, int>();
			foreach (TechnologyNodeDefinition node in tree.Nodes)
			{
				if (node != null)
				{
					if (!node.InitiallyUnlocked)
					{
						dictionary.TryGetValue(node.Stage, out var value);
						dictionary[node.Stage] = value + node.PointCost;
					}
					ValidateRecipes(node, content, list);
					ValidateBuildings(node, content, list);
					ValidateFeatures(node, list);
				}
			}
			foreach (KeyValuePair<int, int> item in dictionary)
			{
				if (!progression.TryGetStage(item.Key, out var curve))
				{
					list.Add($"Technology stage {item.Key} has no progression " + "curve.");
				}
				else if (item.Value > curve.MaximumLevel)
				{
					list.Add($"Technology stage {item.Key} costs {item.Value} " + "points but its maximum level is only " + $"{curve.MaximumLevel}.");
				}
			}
			return list.ToArray();
		}

		private static void ValidateRecipes(TechnologyNodeDefinition node, ContentDatabase content, ICollection<string> errors)
		{
			foreach (string recipeId in node.RecipeIds)
			{
				if (!content.TryGetCraftingRecipe(recipeId, out var _))
				{
					errors.Add("Technology '" + node.Id + "' references unknown recipe '" + recipeId + "'.");
				}
			}
		}

		private static void ValidateBuildings(TechnologyNodeDefinition node, ContentDatabase content, ICollection<string> errors)
		{
			foreach (string buildingId in node.BuildingIds)
			{
				if (!content.TryGetBuilding(buildingId, out var _))
				{
					errors.Add("Technology '" + node.Id + "' references unknown building '" + buildingId + "'.");
				}
			}
		}

		private static void ValidateFeatures(TechnologyNodeDefinition node, ICollection<string> errors)
		{
			foreach (string featureId in node.FeatureIds)
			{
				if (!TechnologyFeatureIds.IsKnown(featureId))
				{
					errors.Add("Technology '" + node.Id + "' references unknown feature '" + featureId + "'.");
				}
			}
		}
	}
}
