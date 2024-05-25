using ShareBook.Domain.Common;
using System;
using System.Text.RegularExpressions;

namespace ShareBook.Domain;

public class MailBounce : BaseEntity
{
    public string? Email { get; set; }
    public string? Subject { get; set; }
    public string? Body { get; set; }
    public string? ErrorCode { get; set; }
    public bool IsSoft { get; set; } = false;
    public bool IsBounce { get; set; } = false;

    public MailBounce(string subject, string body)
    {
        Subject = subject;
        Body = body;

        ExtractFromBody();
    }

    private void ExtractFromBody()
    {
        try
        {
            if (string.IsNullOrEmpty(Body)) return;

            // Tenta extrair o email de destino original do corpo do email
            var emailPattern = @"\w+([-+.]\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*";
            var errorCodePattern = @"Remote Server returned: '(\d{3})";

            var emailRegex = new Regex(emailPattern, RegexOptions.None, TimeSpan.FromSeconds(5));
            var emailMatch = emailRegex.Match(Body);

            Email = emailMatch.Success ? emailMatch.Value : null;

            // Verifica se o corpo do email contém um código de erro
            var errorCodeRegex = new Regex(errorCodePattern, RegexOptions.None, TimeSpan.FromSeconds(5));
            var errorCodeMatch = errorCodeRegex.Match(Body);

            if (errorCodeMatch.Success)
            {
                IsBounce = true;
                ErrorCode = errorCodeMatch.Groups.Count == 2 ? errorCodeMatch.Groups[1].Value : null;

                if (!string.IsNullOrEmpty(ErrorCode) && ErrorCode.StartsWith('4'))
                {
                    // Soft bounce
                    IsSoft = true;
                }
            }
            else
            {
                IsBounce = false;
                ErrorCode = null;
            }
        }
        catch (RegexMatchTimeoutException)
        {
            Console.WriteLine("A operação de correspondência de regex excedeu o tempo limite.");
        }
    }
}