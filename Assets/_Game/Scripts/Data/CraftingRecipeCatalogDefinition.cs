using System.Collections.Generic;
using System;
using UnityEngine;

namespace Eidren.Data
{
	[CreateAssetMenu(menuName = "Eidren/Crafting/Recipe Catalog")]
	public sealed class CraftingRecipeCatalogDefinition : ScriptableObject
	{
		[SerializeField]
		private CraftingRecipeDefinition[] recipes = Array.Empty<CraftingRecipeDefinition>();

		public IReadOnlyList<CraftingRecipeDefinition> Recipes => recipes ?? Array.Empty<CraftingRecipeDefinition>();

		public CraftingRecipeDefinition[] RecipeArray => (recipes != null) ? ((CraftingRecipeDefinition[])recipes.Clone()) : Array.Empty<CraftingRecipeDefinition>();
	}

	public static class CraftingRules
	{
		public const int MaximumIngredientTypes = 5;
	}
}
