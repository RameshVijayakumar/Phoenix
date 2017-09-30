using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace Phoenix.Web.Models
{
    public class TagModel
    {
        public int TagId { get; set; }
        //[RegularExpression(@"/^\S*$/",ErrorMessage = "No Spaces allowed")]
        //[RegularExpression("^[a-zA-Z][a-zA-Z0-9.,$;]+$", ErrorMessage = "First character must be a alphabet and Character must be either a alphabet(a-z or A-Z) or digit(0-9) or a dash(-)")]
        [RegularExpression("^[A-Za-z][A-Za-z0-9-]*$", ErrorMessage = "You can use letters, numbers and dashes. It must begin with a letter.")]
        [StringLength(64,ErrorMessage="Max length is 64")]
        [MinLength(2,ErrorMessage="Please enter atleast 2 characters")]
        public string TagName { get; set; }

        public string TagKey { get; set; }

        public int NetworkObjectId { get; set; }

        public bool HasAssociatedData { get; set; }

        public List<AssetTagModel> AssetTagLinks { get; set; }

        //This indicates where this tag was created at this level or it is just inherited from parents.
        public bool IsInheritedTag { get; set; }

        public bool IsActive { get; set; }
    }
}