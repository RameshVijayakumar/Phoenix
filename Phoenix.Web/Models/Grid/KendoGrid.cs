using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

using System.Globalization;

namespace Phoenix.Web.Models.Grid
{
    public class KendoGrid<T>
    {
        public IEnumerable<T> data { get; set; }
        public int total { get; set; }


        public string GetSorting(KendoGridRequest request)
        {
            var expression = "";

            foreach (var sortObject in request.SortObjects)
            {
                expression += sortObject.Field + " " + sortObject.Direction + ", ";
            }

            if (expression.Length < 2)
                return "true";

            expression = expression.Substring(0, expression.Length - 2);

            return expression;
        }

        public string GetFiltering(KendoGridRequest request)
        {
            var finalExpression = "";

            foreach (var filterObject in request.FilterObjectWrapper.FilterObjects)
            {
                if (finalExpression.Length > 0)
                    finalExpression += " " + request.FilterObjectWrapper.LogicToken + " ";


                if (filterObject.IsConjugate)
                {
                    var expression1 = GetExpression(filterObject.Field1, filterObject.Operator1, filterObject.Value1);
                    var expression2 = GetExpression(filterObject.Field2, filterObject.Operator2, filterObject.Value2);
                    var combined = string.Format("({0} {1} {2})", expression1, request.FilterObjectWrapper.LogicToken, expression2);
                    finalExpression += combined;
                }
                else
                {
                    var expression = GetExpression(filterObject.Field1, filterObject.Operator1, filterObject.Value1);
                    finalExpression += expression;
                }
            }

            if (finalExpression.Length == 0)
                return "true";

            return finalExpression;
        }

        private static string GetExpression(string field, string op, string param)
        {
            string caseMod = "";
            var p = typeof(T).GetProperty(field);

            // Loop through Inner Fields to get the type of Property
            var currentType = typeof(T);
            foreach (string propertyName in field.Split('.'))
            {
                p = currentType.GetProperty(propertyName);
                currentType = p.PropertyType;
            }

            var dataType = (p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>)) ?
                p.PropertyType.GetGenericArguments()[0].Name.ToLower() : p.PropertyType.Name.ToLower(); ;


            if (dataType == "guid")
            {
                param = @"Guid(""" + param.ToLower() + @""")";
            }

            if (dataType == "string" && string.IsNullOrWhiteSpace(param) == false)
            {
                param = @"""" + param.ToLower() + @"""";
                caseMod = ".ToLower()";
            }

            if (dataType == "datetime")
            {
                var i = param.IndexOf("GMT");
                if (i > 0)
                {
                    param = param.Remove(i);
                }
                var date = DateTime.Parse(param, new CultureInfo("en-US"));

                var str = string.Format("DateTime({0}, {1}, {2})", date.Year, date.Month, date.Day);
                param = str;
            }

            string exStr;

            switch (op)
            {
                case "eq":
                    if (string.IsNullOrWhiteSpace(param))
                    {
                        exStr = string.Format("{0} == {1}", field, "null");
                    }
                    else if (dataType == "guid")
                    {
                        exStr = string.Format("{0}.Equals({1})", field, param);
                    }
                    else
                    {
                        exStr = string.Format("{0}{2} == {1}", field, param, caseMod);
                    }
                    break;

                case "neq":
                    if (string.IsNullOrWhiteSpace(param))
                    {
                        if (dataType != "string")
                        {
                            exStr = string.Format("{0} != {1}", field, "null");
                        }
                        else
                        {
                            exStr = string.Format("!string.IsNullOrWhiteSpace({0})", field);

                        }
                    }
                    else if (dataType == "guid")
                    {
                        exStr = string.Format("!{0}.Equals({1})", field, param);
                    }
                    else
                    {
                        exStr = string.Format("{0}{2} != {1}", field, param, caseMod);
                    }
                    break;

                case "contains":
                    exStr = string.Format("{0}{2}.Contains({1})", field, param, caseMod);
                    break;

                case "doesnotcontain":
                    exStr = string.Format("!{0}{2}.Contains({1})", field, param, caseMod);
                    break;
                case "startswith":
                    exStr = string.Format("{0}{2}.StartsWith({1})", field, param, caseMod);
                    break;

                case "endswith":
                    exStr = string.Format("{0}{2}.EndsWith({1})", field, param, caseMod);
                    break;
                case "gte":
                    exStr = string.Format("{0}{2} >= {1}", field, param, caseMod);
                    break;
                case "gt":
                    exStr = string.Format("{0}{2} > {1}", field, param, caseMod);
                    break;
                case "lte":
                    exStr = string.Format("{0}{2} <= {1}", field, param, caseMod);
                    break;
                case "lt":
                    exStr = string.Format("{0}{2} < {1}", field, param, caseMod);
                    break;
                default:
                    exStr = "";
                    break;
            }

            return exStr;
        }
    }
}
