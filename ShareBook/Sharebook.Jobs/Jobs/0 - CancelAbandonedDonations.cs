﻿using Microsoft.Extensions.Configuration;
using ShareBook.Domain;
using ShareBook.Domain.DTOs;
using ShareBook.Domain.Enums;
using ShareBook.Repository;
using ShareBook.Service;
using System;
using System.Linq;
using System.Text;

namespace Sharebook.Jobs
{
    public class CancelAbandonedDonations : GenericJob, IJob
    {
        private readonly IBookService _bookService;
        private readonly IBookUserService _bookUserService;
        private readonly int _maxLateDonationDaysAutoCancel;

        public CancelAbandonedDonations(IJobHistoryRepository jobHistoryRepo, IBookService bookService, IBookUserService bookUserService, IConfiguration configuration) : base(jobHistoryRepo)
        {
            JobName = "CancelAbandonedDonations";
            Description = "Cancela as doações abandonadas.";
            Interval = Interval.Dayly;
            Active = true;
            BestTimeToExecute = new TimeSpan(6, 0, 0);

            _bookService = bookService;
            _bookUserService = bookUserService;

            _maxLateDonationDaysAutoCancel = int.Parse(configuration["SharebookSettings:MaxLateDonationDaysAutoCancel"]);
        }

        public override JobHistory Work()
        {
            var booksLate = _bookService.GetBooksChooseDateIsLate();

            var refDate = DateTime.Today.AddDays(_maxLateDonationDaysAutoCancel * -1);
            var booksAbandoned = booksLate.Where(b => b.ChooseDate < refDate).ToList();

            var stringBuilder = new StringBuilder();

            stringBuilder.Append($"Encontradas {booksAbandoned.Count} doações abandonadas com mais de {_maxLateDonationDaysAutoCancel} dias de atraso.\n\n");

            foreach (var book in booksAbandoned)
            {
                var dto = new BookCancelationDto
                {
                    Book = book,
                    CanceledBy = "ShareBot",
                    Reason = $"Cancelamento automático de doação abandonada. Com mais de {_maxLateDonationDaysAutoCancel} dias de atraso.",
                };

                _bookUserService.Cancel(dto);
                stringBuilder.Append($"Doação do livro {book.Title} foi cancelada.\n");
            }

            var details = stringBuilder.ToString();

            return new JobHistory()
            {
                JobName = JobName,
                IsSuccess = true,
                Details = details
            };
        }
    }
}