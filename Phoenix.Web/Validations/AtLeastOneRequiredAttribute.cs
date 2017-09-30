using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;
using System.Reflection;

namespace Phoenix.Web.Validations
{
    public class AtLeastOneRequiredAttribute : ValidationAttribute, IClientValidatable
    {
        private readonly string[] _properties;
        public AtLeastOneRequiredAttribute(params string[] properties)
        {
            _properties = properties;
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (_properties == null || _properties.Length < 1)
            {
                return null;
            }

            foreach (var property in _properties)
            {
                PropertyInfo propertyInfo = null;

                if (property.Contains("_"))
                {
                    propertyInfo = getNestedProperty(property, validationContext.ObjectInstance);
                }
                else
                {
                    propertyInfo = validationContext.ObjectType.GetProperty(property);
                }
                //var propertyInfo = validationContext.ObjectType.GetProperty(property);
                if (propertyInfo == null)
                {
                    return new ValidationResult(string.Format("unknown property {0}", property));
                }

                var propertyValue = propertyInfo.GetValue(validationContext.ObjectInstance, null);
                if (propertyValue is string && !string.IsNullOrEmpty(propertyValue as string))
                {
                    return null;
                }

                if (propertyValue != null)
                {
                    return null;
                }
            }

            return new ValidationResult(FormatErrorMessage(validationContext.DisplayName));
        }

        public IEnumerable<ModelClientValidationRule> GetClientValidationRules(ModelMetadata metadata, ControllerContext context)
        {
            var rule = new ModelClientValidationRule
            {
                ErrorMessage = ErrorMessage,
                ValidationType = "atleastonerequired"
            };
            rule.ValidationParameters["properties"] = string.Join(",", _properties);

            yield return rule;
        }

        private PropertyInfo getNestedProperty(string expression, object initialObject)
        {
            PropertyInfo currentPropertyInfo = null;
            object nestedValue = initialObject;
            var property = expression.Split(new string[] { "_" }, StringSplitOptions.RemoveEmptyEntries);

            if (property.Length > 1)
            {
                var toResolve = property[1].Split(new string[] { "." }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < toResolve.Length; i++)
                {
                    currentPropertyInfo = nestedValue.GetType().GetProperty(toResolve[i]);
                } //for
            } // if
            else
            {
                currentPropertyInfo = nestedValue.GetType().GetProperty(expression);
            }
            return currentPropertyInfo;
        }
    }
}