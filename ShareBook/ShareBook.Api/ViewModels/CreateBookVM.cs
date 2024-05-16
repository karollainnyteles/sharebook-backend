using ShareBook.Domain.Enums;
using System;
using System.ComponentModel.DataAnnotations;

namespace ShareBook.Api.ViewModels
{
    public class CreateBookVM : BaseViewModel
    {
        public string Title { get; set; }

        public string Author { get; set; }

        [Required]
        public Guid CategoryId { get; set; }

        public string ImageName { get; set; }

        public byte[] ImageBytes { get; set; }

        [Required]
        public FreightOption FreightOption { get; set; }

        public string Synopsis { get; set; }
        public string Type { get; set; }
        public string EBookDownloadLink { get; set; }
        public string EBookPdfFile { get; set; }
    }
}