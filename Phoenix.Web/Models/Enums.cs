using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Phoenix.Web.Models
{
    public enum MenuType
    {
        Menu = 1,
        Category = 2,
        Item = 3,
        ItemCollection = 4,
        ItemCollectionItem = 5,
        SubCategory = 6,
        PrependItem = 7
    }

    public enum PLUType
    {
        Base = 1,
        Alternate = 2,
        Generate = 3,
        DoNotGenerate = 4
    }

    public enum AuditLogType
    {
        Menu = 1,
        Category = 2,
        Item = 3,
        ItemCollection = 4,
        ItemCollectionItem = 5,
        Asset = 6,
        Site = 7,
        Group = 8,
        Profile = 9,
        Schedule = 10,
        Other = 11,
        Tag = 12,
        Target = 13,
        SpecialNotice = 14,
        ModifierFlag = 15
    }

    public enum OperationPerformed
    {
        Created,
        Deleted,
        Updated,
        Disabled,
        Other
    }

    public enum EntityType
    {
        Menu,
        Category,
        Item,
        ItemCollection,
        ItemCollectionItem,
        Asset,
        Site,
        Group,
        Profile,
        Schedule,
        Other,
        Tag,
        POSMap,
        Target,
        SpecialNotice,
        User,
        ModifierFlag,
        ScheduleCycle,
        POSItem,
        Channel
    }

    public enum ManageMessageId
    {
        ChangePasswordSuccess,
        SetPasswordSuccess,
        RemoveLoginSuccess,
    }

    public enum TagKeys
    {
        Tag = 1,
        Channel = 2,
    }
    public enum TagEntity
    {
        Asset = 1,
        Menu = 2,
        Target = 3,
    }
}