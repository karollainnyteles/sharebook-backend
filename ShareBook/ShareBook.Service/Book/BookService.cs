using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ShareBook.Domain;
using ShareBook.Domain.Common;
using ShareBook.Domain.DTOs;
using ShareBook.Domain.Enums;
using ShareBook.Domain.Exceptions;
using ShareBook.Helper;
using ShareBook.Helper.Extensions;
using ShareBook.Helper.Image;
using ShareBook.Repository;
using ShareBook.Repository.UoW;
using ShareBook.Service.AwsSqs;
using ShareBook.Service.AwsSqs.Dto;
using ShareBook.Service.Generic;
using ShareBook.Service.Upload;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;

namespace ShareBook.Service;

public class BookService : BaseService<Book>, IBookService
{
    private readonly IBooksEmailService _booksEmailService;
    private readonly IConfiguration _configuration;

    private readonly NewBookQueue _newBookQueue;
    private readonly IUploadService _uploadService;

    private const string BooksDirectory = "Books";

    public BookService(IBookRepository bookRepository,
        IUnitOfWork unitOfWork, IValidator<Book> validator,
        IUploadService uploadService, IBooksEmailService booksEmailService, IConfiguration configuration,
        NewBookQueue newBookQueue)
        : base(bookRepository, unitOfWork, validator)
    {
        _uploadService = uploadService;
        _booksEmailService = booksEmailService;
        _configuration = configuration;
        _newBookQueue = newBookQueue;
    }

    public void Approve(Guid bookId, DateTime? chooseDate)
    {
        var daysInShowcase = int.Parse(_configuration["SharebookSettings:DaysInShowcase"]);

        var book = _repository.Get().Include(b => b.Category).FirstOrDefault(b => b.Id == bookId);
        if (book == null)
            throw new ShareBookException(ShareBookException.Error.NotFound);

        book.Status = BookStatus.Available;
        book.ChooseDate = chooseDate?.Date ?? DateTime.Today.AddDays(daysInShowcase);
        _repository.Update(book);

        // notifica o doador
        _booksEmailService.SendEmailBookApproved(book).Wait();

        // notifica possíveis interessados.
        var message = new NewBookBody
        {
            BookId = book.Id,
            BookTitle = book.Title,
            CategoryId = book.CategoryId
        };
        _newBookQueue.SendMessage(message).Wait();
    }

    public void Received(Guid bookId, Guid winnerUserId)
    {
        var book = _repository.Get().Include(f => f.BookUsers)
            .ThenInclude(bu => bu.User)
            .FirstOrDefault(f => f.Id == bookId);

        if (book == null)
            throw new ShareBookException(ShareBookException.Error.NotFound);

        // Verifica se o usuario é realmente o ganhador do livro
        var winner = book.WinnerUser();
        if (winner == null || winner.Id != winnerUserId)
            throw new ShareBookException(ShareBookException.Error.Forbidden);

        book.Status = BookStatus.Received;
        _repository.Update(book);

        _booksEmailService.SendEmailBookReceived(book);
    }

    public void UpdateBookStatus(Guid bookId, BookStatus bookStatus)
    {
        var book = _repository.Find(bookId);
        if (book == null)
            throw new ShareBookException(ShareBookException.Error.NotFound);

        book.Status = bookStatus;
        _repository.Update(book);
    }

    public IList<dynamic> FreightOptions()
    {
        var enumValues = new List<dynamic>();
        foreach (FreightOption freightOption in Enum.GetValues(typeof(FreightOption)))
            enumValues.Add(new
            {
                Value = freightOption.ToString(),
                Text = freightOption.Description()
            });
        return enumValues;
    }

    public IList<Book> AvailableBooks()
    {
        return SetImageUrl(
            _repository.Get()
                .Include(b => b.User)
                .ThenInclude(u => u.Address)
                .Include(b => b.Category)
                .Where(b => b.Status == BookStatus.Available)
                .OrderByDescending(b => b.CreationDate)
                .ToList()
        );
    }

    public IList<Book> Random15Books()
    {
        return SetImageUrl(
            _repository.Get()
                .Include(b => b.User)
                .ThenInclude(u => u.Address)
                .Include(b => b.Category)
                .Where(b => b.Status == BookStatus.Available)
                .OrderBy(x => Guid.NewGuid()) // ordem aleatória
                .Take(15) // apenas 15 registros
                .ToList()
        );
    }

    public IList<Book> Random15EBooks()
    {
        return SetImageUrl(
            _repository.Get()
                .Include(b => b.User)
                .ThenInclude(u => u.Address)
                .Include(b => b.Category)
                .Where(b => b.Status == BookStatus.Available && b.Type == BookType.Eletronic)
                .OrderBy(x => Guid.NewGuid()) // ordem aleatória
                .Take(15) // apenas 15 registros
                .ToList()
        );
    }

    public IList<Book> GetAll(int page, int items)
    {
        return _repository.Get().Include(b => b.User).Include(b => b.BookUsers)
            .Skip((page - 1) * items)
            .Take(items).ToList();
    }

    public override Book Find(object keyValue)
    {
        var result = _repository
            .Get()
            .Include(b => b.User)
            .ThenInclude(u => u.Address)
            .Include(b => b.Category)
            .Include(b => b.UserFacilitator)
            .FirstOrDefault(b => b.Id == (Guid)keyValue);

        if (result == null)
            throw new ShareBookException(ShareBookException.Error.NotFound);

        result.ImageUrl = _uploadService.GetImageUrl(result.ImageSlug, BooksDirectory);

        return result;
    }

    public override Result<Book> Insert(Book entity)
    {
        entity.UserId = new Guid(Thread.CurrentPrincipal?.Identity?.Name);

        EBookValidate(entity);

        var result = Validate(entity);
        if (result.Success)
        {
            entity.Slug = SetSlugByTitleOrIncremental(entity);

            entity.ImageSlug = ImageHelper.FormatImageName(entity.ImageName, entity.Slug);

            if (entity.IsEbookPdfValid())
                entity.EBookPdfFile = _uploadService.UploadPdf(entity.EBookPdfBytes, entity.EBookPdfFile, "EBooks");

            result.Value = _repository.Insert(entity);

            result.Value.ImageUrl = _uploadService.UploadImage(entity.ImageBytes, entity.ImageSlug, BooksDirectory);

            result.Value.ImageBytes = null;

            _booksEmailService.SendEmailNewBookInserted(entity).Wait();
        }

        return result;
    }

    public override Result<Book> Update(Book entity)
    {
        var result = Validate(entity, x =>
                x.Title,
            x => x.Author,
            x => x.FreightOption,
            x => x.Id);

        var bookId = entity.Id;

        if (!result.Success) return result;

        //buscar o book no banco para obter um objeto para ser re-hidratado
        var savedBook = _repository.Find(bookId);

        if (savedBook == null)
            throw new ShareBookException(ShareBookException.Error.NotFound);

        EBookValidate(entity);

        //imagem eh opcional no update
        if (!string.IsNullOrEmpty(entity.ImageName) && entity.ImageBytes.Length > 0)
        {
            entity.ImageSlug = ImageHelper.FormatImageName(entity.ImageName, savedBook.Slug);
            _uploadService.UploadImage(entity.ImageBytes, savedBook.ImageSlug, BooksDirectory);
        }

        //preparar o book para atualização
        savedBook.Author = entity.Author;
        savedBook.FreightOption = entity.FreightOption;
        savedBook.Author = entity.Author;
        savedBook.ImageSlug = entity.ImageSlug;
        savedBook.Title = entity.Title;
        savedBook.CategoryId = entity.CategoryId;

        // Condição efetuada para evitar busca no BD desnecessariamente por conta do SetSlugByTitleOrIncremental()
        if (savedBook.Slug != entity.Slug)
            savedBook.Slug = SetSlugByTitleOrIncremental(entity);

        savedBook.Synopsis = entity.Synopsis;
        savedBook.TrackingNumber = entity.TrackingNumber;

        if (entity.UserIdFacilitator.HasValue && entity.UserIdFacilitator != Guid.Empty)
            savedBook.UserIdFacilitator = entity.UserIdFacilitator;

        result.Value = _repository.UpdateAsync(savedBook).Result;
        result.Value.ImageBytes = null;

        return result;
    }

    public PagedList<Book> FullSearch(string criteria, int page, int itemsPerPage, bool isAdmin = false)
    {
        Expression<Func<Book, bool>> filter = x => (x.Author.Contains(criteria)
                                                    || x.Title.Contains(criteria)
                                                    || x.Category.Name.Contains(criteria))
                                                   && x.Status == BookStatus.Available;

        if (!isAdmin)
            filter = x => x.Author.Contains(criteria)
                          || x.Title.Contains(criteria)
                          || x.Category.Name.Contains(criteria);

        return SearchBooks(filter, page, itemsPerPage);
    }

    public PagedList<Book> ByCategoryId(Guid categoryId, int page, int items)
    {
        return SearchBooks(x => x.Status == BookStatus.Available && x.CategoryId == categoryId, page, items);
    }

    public Book BySlug(string slug)
    {
        var pagedBook = SearchBooks(x => x.Slug.Equals(slug), 1, 1);
        return pagedBook.Items.FirstOrDefault();
    }

    public bool UserRequestedBook(Guid bookId)
    {
        var userId = new Guid(Thread.CurrentPrincipal?.Identity?.Name);
        return _repository.Any(x =>
            x.Id == bookId &&
            x.BookUsers
                .Any(y =>
                    y.Status == DonationStatus.WaitingAction
                    && y.UserId == userId
                ));
    }

    public override PagedList<Book> Get<TKey>(Expression<Func<Book, bool>> filter, Expression<Func<Book, TKey>> order,
        int page, int itemsPerPage)
    {
        return base.Get(filter, order, page, itemsPerPage);
    }

    public IList<Book> GetUserDonations(Guid userId)
    {
        return _repository.Get()
            .Include(b => b.BookUsers)
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.CreationDate)
            .ToList();
    }

    public IList<Book> GetBooksChooseDateIsToday()
    {
        // limite é o dia de hoje.
        var startDateTime = DateTime.Today; //Today at 00:00:00
        var endDateTime = DateTime.Today.AddDays(1).AddTicks(-1); //Today at 23:59:59

        // livros em que o choosedate é hoje.
        var books = _repository
            .Get().Include(x => x.User).Include(x => x.BookUsers).Include(x => x.UserFacilitator)
            .Where(x =>
                x.ChooseDate >= startDateTime &&
                x.ChooseDate <= endDateTime
            ).ToList();

        return books;
    }

    public IList<Book> GetBooksChooseDateIsLate()
    {
        var today = DateTime.Today;

        var booksLate = _repository
            .Get().Include(x => x.User).Include(x => x.BookUsers).Include(x => x.UserFacilitator)
            .Where(x => x.ChooseDate < today && x.Status == BookStatus.AwaitingDonorDecision)
            .OrderBy(x => x.ChooseDate)
            .ToList();

        return booksLate;
    }

    /// <summary>
    ///     Bom para remover o livro da vitrine.
    /// </summary>
    /// <returns></returns>
    public IList<Book> GetBooksChooseDateIsTodayOrLate()
    {
        // limite é o dia de hoje.
        var endDateTime = DateTime.Today.AddDays(1).AddTicks(-1); //Today at 23:59:59

        // livros em que o choosedate é hoje.
        var books = _repository
            .Get().Include(x => x.User).Include(x => x.BookUsers).Include(x => x.UserFacilitator)
            .Where(x =>
                x.ChooseDate <= endDateTime && x.Status == BookStatus.Available
            ).ToList();

        return books;
    }

    public void AddFacilitatorNotes(Guid bookId, string facilitatorNotes)
    {
        var book = _repository.Find(bookId);
        if (book == null)
            throw new ShareBookException(ShareBookException.Error.NotFound);

        var saoPauloNow = DateTimeHelper.ConvertDateTimeSaoPaulo(DateTime.Now);
        var date = saoPauloNow.ToString("dd/MM/yyyy");
        var lineBreak = string.IsNullOrEmpty(book.FacilitatorNotes) ? "" : "\n";
        book.FacilitatorNotes += string.Format("{0}{1} - {2}", lineBreak, date, facilitatorNotes);

        _repository.Update(book);
    }

    public Book GetBookWithAllUsers(Guid bookId)
    {
        var books = _repository
            .Get().Include(x => x.User).ThenInclude(u => u.Address)
            .Include(x => x.UserFacilitator).ThenInclude(u => u.Address)
            .Include(x => x.BookUsers).ThenInclude(bu => bu.User).ThenInclude(u => u.Address)
            .Where(x => x.Id == bookId)
            .ToList();

        return books.FirstOrDefault();
    }

    public void RenewChooseDate(Guid bookId)
    {
        var book = _repository.Find(bookId);
        if (book == null)
            throw new ShareBookException(ShareBookException.Error.NotFound);

        if (!book.MayChooseWinner())
            throw new ShareBookException(ShareBookException.Error.BadRequest, "Aguarde a data de decisão.");

        book.Status = BookStatus.Available;
        book.ChooseDate = DateTime.Now.AddDays(10);
        _repository.Update(book);
    }

    private IList<Book> SetImageUrl(IList<Book> books)
    {
        return books.Select(b =>
        {
            b.ImageUrl = _uploadService.GetImageUrl(b.ImageSlug, BooksDirectory);
            return b;
        }).ToList();
    }

    #region Private

    private PagedList<Book> SearchBooks(Expression<Func<Book, bool>> filter, int page, int itemsPerPage)
    {
        return SearchBooks(filter, page, itemsPerPage, x => x.CreationDate);
    }

    private PagedList<Book> SearchBooks<TKey>(Expression<Func<Book, bool>> filter, int page, int itemsPerPage,
        Expression<Func<Book, TKey>> expression)
    {
        var query = _repository.Get()
            .Where(filter)
            .OrderByDescending(expression)
            .Select(u => new Book
            {
                Id = u.Id,
                Title = u.Title,
                Author = u.Author,
                Status = u.Status,
                FreightOption = u.FreightOption,
                ImageUrl = _uploadService.GetImageUrl(u.ImageSlug, BooksDirectory),
                Slug = u.Slug,
                CreationDate = u.CreationDate,
                Synopsis = u.Synopsis,
                ChooseDate = u.ChooseDate,
                User = new User
                {
                    Id = u.User.Id,
                    Email = u.User.Email,
                    Name = u.User.Name,
                    Linkedin = u.User.Linkedin,
                    Address = new Address
                    {
                        City = u.User.Address.City,
                        State = u.User.Address.State,
                        Country = u.User.Address.Country,
                        UserId = u.User.Address.UserId,
                        Id = u.User.Address.Id,
                        CreationDate = u.User.Address.CreationDate
                    }
                },
                CategoryId = u.CategoryId,
                Category = u.Category
            });

        return FormatPagedList(query, page, itemsPerPage);
    }

    private string SetSlugByTitleOrIncremental(Book entity)
    {
        var slug = _repository.Get()
            .Where(x => x.Title.ToUpper().Trim().Equals(entity.Title.ToUpper().Trim())
                        && !x.Id.Equals(entity.Id))
            .OrderByDescending(x => x.CreationDate).FirstOrDefault()?.Slug;

        return string.IsNullOrWhiteSpace(slug) ? entity.Title.GenerateSlug() : slug.AddIncremental();
    }

    private static void EBookValidate(Book entity)
    {
        if (entity.Type == BookType.Eletronic &&
            string.IsNullOrEmpty(entity.EBookDownloadLink) &&
            string.IsNullOrEmpty(entity.EBookPdfFile))
            throw new ShareBookException(ShareBookException.Error.BadRequest,
                "Necessário informar o link ou o arquivo em caso de um E-Book.");
    }

    public BookStatsDto GetStats()
    {
        var groupedStatus = _repository.Get()
            .GroupBy(b => b.Status)
            .Select(g => new
            {
                Status = g.Key,
                Total = g.Count()
            })
            .ToList();

        var status = new BookStatsDto();

        status.TotalWaitingApproval = groupedStatus.FirstOrDefault(g => g.Status == BookStatus.WaitingApproval)?.Total ?? 0;

        status.TotalOk = groupedStatus
            .Where(g => g.Status is BookStatus.WaitingSend or BookStatus.Sent or BookStatus.Received)
            .Sum(g => g.Total);

        return status;
    }

    #endregion Private
}