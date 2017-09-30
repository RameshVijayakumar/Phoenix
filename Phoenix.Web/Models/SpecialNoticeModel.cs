using System.ComponentModel.DataAnnotations;

namespace Phoenix.Web.Models
{
    public class SpecialNoticeModel
    {
        public SpecialNoticeModel()
        {
            DefaultIncludeInMenu = true;
        }
        public int NoticeId { get; set; }

        [Required(ErrorMessage = "Notice Name is required")]
        public string NoticeName { get; set; }

        [Required(ErrorMessage = "Notice Text is required")]
        public string NoticeText { get; set; }     
        
        public int NetworkObjectId { get; set; }       

        public string LastUpdated { get; set; }

        public bool DefaultIncludeInMenu { get; set; }

        public bool IsLinkedToMenu { get; set; }
    }  
}


