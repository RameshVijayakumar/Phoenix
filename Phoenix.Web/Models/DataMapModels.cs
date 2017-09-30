using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Phoenix.DataAccess;
using System.ComponentModel.DataAnnotations;
using Phoenix.Web.Validations;

namespace Phoenix.Web.Models
{
    public class MenuItemModel
    {
        public int Id { get; set; }
        public int ParentId { get; set; }
        public string ParentName { get; set; }
        public Guid DataId { get; set; }
        public string ItemName { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public string MapStatus { get; set; }
        public bool IsAvailable { get; set; }

    }
    public class MenuItemModelPOS : MenuItemModel
    {
        public string MappedPLU { get; set; }
        public string MappedPOSItemName { get; set; }
        public int MappedPOSDataId { get; set; }
        public bool IsOverride { get; set; }
        public bool IsODSAvailable { get; set; }
        public OperationalPOSDataModel ODSData { get; set; }
        public List<Dropdown> POSDataList { get; set; }
    }

    public class OperationalPOSDataModel
    {
        public int POSDataId { get; set; }
        public string ScreenGroupName { get; set; }
        public int ScreenPos { get; set; }
        public int PLU { get; set; }
        public string ItemName { get; set; }
        public bool IsModifier { get; set; }
        public bool IsSold { get; set; }
        public decimal BasePrice { get; set; }
        public string TaxTypeIds { get; set; }
        public DateTime InsertedDate { get; set; }
    }

    public class MenuItemModelRecipe : MenuItemModel
    {
        public string RecipeName { get; set; }
        public int RecipeId { get; set; }
    }
    public class OperationalRecipeDataModel
    {
        public string Name { get; set; }
        public int RecipeId { get; set; }
        public int Servings { get; set; }
        public string StarChefGuid { get; set; }
        public decimal Cost { get; set; }
        public DateTime? LastUpdated { get; set; }
    }



    public class POSAdminModel
    {
        public POSAdminModel()
        {
            POSItem = new POSDataModel();
        }
        public POSDataModel POSItem { get; set; }

    }

    public class POSDataModel
    {
        public int ItemPOSDataLinkId { get; set; }
        public int ItemId { get; set; }
        public int POSDataId { get; set; }

        [AtLeastOneRequired("POSItem_PLU", "POSItem_AlternatePLU", ErrorMessage = "Please enter either PLU or Alternate ID")]
        public int? PLU { get; set; }

        [Display(Name = "Alternate ID")]
        public string AlternatePLU { get; set; }

        [Display(Name = "Price")]
        [DisplayFormat(DataFormatString = "{0:c}", ApplyFormatInEditMode = true)]
        public decimal? BasePrice { get; set; }

        [Display(Name = "POS Name")]
        [Required(ErrorMessage = "Please enter the Name")]
        public string POSItemName { get; set; }

        [Display(Name = "Menu Item Name")]
        public string MenuItemName { get; set; }

        [Display(Name = "Kitchen Text")]
        public string ButtonText { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime UpdatedDate { get; set; }

        public bool IsDefault { get; set; }
        public bool ToRemove { get; set; }
        public bool ToDelete { get; set; }
        public bool IsAlcohol { get; set; }
        public bool IsModifier { get; set; }
        public int NetworkObjectId { get; set; }
    }
}