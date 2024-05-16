using System;
using System.ComponentModel.DataAnnotations;

namespace ShareBook.Api.ViewModels
{
    public class DonateBookUserVM
    {
        [Required]
        public Guid UserId { get; set; }

        public string Note { get; set; }
    }
}