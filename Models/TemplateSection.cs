using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaskManagementApp.Models
{
    public class TemplateSection
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Title { get; set; }

        public string Description { get; set; }

        public int Order { get; set; }

        public int ProjectTemplateId { get; set; }
        [ForeignKey("ProjectTemplateId")]
        public virtual ProjectTemplate ProjectTemplate { get; set; }

        public int? ParentSectionId { get; set; }
        [ForeignKey("ParentSectionId")]
        public virtual TemplateSection ParentSection { get; set; }

        public virtual ICollection<TemplateSection> ChildSections { get; set; } = new List<TemplateSection>();
    }
}