﻿using Microsoft.Extensions.Configuration;
using Moq;
using ShareBook.Domain;
using ShareBook.Domain.Validators;
using ShareBook.Repository;
using ShareBook.Repository.UoW;
using ShareBook.Service;
using ShareBook.Service.Muambator;
using ShareBook.Test.Unit.Mocks;
using System;
using System.Threading;
using Xunit;

namespace ShareBook.Test.Unit.Services
{
    public class BookUserServiceTests
    {
        private Guid bookId;

        private readonly Mock<IBookService> bookServiceMock;
        private readonly Mock<IBookUserRepository> bookUserRepositoryMock;
        private readonly Mock<IBooksEmailService> bookEmailService;
        private readonly Mock<IUnitOfWork> unitOfWorkMock;
        private readonly Mock<IBookUsersEmailService> bookUsersEmailService;
        private readonly BookUserValidator bookUserValidator;
        private readonly Mock<IMuambatorService> muambatorServiceMock;
        private readonly Mock<IBookRepository> bookRepositoryMock;
        private readonly Mock<IConfiguration> configurationMock;

        public BookUserServiceTests()
        {
            bookId = new Guid("5489A967-9320-4350-E6FC-08D5CC8498F3");
            bookServiceMock = new Mock<IBookService>();
            bookUserRepositoryMock = new Mock<IBookUserRepository>();
            bookEmailService = new Mock<IBooksEmailService>();
            unitOfWorkMock = new Mock<IUnitOfWork>();
            bookUsersEmailService = new Mock<IBookUsersEmailService>();
            muambatorServiceMock = new Mock<IMuambatorService>();
            bookRepositoryMock = new Mock<IBookRepository>();
            configurationMock = new Mock<IConfiguration>();

            configurationMock.Setup(c => c["SharebookSettings:MaxRequestsPerBook"]).Returns("50");

            bookServiceMock.SetReturnsDefault(true);

            bookServiceMock.Setup(s => s.GetBookWithAllUsers(It.IsAny<Guid>())).Returns(() =>
            {
                return BookMock.GetLordTheRings();
            });
        }

        [Fact]
        public void RequestBook()
        {
            Thread.CurrentPrincipal = UserMock.GetClaimsUser();
            var service = new BookUserService(bookUserRepositoryMock.Object,
                bookServiceMock.Object, bookUsersEmailService.Object, muambatorServiceMock.Object, bookRepositoryMock.Object,
                unitOfWorkMock.Object, bookUserValidator, configurationMock.Object);

            const string reason = "I need this book because I'm learning a new programming language.";
            service.Insert(bookId, reason);

            bookUserRepositoryMock
                .Verify(x => x.Insert(It.IsAny<BookUser>()), Times.Once);
        }
    }
}