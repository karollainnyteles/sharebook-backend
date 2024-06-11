using Microsoft.Extensions.Configuration;
using Moq;
using ShareBook.Domain;
using ShareBook.Domain.Common;
using ShareBook.Domain.Enums;
using ShareBook.Domain.Validators;
using ShareBook.Repository;
using ShareBook.Repository.UoW;
using ShareBook.Service;
using ShareBook.Service.AwsSqs;
using ShareBook.Service.Upload;
using ShareBook.Test.Unit.Mocks;
using System;
using System.Text;
using System.Threading;
using Xunit;

namespace ShareBook.Test.Unit.Services
{
    public class BookServiceTests
    {
        private readonly Mock<IBookService> bookServiceMock;
        private readonly Mock<IUploadService> uploadServiceMock;
        private readonly Mock<IBookRepository> bookRepositoryMock;
        private readonly Mock<IBooksEmailService> bookEmailService;
        private readonly Mock<IUnitOfWork> unitOfWorkMock;
        private readonly Mock<IBookUserService> bookUserServiceMock;
        private readonly Mock<IConfiguration> configurationMock;

        private readonly Mock<NewBookQueue> sqsMock;

        public BookServiceTests()
        {
            // Definindo quais serão as classes mockadas
            bookServiceMock = new Mock<IBookService>();
            uploadServiceMock = new Mock<IUploadService>();
            unitOfWorkMock = new Mock<IUnitOfWork>();
            bookRepositoryMock = new Mock<IBookRepository>();
            bookEmailService = new Mock<IBooksEmailService>();
            bookUserServiceMock = new Mock<IBookUserService>();
            configurationMock = new Mock<IConfiguration>();
            sqsMock = new Mock<NewBookQueue>();

            bookRepositoryMock.Setup(repo => repo.Insert(It.IsAny<Book>())).Returns(() =>
            {
                return BookMock.GetLordTheRings();
            });
            uploadServiceMock.Setup(service => service.UploadImage(null, null, null));
            bookServiceMock.Setup(service => service.Insert(It.IsAny<Book>())).Verifiable();
        }

        [Fact]
        public void AddBook()
        {
            Thread.CurrentPrincipal = UserMock.GetClaimsUser();
            var service = new BookService(bookRepositoryMock.Object,
                unitOfWorkMock.Object, new BookValidator(),
                uploadServiceMock.Object, bookEmailService.Object, configurationMock.Object, sqsMock.Object);
            Result<Book> result = service.Insert(new Book()
            {
                Title = "Lord of the Rings",
                Author = "J. R. R. Tolkien",
                ImageName = "lotr.png",
                ImageBytes = Encoding.UTF8.GetBytes("STRINGBASE64"),
                FreightOption = FreightOption.City,
                CategoryId = Guid.NewGuid()
            });
            Assert.NotNull(result);
            Assert.True(result.Success);
        }

        [Fact]
        public void AddEBookByLink()
        {
            Thread.CurrentPrincipal = UserMock.GetClaimsUser();
            var service = new BookService(bookRepositoryMock.Object,
                unitOfWorkMock.Object, new BookValidator(),
                uploadServiceMock.Object, bookEmailService.Object, configurationMock.Object, sqsMock.Object);
            Result<Book> result = service.Insert(new Book()
            {
                Title = "Lord of the Rings",
                Author = "J. R. R. Tolkien",
                ImageName = "lotr.png",
                ImageBytes = Encoding.UTF8.GetBytes("STRINGBASE64"),
                FreightOption = FreightOption.City,
                CategoryId = Guid.NewGuid(),
                Type = BookType.Eletronic,
                EBookDownloadLink = "download-link-ebook"
            });
            Assert.NotNull(result);
            Assert.True(result.Success);
        }

        [Fact]
        public void AddEBookByPdfFile()
        {
            Thread.CurrentPrincipal = UserMock.GetClaimsUser();
            var service = new BookService(bookRepositoryMock.Object,
                unitOfWorkMock.Object, new BookValidator(),
                uploadServiceMock.Object, bookEmailService.Object, configurationMock.Object, sqsMock.Object);
            Result<Book> result = service.Insert(new Book()
            {
                Title = "Lord of the Rings",
                Author = "J. R. R. Tolkien",
                ImageName = "lotr.png",
                ImageBytes = Encoding.UTF8.GetBytes("STRINGBASE64"),
                FreightOption = FreightOption.City,
                CategoryId = Guid.NewGuid(),
                Type = BookType.Eletronic,
                EBookPdfFile = "pdf-ebook",
                EBookPdfBytes = Encoding.UTF8.GetBytes("STRINGBASE64")
            });
            Assert.NotNull(result);
            Assert.True(result.Success);
        }
    }
}