using System;
using System.ComponentModel.DataAnnotations;

namespace ShareBook.Api.ViewModels
{
    public class RequestBookVM
    {
        [Required]
        public Guid BookId { get; set; }

        public string Reason { get; set; }
    }
}