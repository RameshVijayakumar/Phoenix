//------------------------------------------------------------------------------
// <auto-generated>
//    This code was generated from a template.
//
//    Manual changes to this file may cause unexpected behavior in your application.
//    Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Phoenix.DataAccess
{
    using System;
    using System.Collections.Generic;
    
    public partial class ODSIngredient
    {
        public int IngredientId { get; set; }
        public int RecipeId { get; set; }
        public string Name { get; set; }
        public Nullable<System.Guid> StarChefGUID { get; set; }
        public decimal Cost { get; set; }
        public decimal Quantity { get; set; }
        public Nullable<int> UnitOfMeasureId { get; set; }
    
        public virtual ODSRecipe ODSRecipe { get; set; }
        public virtual ODSUnitOfMeasure ODSUnitOfMeasure { get; set; }
    }
}