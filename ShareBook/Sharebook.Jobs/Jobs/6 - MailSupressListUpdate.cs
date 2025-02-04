﻿using ShareBook.Domain;
using ShareBook.Domain.Enums;
using ShareBook.Repository;
using ShareBook.Service;
using System;

namespace Sharebook.Jobs;

public class MailSupressListUpdate : GenericJob, IJob
{
    private readonly IEmailService _emailService;

    public MailSupressListUpdate(
        IJobHistoryRepository jobHistoryRepo,
        IEmailService emailService) : base(jobHistoryRepo)
    {
        JobName = "MailSupressListUpdate";
        Description = @"Atualiza a lista de emails suprimidos. Essa lista serve para manter boa reputação do nosso
                        mailling. Além de ser um requisito da AWS.";
        Interval = Interval.Dayly;
        Active = true;
        BestTimeToExecute = new TimeSpan(2, 0, 0);

        _emailService = emailService;
    }

    public override JobHistory Work()
    {
        var log = _emailService.ProcessBounceMessages().Result;

        return new JobHistory()
        {
            JobName = JobName,
            IsSuccess = true,
            Details = String.Join("\n", log)
        };
    }
}