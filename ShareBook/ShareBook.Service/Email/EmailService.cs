using MailKit;
using MailKit.Net.Imap;
using MailKit.Net.Smtp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using MimeKit;
using ShareBook.Domain;
using ShareBook.Domain.Enums;
using ShareBook.Repository;
using ShareBook.Service.AwsSqs;
using ShareBook.Service.AwsSqs.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Security;
using System.Threading.Tasks;

namespace ShareBook.Service;

public class EmailService : IEmailService
{
    private readonly EmailSettings _settings;
    private readonly IUserRepository _userRepository;
    private readonly IConfiguration _configuration;
    private readonly MailSenderLowPriorityQueue _mailSenderLowPriorityQueue;
    private readonly MailSenderHighPriorityQueue _mailSenderHighPriorityQueue;
    private readonly ImapClient _imapClient;

    private readonly ApplicationDbContext _ctx;

    public EmailService(IOptions<EmailSettings> emailSettings, IUserRepository userRepository,
    IConfiguration configuration, MailSenderLowPriorityQueue mailSenderLowPriorityQueue,
    MailSenderHighPriorityQueue mailSenderHighPriorityQueue, ApplicationDbContext ctx)
    {
        _settings = emailSettings.Value;
        _userRepository = userRepository;
        _configuration = configuration;
        _mailSenderLowPriorityQueue = mailSenderLowPriorityQueue;
        _mailSenderHighPriorityQueue = mailSenderHighPriorityQueue;

        _imapClient = new ImapClient();
        _imapClient.CheckCertificateRevocation = false;

#if DEBUG
        _imapClient.ServerCertificateValidationCallback = (s, c, h, e) => true;
#endif
        _ctx = ctx;
    }

    public async Task SendToAdmins(string messageText, string subject)
    {
        var firstAdm = _userRepository.Get().FirstOrDefault(u => u.Profile == Profile.Administrator);
        await Send(firstAdm?.Email, firstAdm?.Name, messageText, subject, copyAdmins: true, highPriority: true);
    }

    public async Task Send(string emailRecipient, string nameRecipient, string messageText, string subject)
        => await Send(emailRecipient, nameRecipient, messageText, subject, copyAdmins: false, highPriority: true);

    public async Task Send(string emailRecipient, string nameRecipient, string messageText, string subject, bool copyAdmins, bool highPriority)
    {
        var sqsEnabled = bool.Parse(_configuration["AwsSqsSettings:IsActive"]);

        if (!sqsEnabled)
        {
            await SendSmtp(emailRecipient, nameRecipient, messageText, subject, copyAdmins);
            return;
        }

        var queueMessage = new MailSenderbody
        {
            CopyAdmins = copyAdmins,
            Subject = subject,
            BodyHTML = messageText,
            Destinations = new List<Destination>{
                {
                    new Destination {
                        Name = nameRecipient,
                        Email = emailRecipient
                    }
                }
            }
        };

        if (highPriority)
            await _mailSenderHighPriorityQueue.SendMessage(queueMessage);
        else
            await _mailSenderLowPriorityQueue.SendMessage(queueMessage);
    }

    public async Task SendSmtp(string emailRecipient, string nameRecipient, string messageText, string subject, bool copyAdmins)
    {
        var message = FormatEmail(emailRecipient, nameRecipient, messageText, subject, copyAdmins);

        var client = new SmtpClient();

        if (_settings.UseSSL)
            client.ServerCertificateValidationCallback = (s, c, h, e) =>
            {
#if DEBUG
                return true;
#endif
                return e == SslPolicyErrors.None;
            };

        client.CheckCertificateRevocation = false;
        await client.ConnectAsync(_settings.HostName, _settings.Port, _settings.UseSSL);
        await client.AuthenticateAsync(_settings.Username, _settings.Password);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }

    private MimeMessage FormatEmail(string emailRecipient, string nameRecipient, string messageText, string subject, bool copyAdmins)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("Sharebook", "contato@sharebook.com.br"));
        message.To.Add(new MailboxAddress(nameRecipient, emailRecipient));

        if (copyAdmins)
        {
            var adminsEmails = FormatEmailGetAdminEmails();
            message.Cc.AddRange(adminsEmails);
        }

        message.Subject = subject;
        message.Body = new TextPart("HTML")
        {
            Text = messageText
        };
        return message;
    }

    private InternetAddressList FormatEmailGetAdminEmails()
    {
        var admins = _userRepository.Get()
            .Select(u => new User
            {
                Email = u.Email,
                Profile = u.Profile
            }
            )
            .Where(u => u.Profile == Domain.Enums.Profile.Administrator)
            .ToList();

        InternetAddressList list = new InternetAddressList();
        foreach (var admin in admins)
        {
            list.Add(new MailboxAddress(admin.Email));
        }

        return list;
    }

    public async Task Test(string email, string name)
    {
        var subject = "Sharebook - teste de email";
        var message = $"<p>Olá {name},</p> <p>Esse é um email de teste para verificar se o sharebook consegue fazer contato com você. Por favor avise o facilitador quando esse email chegar. Obrigado.</p>";
        await this.SendSmtp(email, name, message, subject, copyAdmins: false);
    }

    public async Task<IList<string>> ProcessBounceMessages()
    {
        var log = new List<string>();

        if (string.IsNullOrEmpty(_settings.BounceFolder))
        {
            log.Add("Não foi possível processar os emails bounce porque o 'BounceFolder' não está configurado.");
            return log;
        }

        await _imapClient.ConnectAsync(_settings.HostName, _settings.ImapPort, true);
        await _imapClient.AuthenticateAsync(_settings.Username, _settings.Password);

        var bounceFolder = GetBounceFolder();
        await bounceFolder.OpenAsync(FolderAccess.ReadWrite);

        var MAX_EMAILS_TO_PROCESS = 50;
        var items = await bounceFolder.FetchAsync(0, MAX_EMAILS_TO_PROCESS, MessageSummaryItems.UniqueId | MessageSummaryItems.Size | MessageSummaryItems.Flags);

        foreach (var uniqueId in items.Select(item => item.UniqueId))
        {
            var message = await bounceFolder.GetMessageAsync(uniqueId);
            var bounce = new MailBounce(message.Subject, message.TextBody);
            await bounceFolder.AddFlagsAsync(uniqueId, MessageFlags.Deleted, true);

            if (bounce.IsBounce)
            {
                log.Add($"Email bounce processado:  subject: {message.Subject}, errorCode: {bounce.ErrorCode}");
                _ctx.MailBounces.Add(bounce);
            }
            else
            {
                log.Add($"Não vou processar porque NÃO É um email bounce:  subject: {message.Subject}");
            }
        }

        _ctx.SaveChanges();

        // Remove os emails bounce no server
        await bounceFolder.ExpungeAsync();

        await _imapClient.DisconnectAsync(true);

        return log;
    }

    private IMailFolder? GetBounceFolder()
    {
        var personal = _imapClient.GetFolder(_imapClient.PersonalNamespaces[0]);
        foreach (var folder in personal.GetSubfolders(false))
            if (folder.Name == _settings.BounceFolder)
                return folder;

        return null;
    }

    public async Task<IList<MailBounce>> GetBounces(IList<string> emails)
    {
        return await _ctx.MailBounces.Where(m => emails.Contains(m.Email)).ToListAsync();
    }

    public bool IsBounce(string email, IList<MailBounce> bounces)
    {
        var hardBounces = bounces.Where(b => !b.IsSoft).ToList();
        var softBounces = bounces.Where(b => b.IsSoft && b.CreationDate > DateTime.Now.AddDays(-1)).ToList();

        if (hardBounces.Exists(b => b.Email == email))
            return true;

        return softBounces.Exists(b => b.Email == email);
    }
}