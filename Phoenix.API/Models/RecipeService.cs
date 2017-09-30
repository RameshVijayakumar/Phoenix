using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.Entity;
using System.Data;
using Phoenix.DataAccess;
using Phoenix.Common;
using Omu.ValueInjecter;
using Phoenix.API;
using System.Net;

namespace Phoenix.API.Models
{
    /// <summary>
    /// Class where business layer for RecipesController resides.
    /// </summary>
    public class RecipeService : IRecipeService
    {
        private IRepository _repository;
        private DbContext _context;
        private string _lastActionResult;
        public string LastActionResult
        {
            get { return _lastActionResult; }
        }

        /// <summary>
        /// Constructor where DBContext object and Repository object gets initialised.
        /// </summary>
        public RecipeService()
        {
            //TODO: inject these interfaces
            _context = new ProductMasterContext();
            _repository = new GenericRepository(_context);
        }

        /// <summary>
        /// Gets recipe for items in the menu
        /// </summary>
        /// <param name="id">Id of the menu for which recipe of items need to be fetched.</param>
        /// <returns>FullRecipeModel object</returns>
        public FullRecipeModel GetRecipe(string siteId, out HttpStatusCode status)
        {
            FullRecipeModel retModel = new FullRecipeModel();
            status = HttpStatusCode.OK;
            try
            {
                Guid siteGuid;
                if (Guid.TryParse(siteId, out siteGuid))
                {
                    // get menu id for given site-id
                    var siteInfo = _repository.GetQuery<SiteInfo>(s => s.SiteId == siteGuid)
                        .Include("NetworkObject")
                        .Include("NetworkObject.Menus") ///TODO - NewSchema changes Need Review
                        .FirstOrDefault();

                    if (siteInfo != null)
                    {
                        ///TODO - NewSchema changes Need Review
                        // get the first menu
                        var mnu = siteInfo.NetworkObject.Menus.FirstOrDefault();
                        if (mnu != null)
                        {

                            var menu = _repository.GetQuery<Menu>(m => m.MenuId == mnu.MenuId)
                                        .Include("Categories")
                                        .Include("Categories.CategoryObjects")
                                        .Include("Categories.CategoryObjects.Item.Recipe")
                                        .Include("Categories.CategoryObjects.Item.Recipe.Ingredients")
                                        .Include("Categories.CategoryObjects.Item.Recipe.UnitOfMeasure")
                                        .Include("Categories.CategoryObjects.Item.Recipe.RecipeChildren")
                                        .Include("Categories.CategoryObjects.Item.Recipe.RecipeChildren.RecipeChildren")
                                        .FirstOrDefault();

                            if (null != menu)
                            {
                                List<RecipeForItemModel> itemList = new List<RecipeForItemModel>();
                                ///TODO - NewSchema changes Need Review
                                // Get all categories in the menu.
                                var categoryMenuLinks = _repository.GetQuery<CategoryMenuLink>(c => c.MenuId == menu.MenuId).Select( c=> c.CategoryId).ToList();
                                var categories = _repository.GetQuery<Category>(x => categoryMenuLinks.Contains(x.CategoryId));
                                foreach (var category in categories)
                                {
                                    // Get the item links in the category.
                                    var categoryObjects = category.CategoryObjects;
                                    foreach (var categoryObject in categoryObjects)
                                    {
                                        var recipe = categoryObject.Item.Recipe;

                                        if (null != recipe)
                                        {
                                            RecipeForItemModel item = new RecipeForItemModel();

                                            // Inject the values from the source object into target object for matching column names via ValueInject.
                                            item.InjectFrom(categoryObject.Item);

                                            RecipeModel recipeModel = new RecipeModel();

                                            // Get the recipe
                                            recipeModel.InjectFrom(recipe);
                                            recipeModel.UnitOfMeasure = recipe.UnitOfMeasure == null ? null : recipe.UnitOfMeasure.UOfMeasure;

                                            getSubRecipe(recipe, recipeModel);

                                            /*
                                            // Get the Ingredient List
                                            recipeModel.Ingredients = getIngredientList(recipe);

                                            //Get the Linked Recipes
                                            List<RecipeModel> subRecipeList = null;

                                            var linkedRecipes = recipe.RecipeChildren;
                                            if (linkedRecipes.Count > 0)
                                            {
                                                subRecipeList = new List<RecipeModel>();
                                                foreach (var linkedRecipe in linkedRecipes)
                                                {
                                                    RecipeModel subRecipe = new RecipeModel();
                                                    //subRecipe.InjectFrom(linkedRecipe.Recipe);
                                                    subRecipe.InjectFrom(linkedRecipe);
                                                    //ValuenInjector did not inject values for servings and cost. Hence doing it explicitly below
                                                    subRecipe.Servings = linkedRecipe.Servings;

                                                    subRecipe.Cost = linkedRecipe.Cost; 

                                                    //Get the ingredient List for each linked recipe.
                                                    subRecipe.Ingredients = getIngredientList(linkedRecipe);
                                                    subRecipeList.Add(subRecipe);
                                                }
                                            }
                                            recipeModel.Recipes = subRecipeList;
                                            */

                                            item.Recipe = recipeModel;
                                            itemList.Add(item);
                                        }
                                    }
                                }
                                retModel.Items = itemList;
                            }
                        }
                        else
                        {
                            status = HttpStatusCode.NotFound;
                            _lastActionResult = string.Format("No menu found for site {0} {1}, id {2}",
                                siteInfo.NetworkObject.Name, siteInfo.StoreNumber, siteInfo.NetworkObject.IrisId);
                        }

                    }
                    else
                    {
                        status = HttpStatusCode.NotFound;
                        _lastActionResult = "Invalid site id";
                    }
                }
                else
                {
                    status = HttpStatusCode.NotFound;
                    _lastActionResult = "The site id must be a valid GUID";
                }

            }
            catch (Exception e)
            {
                status = HttpStatusCode.InternalServerError;
                _lastActionResult = "Internal server error";

                //log exception
                Logger.WriteError(e);
            }
            return retModel;
        }

        private void getSubRecipe(Recipe recipeEntity, RecipeModel recipeModel)
        {
            if (recipeEntity != null)
            {
                foreach (var sr in recipeEntity.RecipeChildren)
                {
                    RecipeModel subRecipe = new RecipeModel();
                    subRecipe.InjectFrom(sr);
                    subRecipe.UnitOfMeasure = sr.UnitOfMeasure == null ? null : sr.UnitOfMeasure.UOfMeasure;

                    if (recipeModel.Recipes == null)
                    {
                        recipeModel.Recipes = new List<RecipeModel>();
                    }
                    recipeModel.Recipes.Add(subRecipe);

                    // resurive call to subrecipes
                    getSubRecipe(sr, subRecipe);
                }

                foreach (var i in recipeEntity.Ingredients)
                {
                    IngredientModel ing = new IngredientModel();
                    ing.InjectFrom(i);
                    ing.UnitOfMeasure = i.UnitOfMeasure == null ? null : i.UnitOfMeasure.UOfMeasure;

                    if (recipeModel.Ingredients == null)
                    {
                        recipeModel.Ingredients = new List<IngredientModel>();
                    }

                    recipeModel.Ingredients.Add(ing);
                }
            }
        }
        /// <summary>
        /// Gets the ingredient list in the recipe
        /// </summary>
        /// <param name="recipe">Recipe for which ingredients need to be fetched.</param>
        /// <returns>List of IngredientModel objects.</returns>
        private List<IngredientModel> getIngredientList(Recipe recipe)
        {
            List<IngredientModel> ingredientModelList = null;
            if(null != recipe)
            {

                if (recipe.Ingredients.Count > 0)
                {
                    ingredientModelList = new List<IngredientModel>();
                    foreach (var ingredient in recipe.Ingredients)
                    {
                        IngredientModel ingredientModel = new IngredientModel();
                        // Inject the values from the source object into target object for matching column names via ValueInject.
                        ingredientModel.InjectFrom(ingredient);

                        ingredientModel.UnitOfMeasure = ingredient.UnitOfMeasure == null ? string.Empty : ingredient.UnitOfMeasure.UOfMeasure;

                        ingredientModelList.Add(ingredientModel);
                    }

                }
            }
            return ingredientModelList;
        }
    }

    /// <summary>
    /// Interface class that RecipeService class implements
    /// </summary>
    public interface IRecipeService
    {
        string LastActionResult { get; }
        FullRecipeModel GetRecipe(string siteId, out HttpStatusCode status);
    }
}