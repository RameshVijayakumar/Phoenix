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
    
    public partial class SpecialNotice
    {
        public SpecialNotice()
        {
            this.SpecialNoticeMenuLinks = new HashSet<SpecialNoticeMenuLink>();
        }
    
        public int NoticeId { get; set; }
        public string NoticeName { get; set; }
        public string NoticeText { get; set; }
        public Nullable<System.DateTime> LastUpdatedDate { get; set; }
        public int NetworkObjectId { get; set; }
        public bool DefaultIncludeInMenu { get; set; }
    
        public virtual ICollection<SpecialNoticeMenuLink> SpecialNoticeMenuLinks { get; set; }
        public virtual NetworkObject NetworkObject { get; set; }
    }
}