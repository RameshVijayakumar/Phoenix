using System;
using System.Web.Mvc;
using System.ComponentModel;
using System.Linq;
using System.Collections;

namespace Phoenix.Web.Models
{
    public class ModelStateWrapper : IValidationDictionary
    {
        private ModelStateDictionary _modelState;

        public ModelStateDictionary ModelState
        {
            get { return _modelState; }
            set { _modelState = value; }
        }

        public ModelStateWrapper(ModelStateDictionary modelState)
        {
            _modelState = modelState;
        }

        #region IValidationDictionary Members

        public void AddError(string key, string errorMessage)
        {

            _modelState.AddModelError(key, errorMessage);
        }

        public bool IsValid
        {

            get
            { return _modelState.IsValid; }
        }

        #endregion
    }


    public interface IValidationDictionary
    {
        void AddError(string key, string errorMessage);
        bool IsValid { get; }

    }

    /// <summary>
    /// Helper class to implement extention methods
    /// </summary>
    public static class ModelStateHelper
    {
        /// <summary>
        /// Extension method to create Json object from ModelState 
        /// </summary>
        /// <param name="state">ModelState object</param>
        /// <returns>Jason object</returns>
        public static JsonResult ValidationErrors(this ModelStateDictionary state)
        {
            return new JsonResult
            {
                Data = new
                {
                    Tag = "ValidationError",
                    State = from e in state
                            where e.Value.Errors.Count > 0
                            select new
                            {
                                Name = e.Key,
                                Errors = e.Value.Errors.Select(x => x.ErrorMessage)
                                   .Concat(e.Value.Errors.Where(x => x.Exception != null).Select(x => x.Exception.Message))
                            }
                },
                JsonRequestBehavior = JsonRequestBehavior.AllowGet
            };
        }

        public static string ValidationErrorMessages(this ModelStateDictionary state)
        {
            return string.Join(", ", state.Values
                                          .SelectMany(x => x.Errors)
                                          .Select(x => x.ErrorMessage));
        }

    }
}