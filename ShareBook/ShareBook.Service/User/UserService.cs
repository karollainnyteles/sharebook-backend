﻿using AutoMapper;
using FluentValidation;
using ShareBook.Domain;
using ShareBook.Domain.Common;
using ShareBook.Domain.DTOs;
using ShareBook.Domain.Enums;
using ShareBook.Domain.Exceptions;
using ShareBook.Helper.Crypto;
using ShareBook.Repository;
using ShareBook.Repository.Repository;
using ShareBook.Repository.UoW;
using ShareBook.Service.Generic;
using ShareBook.Service.Recaptcha;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace ShareBook.Service
{
    public class UserService : BaseService<User>, IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IBookRepository _bookRepository;
        private readonly IUserEmailService _userEmailService;
        private readonly IRecaptchaService _recaptchaService;

        private readonly IMapper _mapper;

        #region Public

        public UserService(IUserRepository userRepository, IBookRepository bookRepository,
            IUnitOfWork unitOfWork,
            IValidator<User> validator,
            IMapper mapper,
            IUserEmailService userEmailService,
            IRecaptchaService recaptchaService) : base(userRepository, unitOfWork, validator)
        {
            _userRepository = userRepository;
            _userEmailService = userEmailService;
            _bookRepository = bookRepository;
            _mapper = mapper;
            _recaptchaService = recaptchaService;
        }

        public Result<User> AuthenticationByEmailAndPassword(User user)
        {
            var result = Validate(user, x => x.Email, x => x.Password);

            string decryptedPass = user.Password;

            user = _repository.Find(e => e.Email == user.Email);

            if (user == null)
            {
                result.Messages.Add("Não encontramos esse email no Sharebook. Você já se cadastrou?");
                return result;
            }

            if (user.IsBruteForceLogin())
            {
                result.Messages.Add("Login bloqueado por 30 segundos, para proteger sua conta.");
                return result;
            }

            // persiste última tentativa de login ANTES do SUCESSO ou FALHA pra ter métrica de
            // verificação de brute force.
            user.LastLogin = DateTime.Now;
            _userRepository.Update(user);

            if (!IsValidPassword(user, decryptedPass))
            {
                result.Messages.Add("Email ou senha incorretos");
                return result;
            }

            if (!user.Active)
            {
                result.Messages.Add("Usuário com acesso temporariamente suspenso.");
                return result;
            }

            if (!user.ParentAproved)
            {
                result.Messages.Add($"Usuário menor de idade. Aguardando consentimento dos pais. Foi enviado um email para {user.ParentEmail} em {user.CreationDate?.ToString("dd/MM/yyyy")}.");
                return result;
            }

            result.Value = UserCleanup(user);
            return result;
        }

        public Result<User> Insert(RegisterUserDto userDto)
        {
            User user = _mapper.Map<User>(userDto);
            Result resultRecaptcha = _recaptchaService.SimpleValidationRecaptcha(userDto?.RecaptchaReactive);

            var result = Validate(user);
            if (!resultRecaptcha.Success && resultRecaptcha.Messages != null)
                result.Messages.AddRange(resultRecaptcha.Messages);

            if (!result.Success)
                return result;

            // Senha forte não é mais obrigatória.

            if (_repository.Any(x => x.Email == user.Email))
                throw new ShareBookException("Usuário já possui email cadastrado.");

            // LGPD - CONSENTIMENTO DOS PAIS.
            if (userDto?.Age < 12)
                ParentAprovalStartFlow(userDto, user);

            user.Email = user.Email.ToLowerInvariant();
            if (result.Success)
            {
                user = GetUserEncryptedPass(user);
                result.Value = UserCleanup(_repository.Insert(user));
            }
            return result;
        }

        private void ParentAprovalStartFlow(RegisterUserDto userDto, User user)
        {
            user.ParentAproved = false;
            user.ParentHashCodeAproval = Guid.NewGuid().ToString();

            if (string.IsNullOrEmpty(userDto.ParentEmail))
                throw new ShareBookException("Menor de idade. Obrigatório informar o email do pai ou responsável.");

            _userEmailService.SendEmailRequestParentAproval(userDto, user);
        }

        public override Result<User> Update(User user)
        {
            user.Id = new Guid(Thread.CurrentPrincipal?.Identity?.Name);
            Result<User> result = Validate(user, x =>
               x.Email,
                x => x.Linkedin,
                x => x.Name,
                x => x.Phone,
                x => x.Id);

            if (!result.Success) return result;

            var userAux = _repository.Find(new IncludeList<User>(x => x.Address), user.Id);

            if (userAux == null) result.Messages.Add("Usuário não existe.");

            if (_repository.Any(u => u.Email == user.Email && u.Id != user.Id))
                result.Messages.Add("Email já existe.");

            if (result.Success && userAux is not null)
            {
                userAux.Change(user.Email, user.Name, user.Linkedin, user.Instagram, user.Phone, user.AllowSendingEmail);
                userAux.ChangeAddress(user.Address);

                result.Value = UserCleanup(_repository.Update(userAux));
            }

            return result;
        }

        public override User Find(object keyValue)
        {
            var includes = new IncludeList<User>(x => x.Address);
            return _repository.Find(includes, keyValue);
        }

        public Result<User> ValidOldPasswordAndChangeUserPassword(User user, string newPassword)
        {
            var resultUserAuth = this.AuthenticationByIdAndPassword(user);

            if (resultUserAuth.Success)
                ChangeUserPassword(resultUserAuth.Value, newPassword);

            return resultUserAuth;
        }

        public Result<User> ChangeUserPassword(User user, string newPassword)
        {
            var result = Validate(user);

            // Senha forte não é mais obrigatória. Apenas validação de tamanho.
            if (newPassword.Length < 6 || newPassword.Length > 32)
                throw new ShareBookException("A senha deve ter entre 6 e 32 letras.");

            user.ChangePassword(newPassword);
            user = GetUserEncryptedPass(user);
            user = _userRepository.UpdatePassword(user).Result;
            result.Value = UserCleanup(user);

            return result;
        }

        public Result GenerateHashCodePasswordAndSendEmailToUser(string email)
        {
            var result = new Result();
            var user = _repository.Find(e => e.Email == email);

            if (user == null)
            {
                result.Messages.Add("E-mail não encontrado.");
                return result;
            }

            user.GenerateHashCodePassword();
            _repository.Update(user);
            _userEmailService.SendEmailForgotMyPasswordToUserAsync(user).Wait();
            result.SuccessMessage = "E-mail enviado com as instruções para recuperação da senha.";
            return result;
        }

        public Result ConfirmHashCodePassword(string hashCodePassword)
        {
            var result = new Result();

            var userConfirmedHashCodePassword = _repository.Find(e => e.HashCodePassword.Equals(hashCodePassword));

            if (userConfirmedHashCodePassword == null)
                result.Messages.Add("Hash code não encontrado.");
            else if (result.Success && !userConfirmedHashCodePassword.HashCodePasswordIsValid(hashCodePassword))
                result.Messages.Add("Chave errada ou expirada. Por favor gere outra chave");
            else
                result.Value = UserCleanup(userConfirmedHashCodePassword);

            return result;
        }

        public IList<User> GetFacilitators(Guid userIdDonator)
        {
            string query = @"SELECT
                            CONCAT(Name, ' (', total, ')') as Name, Id
                            FROM
                            (
                                SELECT TOP 100
                                    u.Name, u.Id,
                                    (SELECT COUNT(*) as total FROM Books b
                                      WHERE b.UserIdFacilitator = u.Id and b.UserId = {0}
                                    ) as total
                                FROM
                                    Users u
                                WHERE u.Profile = 0 -- Administrador
                                ORDER BY total desc, u.Name
                            ) sub";
            var parameters = new object[] { userIdDonator };

            return _repository.FromSql(query, parameters)
                    .Select(x => new User
                    {
                        Id = x.Id,
                        Name = x.Name
                    }).ToList();
        }

        public IList<User> GetAdmins()
        {
            return _userRepository.Get()
                .Where(u => u.Profile == Domain.Enums.Profile.Administrator)
                .ToList();
        }

        #endregion Public

        #region Private

        private Result<User> AuthenticationByIdAndPassword(User user)
        {
            var result = Validate(user, x => x.Id, x => x.Password);

            string decryptedPass = user.Password;

            user = _repository
                .Get()
                .FirstOrDefault(e => e.Id == user.Id);

            if (user == null || !IsValidPassword(user, decryptedPass))
            {
                result.Messages.Add("Senha incorreta");
                return result;
            }

            result.Value = UserCleanup(user);
            return result;
        }

        public bool IsValidPassword(User user, string decryptedPass)
        {
            return user.Password == Hash.Create(decryptedPass, user.PasswordSalt);
        }

        private static User GetUserEncryptedPass(User user)
        {
            user.PasswordSalt = Salt.Create();
            user.Password = Hash.Create(user.Password, user.PasswordSalt);
            return user;
        }

        private static User UserCleanup(User user)
        {
            user.Password = string.Empty;
            user.PasswordSalt = string.Empty;
            return user;
        }

        public IList<User> GetBySolicitedBookCategory(Guid bookCategoryId) =>
            _userRepository.Get().Where(u => u.AllowSendingEmail && u.BookUsers.Any(bu => bu.Book.CategoryId == bookCategoryId)).ToList();

        public UserStatsDto GetStats(Guid? userId)
        {
            var user = _userRepository.Find(userId);
            var books = _bookRepository.Get().Where(b => b.UserId == userId).ToList();

            if (user == null) throw new ShareBookException(ShareBookException.Error.NotFound, "Usuário não encontrado.");

            var stats = new UserStatsDto
            {
                CreationDate = user.CreationDate,
                TotalLate = books.Count(b => b.ChooseDate < DateTime.Today && b.Status == BookStatus.AwaitingDonorDecision),
                TotalOk = books.Count(b => b.Status == BookStatus.WaitingSend || b.Status == BookStatus.Sent || b.Status == BookStatus.Received),
                TotalCanceled = books.Count(b => b.Status == BookStatus.Canceled),
                TotalWaitingApproval = books.Count(b => b.Status == BookStatus.WaitingApproval),
                TotalAvailable = books.Count(b => b.Status == BookStatus.Available),
            };
            return stats;
        }

        public void ParentAproval(string parentHashCodeAproval)
        {
            var user = _repository
                .Get()
                .FirstOrDefault(u => u.ParentHashCodeAproval == parentHashCodeAproval);

            if (user == null)
                throw new ShareBookException(ShareBookException.Error.NotFound, "Nenhum usuário encontrado.");

            if (user.ParentAproved)
                throw new ShareBookException(ShareBookException.Error.NotFound, "O acesso já foi liberado anteriormente. Tudo certo.");

            user.ParentAproved = true;
            _userRepository.Update(user);

            _userEmailService.SendEmailParentAprovedNotifyUser(user);
        }

        #endregion Private
    }
}