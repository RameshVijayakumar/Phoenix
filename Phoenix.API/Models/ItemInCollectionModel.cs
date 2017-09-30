using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Phoenix.API.Models
{
    /// <summary>
    /// Represents an item that is present in a collection such as Modifiers, Substitutues or Cross-sell
    /// </summary>
    public class ItemInCollectionModelV1V2 : ItemModelV1V2
    {
        // Indicates if this is a modifier item.
        //MOVED TO ItemBase
        //public bool IsModifier { get; set; }   
    }

    public class ItemInCollectionModelV3 : ItemModelV3
    {
        // Indicates if this is a modifier item.
        //MOVED TO ItemBase
        //public bool IsModifier { get; set; }      
    }
}