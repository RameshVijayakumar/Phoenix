using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using Phoenix.API.Models;
using System.Net;
using System.Net.Http;

namespace Phoenix.API.Controllers
{ 
    public class RecipesController : ApiController
    {
        private IRecipeService _recipeService;

        public RecipesController()
        {
            //TODO: inject this interface
            _recipeService = new RecipeService();
        }

        // GET api/recipes/menu/1
        /// <summary>
        /// Gets the recipe for every item in a particular menu.
        /// </summary>
        /// <param name="menuId">Menu Id for which the recipes need to be fetched.</param>
        /// <returns>FullRecipeModel object</returns>
        //[HttpGet]
        //[ActionName("Menu")]
        //public FullRecipeModel GetRecipesForMenu(int menuId)
        //{
        //    return _recipeService.GetRecipe(menuId);
        //}

        [Authorize]
        public FullRecipeModel Get(string id)
        {
            HttpStatusCode status = HttpStatusCode.OK;
            var model = _recipeService.GetRecipe(id, out status);

            if (status != HttpStatusCode.OK)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(status, _recipeService.LastActionResult));
            }
            return model;
        }

    }
}
