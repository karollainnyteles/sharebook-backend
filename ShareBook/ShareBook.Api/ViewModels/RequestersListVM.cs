using ShareBook.Domain.Enums;
using System;

namespace ShareBook.Api.ViewModels
{
    public class RequestersListVM
    {
        public Guid UserId { get; set; }

        public string RequesterNickName { get; set; }

        public string Location { get; set; }

        public int TotalBooksWon { get; set; }

        public int TotalBooksDonated { get; set; }

        public string RequestText { get; set; }

        public DonationStatus Status { get; set; }
    }
}