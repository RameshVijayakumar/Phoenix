using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using Phoenix.Web.Validations;

namespace Phoenix.Web.Models
{
    /// <summary>
    /// Represents the entire menu including id, name, version, categories, items, collections, etc.
    /// </summary>
    public class MenuDataModel
    {

        //Iris Id - Unique Id
        public long IrisId { get; set; }

        // Id that uniquely identifies a menu
        [JsonPropertyAttribute("Id")]
        public int MenuId { get; set; }
        // Menu name.
        [JsonPropertyAttribute("Name")]
        [Required(ErrorMessage = "Please enter the Name")]
        // Internal name for the menu.
        public string InternalName { get; set; }
        // Description for the menu.
        public string Description { get; set; }

        [ScaffoldColumn(false)]
        public int NetworkObjectId { get; set; }
        
        // updated date of the menu.
        [ScaffoldColumn(false)]
        public string LastUpdateDate { get; set; }

        public bool IsEditable { get; set; }

        public bool IsDeletable { get; set; }

        public bool IsMenuOverriden { get; set; }

        public bool IsActionMenuCopy { get; set; }

        public string ChannelListName { get; set; }

        public string ChannelIdList { get; set; }

        public string OperationStatus { get; set; }

        public List<TagModel> Channels { get; set; }

        // List of categories in a menu.
        public List<CategoryModel> Categories { get; set; }
    }

    /// <summary>
    /// Represents a category in the menu.
    /// </summary>
    public class CategoryModel
    {
        public CategoryModel()
        {
            this.ShowPrice = true;
            this.ScheduleDetails = new List<EntitySchSummary>();
            this.Cycles = new List<SchCycleModel>();
        }

        //Iris Id - Unique Id
        public long IrisId { get; set; }

        // Id that uniquely identifies a category.
        [JsonPropertyAttribute("Id")]
        [Display(Name = "Category Id")]
        public int CategoryId { get; set; }
        // Position of the category relative to other categories.
        public int SortOrder { get; set; }
        // Internal name for the category.
        //bug 4738 - Increase Internal Name character limit
        [StringLength(128, ErrorMessage = "Internal Name must be less than {1} characters.")]
        [Required(ErrorMessage = "Please enter the Internal Name")]
        [Display(Name = "Internal Name")]
        public string InternalName { get; set; }
        // User-friendly category name .
        [StringLength(128, ErrorMessage = "Display Name must be less than {1} characters.")]
        [Required(ErrorMessage = "Please enter the Display Name")]
        [Display(Name = "Display Name")]
        public string DisplayName { get; set; }

        [StringLength(64, ErrorMessage = "Deep Link must be less than {1} characters.")]
        [Display(Name = "Deep Link Id")]
        public string DeepLinkId { get; set; }

        // Indicates if it is a featured category.
        [Display(Name = "Featured")]
        public bool IsFeatured { get; set; }
        // Indicates if items in the category should be show with or without prices.
        [Display(Name = "Show Price")]
        public bool ShowPrice { get; set; }
        // Type of the category
        [Display(Name = "Category Type")]
        public int CategoryTypeId { get; set; }
        // Parent Id
        public int? ParentId { get; set; }
        [Display(Name = "End of Order")]
        public bool IsEndOfOrder { get; set; }
        // Indicates the Start date time
        [Display(Name = "Start Date")]
        public DateTime? StartDate { get; set; }
        // Indicates the End date time
        [Display(Name = "End Date")]
        public DateTime? EndDate { get; set; }

        public string MenuName { get; set; }

        public int? OverrideCategoryId { get; set; }
        // List of items within the category.
        public List<ItemModel> Items { get; set; }
        // List of child categories within the category.
        public List<CategoryModel> Categories { get; set; }

        public List<EntitySchSummary> ScheduleDetails { get; set; }

        public List<SchCycleModel> Cycles { get; set; }

        public int MenuId { get; set; }

        public int NetworkObjectId { get; set; }

        public bool IsScheduleModified { get; set; }
    }

    /// <summary>
    /// Represents a collection of items, which can be Modification, Substitution or Cross-sell.
    /// </summary>
    public class CollectionModel
    {
        public CollectionModel()
        {
            this.ShowPrice = true;
            this.ReplacesItem = true;
            this.IsVisibleToGuest = true;
        }

        //Iris Id - Unique Id
        public long IrisId { get; set; }

        // Id that uniquely identifies a collection of items
        [Display(Name = "Collection Id")]
        public int CollectionId { get; set; }
        // Position of the collection relative to other collections.
        public int SortOrder { get; set; }
        // TypeId of collection.
        [Display(Name = "Collection Type")]
        public int CollectionTypeId { get; set; }
        //bug 4738 - Increase Internal Name character limit
        // Internal name for the collection.
        [StringLength(128, ErrorMessage = "Internal Name must be less than {1} characters.")]
        [Required(ErrorMessage = "Please enter the Internal Name")]
        [Display(Name = "Internal Name")]
        public string InternalName { get; set; }
        // User-friendly name of the collection.
        [StringLength(128, ErrorMessage = "Display Name must be less than {1} characters.")]
        [Required(ErrorMessage = "Please enter the Display Name")]
        [Display(Name = "Display Name")]
        public string DisplayName { get; set; }
        // Indicates if items in the collection should be show with or without prices.
        [Display(Name = "Show Price")]
        public bool ShowPrice { get; set; }
        // The minimum number of items in this collection. 
        [Range(0,Int32.MaxValue,ErrorMessage="Minimum Quantity must be valid.")]
        [Display(Name = "Minimum Quantity")]
        public int MinQuantity { get; set; }
        // The maximum number of items in this collection.
        [Range(0, Int32.MaxValue, ErrorMessage = "Maximum Quantity must be valid.")]
        [Display(Name = "Maximum Quantity")]
        public int MaxQuantity { get; set; }
        // Indicates if the collection Is Mandatory.
        [Display(Name = "Mandatory")]
        public bool IsMandatory { get; set; }
        // Indicates if the collection can Propogate.
        [Display(Name = "Propagate")]
        public bool IsPropagate { get; set; }
        // Indicates if the collection can Visible.
        [Display(Name = "Visible To Guest")]
        public bool IsVisibleToGuest { get; set; }

        [Display(Name = "Replaces Item")]
        public bool ReplacesItem { get; set; }

        public int MenuId { get; set; }

        public int NetworkObjectId { get; set; }

        // List of items within the collection.
        public List<ItemModel> Items { get; set; }
    }

    /// <summary>
    /// Represents an item in the menu.
    /// </summary>
    public class ItemModel
    {
        public ItemModel()
        {
            this.ShowPrice = true;
            //New BR Rule : Item is not available by default
            this.IsAvailable = false;
            this.IsEnabled = true;
            this.POSItem = new POSDataModel();
        }

        //Iris Id - Unique Id
        [JsonPropertyAttribute("Id")]
        public long IrisId { get; set; }

        // Id that uniquely identifies the item.
        [JsonIgnore]
        public int ItemId { get; set; }
        // Position of the item relative to other items within the category.
        public int SortOrder { get; set; }
        // Product Lookup Unit number .
        [Display(Name = "PLU")]
        [JsonIgnore]
        public int? BasePLU { get; set; }
        // Internal name for the item.
        //bug 4738 - Increase Internal Name character limit
        [StringLength(128, ErrorMessage = "Internal Name must be less than {1} characters.")]
        [Required(ErrorMessage = "Please enter the Internal Name")]
        [JsonPropertyAttribute("InternalName")]
        [Display(Name = "Internal Name")]
        public string ItemName { get; set; }
        // User-friendly name of the item .
        [StringLength(128, ErrorMessage = "Display Name must be less than {1} characters.")]
        [Required(ErrorMessage = "Please enter the Display Name")]
        [Display(Name = "Display Name")]
        public string DisplayName { get; set; }

        [StringLength(64, ErrorMessage = "POS Name must be less than {1} characters.")]
        //[Required(ErrorMessage = "Please enter the POS Name")]
        [Display(Name = "POS Name")]
        [JsonIgnore]
        public string POSItemName { get; set; }

        [StringLength(64, ErrorMessage = "Deep Link must be less than {1} characters.")]
        [Display(Name = "Link Id")]
        [JsonIgnore]
        public string DeepLinkId { get; set; }
        // User-friendly description of the item.
        [StringLength(512, ErrorMessage = "Description must be less than {1} characters.")]
        [JsonPropertyAttribute("Description")]
        [Display(Name = "Description")]
        [JsonIgnore]
        public string DisplayDescription { get; set; }
        // Indicates if this is a featured item.
        [Display(Name = "Featured")]
        public bool IsFeatured { get; set; }
        // Indicates if this is a modifier item.
        [Display(Name = "Modifier")]
        [JsonIgnore]
        public bool IsModifier { get; set; }
        // Indicates if this is a substitute item.
        [Display(Name = "Substitute")]
        [JsonIgnore]
        public bool IsSubstitute { get; set; }
        // Indicates if this item is included.
        [Display(Name = "Included")]
        public bool IsIncluded { get; set; }
        // Indicates if this item is enabled.
        [Display(Name = "Enabled")]
        [JsonIgnore]
        public bool IsEnabled { get; set; }
        // Indicates if this is a core item.
        [Display(Name = "Core")]
        [JsonIgnore]
        public bool IsCore { get; set; }
        // Indicates if item is available.
        [Display(Name = "Available")]
        public bool IsAvailable { get; set; }
        // Indicates the Start date time
        [Display(Name = "Start Date")]
        public DateTime? StartDate { get; set; }
        // Indicates the End date time
        [Display(Name = "End Date")]
        public DateTime? EndDate { get; set; }

        [Display(Name = "Description")]
        public string SelectedDescription { get; set; }

        [JsonIgnore]
        public int SelectedDescriptionId { get; set; }

        [Display(Name = "Base Price")]
        [JsonIgnore]
        public decimal? BasePrice { get; set; }

        [Display(Name = "Created Date")]
        [JsonIgnore]
        public DateTime CreatedDate { get; set; }

        [Display(Name = "Update Date")]
        [JsonIgnore]
        public DateTime UpdatedDate { get; set; }

        [JsonIgnore]
        public int? OverrideItemId { get; set; }

        [JsonIgnore]
        public int? ParentItemId { get; set; }

        [JsonIgnore]
        public string URL { get; set; }

        [Display(Name = "Show Price")]
        public bool ShowPrice { get; set; }

        [Display(Name = "Override Price")]
        public bool IsPriceOverriden { get; set; }

        [DataType(DataType.Currency)]
        [DisplayFormat(DataFormatString = "{0:N}", ApplyFormatInEditMode = true)]
        public decimal OverridenPrice { get; set; }

        [Display(Name = "Quick Order")]
        public bool QuickOrder { get; set; }

        [Display(Name = "Send Hierarchy")]
        public bool IsSendHierarchy { get; set; }

        [Display(Name = "Top Level")]
        public bool IsTopLevel { get; set; }

        [Display(Name = "Combo")]
        public bool IsCombo { get; set; }

        [Display(Name = "Auto Select")]
        public bool IsAutoSelect { get; set; }

        public int ItemDescriptionId { get; set; }

        [JsonIgnore]
        public int SelectedScheduleId { get; set; }

        [StringLength(14, ErrorMessage = "Kitchen Text must be less than {1} characters.")]
        [Display(Name = "Kitchen Text")]
        public string ButtonText { get; set; }
        
        [Display(Name = "Print on Order")]
        public bool PrintOnOrder { get; set; }

        [Display(Name = "Print on Receipt")]
        public bool PrintOnReceipt { get; set; }

        [Display(Name = "Print Same Line")]
        public bool PrintOnSameLine { get; set; }

        [Display(Name = "Print Recipe")]
        public bool PrintRecipe { get; set; }

        [Display(Name = "Force Recipe")]
        public bool ForceRecipe { get; set; }

        [Display(Name = "Beverage")]
        public bool IsBeverage { get; set; }

        [Display(Name = "Entree Appetizer")]
        public bool IsEntreeApp { get; set; }

        [JsonIgnore]
        public string cDN { get; set; }

        [JsonIgnore]
        public int AdditionalDescCount { get; set; }

        [JsonIgnore]
        public bool IsGeneratePLU { get; set; }

        [JsonIgnore]
        public bool IsZeroPLU { get; set; }

        [JsonIgnore]
        public bool CreateNewAfterSave { get; set; }

        [JsonIgnore]
        [Display(Name = "Alcohol")]
        public bool IsAlcohol { get; set; }

        // bug - 4796 
        //[AtLeastOneRequired("AlternatePLU","BasePLU",ErrorMessage="Please enter either PLU or Alternate ID")]
        [Display(Name = "Alternate ID")]
        [JsonIgnore]
        public string AlternatePLU { get; set; }

        [Display(Name = "Modifier Flag")]
        public int? ModifierFlagId { get; set; }

        [Display(Name = "Cook Time")]
        public int? CookTime { get; set; }

        [Display(Name = "Prep Order")]
        public int? PrepOrderTime { get; set; }

        [Display(Name = "Cost Category")]
        public int? DWItemCategorizationKey { get; set; }

        [Display(Name = "Sub Category")]
        public int? DWItemSubTypeKey { get; set; }

        [Display(Name = "Requested By")]
        [JsonIgnore]
        public string RequestedBy { get; set; }

        [JsonIgnore]
        public bool IsDWFieldsEnabled { get; set; }

        [JsonIgnore]
        public bool IsScheduleModified { get; set; }

        [Display(Name = "POS Item")]
        [JsonIgnore]
        public int SelectedPOSDataId { get; set; }

        [JsonIgnore]
        public int POSDataId { get; set; }

        [JsonIgnore]
        public bool IsDefault { get; set; }

        [JsonIgnore]
        public int? PLU { get; set; }

        [JsonIgnore]
        public string MenuItemName { get; set; }

        [JsonIgnore]
        public int PreviousPOSDataIdSelected { get; set; }

        [JsonIgnore]
        public int Feeds { get; set; }

        [JsonIgnore]
        public string ItemDataHash { get; set; }

        [JsonIgnore]
        public POSDataModel POSItem { get; set; }

        [JsonIgnore]
        public ItemModel MasterItem { get; set; }

        [JsonIgnore]
        public DWItemCategorizationModel DWItemCategorization { get; set; }

        [JsonIgnore]
        public DWItemCategorizationModel DWItemSubType { get; set; }

        [JsonIgnore]
        public MenuType ItemType { get; set; }

        [JsonIgnore]
        // Collections associated with this item.
        public List<CollectionModel> Collections { get; set; }

        [JsonIgnore]
        public List<ItemDescriptionModel> ItemDescriptions { get; set; }

        [JsonIgnore]
        public List<AssetItemModel> Assets { get; set; }

        public List<EntitySchSummary> ScheduleDetails { get; set; }

        [JsonIgnore]
        public List<SchCycleModel> Cycles { get; set; }

        [JsonIgnore]
        public List<POSDataModel> POSDatas { get; set; }

        public int NetworkObjectId { get; set; }

        [JsonIgnore]
        public int? MenuId { get; set; }
        
    }

    public class DWItemCategorizationModel
    {
        public int DWItemCategorizationKey { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int item_cat_id { get; set; }
    }

    public class DWItemSubTypeModel
    {
        public int DWItemSubTypeKey { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int item_sub_type_id { get; set; }
        public int item_type_id { get; set; }
    }

    public class MenuDropdown
    {
        public int Order { get; set; }
        public int MenuId { get; set; }
        public string Name { get; set; }
    }
    public class Dropdown
    {
        public int Id { get; set; }
        public int Value { get; set; }
        public string Text { get; set; }
    }

    public class PLUItemModel
    {
        public int ItemId { get; set; }
        public int PLUItemId { get; set; }
        [Required(ErrorMessage = "Please enter the PLU")]
        public int PLU { get; set; }
        public bool IsActive { get; set; }
    }

    public class ItemDescriptionModel
    {
        public int ItemId { get; set; }
        public int ItemDescriptionId { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }
        public bool ToDelete { get; set; }
    }

    public class CleanUpDataModel
    {
        public CleanUpDataModel()
        {
            CategoryIds = new List<int>();
            ItemIds = new List<int>();
            CollectionIds = new List<int>();
        }

        public List<int> CategoryIds { get; set; }
        public List<int> ItemIds { get; set; }
        public List<int> CollectionIds { get; set; }
    }


    public class ModifierFlagModel
    {
        public int ModifierFlagId { get; set; }
        [Required(ErrorMessage = "Code is Required")]
        public int Code { get; set; }
        [StringLength(32,ErrorMessage="Name must be less than {1} characters.")]
        [Required(ErrorMessage="Name is Required")]
        public string Name { get; set; }
        public int NetworkObjectId { get; set; }
    }

    public class CheckedItemModel
    {
        public int Id { get; set; }
        public int POSDataId { get; set; }
        public bool IsDefault { get; set; }
    }

}