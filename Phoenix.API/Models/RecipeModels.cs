using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Newtonsoft.Json;

namespace Phoenix.API.Models
{
    /// <summary>
    // Represents an object containing a list of recipes for items in a menu.
    /// </summary>
    public class FullRecipeModel
    {
        // List of items having a recipe
        public List<RecipeForItemModel> Items { get; set; }
    }

    public class RecipeForItemModel
    {
        // Id that uniquely identifies an item
        [JsonPropertyAttribute("Id")]
        public int ItemId { get; set; }

        // Item GUID
        [JsonPropertyAttribute("itemguid")]
        public Guid DataId { get; set; }

        // Item name for an item.
        [JsonPropertyAttribute("Name")]
        public string ItemName { get; set; }
        // Recipe for an item
        public RecipeModel Recipe { get; set; }

    }

    /// <summary>
    /// Represents a recipe for an item.
    /// </summary>
    public class RecipeModel
    {
        // Id that uniquely identifies the recipe.
        [JsonPropertyAttribute("Id")]
        public int RecipeId { get; set; }

        // Name of the recipe.
        public string Name { get; set; }

        // Serving size of the recipe.
        [JsonPropertyAttribute("ServingSize")]
        public int Servings { get; set; }

        // Total cost of the recipe.
        [JsonPropertyAttribute("TotalCost")]
        public decimal Cost { get; set; }

        // Quantity
        public decimal Quantity { get; set; }

        // Unit of measure
        public string UnitOfMeasure { get; set; }

        // List of ingredients in the recipe.
        public List<IngredientModel> Ingredients { get; set; }

        // List of linked recipes.
        public List<RecipeModel> Recipes { get; set; }
    }

    /// <summary>
    /// Represents an ingredient in a recipe.
    /// </summary>
    public class IngredientModel
    {
        // Id that uniquely identifies the ingredient
        [JsonPropertyAttribute("Id")]
        public int IngredientId { get; set; }
        // Name of the ingredient.
        public string Name { get; set; }
        // Quantity of the ingredient.
        public decimal Quantity { get; set; }
        // Unit of measure of the ingredient.
        public string UnitOfMeasure { get; set; }
        // Cost of the ingredient.
        public decimal Cost { get; set; }
        // Servings 
        public int Servings { get; set; }
    }
}