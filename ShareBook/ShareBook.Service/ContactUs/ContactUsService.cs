using FluentValidation;
using ShareBook.Domain;
using ShareBook.Domain.Common;
using ShareBook.Service.Recaptcha;

namespace ShareBook.Service
{
    public class ContactUsService : IContactUsService
    {
        private readonly IContactUsEmailService _contactUsEmailService;
        private readonly IValidator<ContactUs> _validator;
        private readonly IRecaptchaService _recaptchaService;

        public ContactUsService(IContactUsEmailService contactUsEmailService, IValidator<ContactUs> validator, IRecaptchaService recaptchaService)
        {
            _contactUsEmailService = contactUsEmailService;
            _validator = validator;
            _recaptchaService = recaptchaService;
        }

        public Result<ContactUs> SendContactUs(ContactUs contactUs, string recaptchaReactive)
        {
            var result = new Result<ContactUs>(_validator.Validate(contactUs));

            var resultRecaptcha = _recaptchaService.SimpleValidationRecaptcha(recaptchaReactive);

            if (!resultRecaptcha.Success)
                result.Messages.AddRange(resultRecaptcha.Messages);

            if (!result.Success)
                return result;

            _contactUsEmailService.SendEmailContactUs(contactUs).Wait();

            return result;
        }
    }
}