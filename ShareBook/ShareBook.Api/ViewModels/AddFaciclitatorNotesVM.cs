using System;
using System.ComponentModel.DataAnnotations;

namespace ShareBook.Api.ViewModels
{
    public class AddFacilitatorNotesVM
    {
        [Required]
        public Guid BookId { get; set; }

        public string FacilitatorNotes { get; set; }
    }
}