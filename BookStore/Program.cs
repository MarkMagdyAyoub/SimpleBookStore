using System;
using System.Collections.Generic;
using System.Linq;

namespace BookStore;

// i assume that the Showcase/Demo book is not for sale wich means not have a price
abstract class Book
{
    public string Title { get; set; }
    public string Author { get; set; }
    public uint Pages { get; set; }
    public string ISBN { get; set; }
    public DateTime YearOfPublishment { get; set; }

    public Book(string title, string author, uint pages, string ISBN, DateTime YearOfPublishment)
    {
        Title = title;
        Author = author;
        Pages = pages;
        this.ISBN = ISBN;
        this.YearOfPublishment = YearOfPublishment;
    }
}

// if the book is for sale then it will implement that interface
interface IPriceable
{
    public double Price { get; set; }
}

// if the book can be shipped then it will implement that interface
interface IShippable
{
    public double Weight { get; set; }
}

// assume we support only pdf , epub , mobi file types for ebooks
enum FileType
{
    PDF,
    EPUB,
    MOBI
}

class PaperBook : Book, IPriceable, IShippable
{
    public double Price { get; set; }
    public double Weight { get; set; }

    public PaperBook(
      string title,
      string author,
      uint pages,
      string ISBN,
      DateTime YearOfPublishment,
      double price,
      double weight
    )
    : base(title, author, pages, ISBN, YearOfPublishment)
    {
        Price = price;
        Weight = weight;
    }
}


// EBook is an abstract class because it can be payable or demo 
abstract class EBook : Book
{
    public FileType FileType { get; set; }

    public EBook(
        string title,
        string author,
        uint pages,
        string isbn,
        DateTime yearOfPublishment,
        FileType fileType
    )
        : base(title, author, pages, isbn, yearOfPublishment)
    {
        FileType = fileType;
    }

    public override string ToString()
    {
        return $"Title: {Title}\nAuthor: {Author}\nPages: {Pages}\nISBN: {ISBN}\nYear: {YearOfPublishment.Year}\nFileType: {FileType}";
    }
}

class PayedEBook : EBook, IPriceable
{
    public double Price { get; set; }

    public PayedEBook(
        string title,
        string author,
        uint pages,
        string isbn,
        DateTime yearOfPublishment,
        FileType fileType,
        double price
    )
        : base(title, author, pages, isbn, yearOfPublishment, fileType)
    {
        Price = price;
    }

    public override string ToString()
    {
        return base.ToString() + $"\nPrice: ${Price}";
    }
}

class DemoBook : EBook
{
    public string DemoNote { get; set; }

    public DemoBook(
        string title,
        string author,
        uint pages,
        string ISBN,
        DateTime YearOfPublishment,
        FileType fileType,
        string demoNote = "Sample Preview"
    ) : base(title, author, pages, ISBN, YearOfPublishment, fileType)
    {
        DemoNote = demoNote;
    }

    public override string ToString()
    {
        return base.ToString() + $"\nNote: {DemoNote}";
    }
}

static class BookMAilService
{
    public static void SendMail(string from, string to, string subject, EBook book)
    {
        Console.WriteLine("=== Sending Email ===");
        Console.WriteLine($"From: {from}");
        Console.WriteLine($"To: {to}");
        Console.WriteLine($"Subject: {subject}");
        Console.WriteLine("--- Book Attached ---");
        Console.WriteLine(book.ToString());
        Console.WriteLine("======================\n");
    }
}

static class PaperBookShippingService
{
    // Assume shipping is 5.0 dollars per kg
    private const double ShippingRatePerKg = 5.0;

    public static double CalculateShippingCost(PaperBook book)
    {
        return book.Weight * ShippingRatePerKg;
    }

    public static void Print(PaperBook book, string recipientName, string address)
    {
        Console.WriteLine("=== Shipping Label ===");
        Console.WriteLine($"Recipient: {recipientName}");
        Console.WriteLine($"Address: {address}");
        Console.WriteLine($"Book Title: {book.Title}");
        Console.WriteLine($"Author: {book.Author}");
        Console.WriteLine($"Weight: {book.Weight} kg");
        Console.WriteLine($"Shipping Cost: ${CalculateShippingCost(book):0.00}");
        Console.WriteLine("======================");
    }
}

class BookStock
{
    public Book Book { get; }
    // private setter because i want to check the quantity before removing it
    public int Quantity { get; private set; }

    public BookStock(Book book, int quantity)
    {
        Book = book;
        Quantity = quantity;
    }

    public void AddStock(int amount)
    {
        Quantity += amount;
    }

    // i prefer to return a boolean instead of void to check if the operation was successful
    // to delegate the error handling to the caller instead of handling it inside the method
    public bool RemoveStock(int amount)
    {
        if (Quantity >= amount)
        {
            Quantity -= amount;
            return true;
        }
        return false;
    }

    public override string ToString()
    {
        return $"{Book.Title} by {Book.Author} ({Book.GetType().Name}) - Stock: {Quantity}";
    }
}

class Inventory
{
    private List<BookStock> stocks = new List<BookStock>();

    // i assume if the developer give me a book that already exists in the inventory 
    // then i will just add the quantity to the existing book
    // otherwise i will add the book to the inventory
    public void AddBook(Book book, int quantity)
    {
        var existing = stocks.FirstOrDefault(b => b.Book.ISBN == book.ISBN);
        if (existing != null)
        {
            existing.AddStock(quantity);
        }
        else
        {
            stocks.Add(new BookStock(book, quantity));
        }
    }

    public bool SellBook(string isbn, int quantity = 1)
    {
        var stock = stocks.FirstOrDefault(b => b.Book.ISBN == isbn);
        if (stock != null && stock.RemoveStock(quantity))
        {
            Console.WriteLine($"Sold {quantity} copy of '{stock.Book.Title}'.");
            return true;
        }

        Console.WriteLine($"Not enough stock for ISBN: {isbn}.");
        return false;
    }

    public void ListInventory()
    {
        Console.WriteLine("=== Inventory List ===");
        foreach (var stock in stocks)
        {
            Console.WriteLine(stock.ToString());
        }
        Console.WriteLine("======================\n");
    }

    public Book? FindBook(string isbn)
    {
        return stocks.FirstOrDefault(b => b.Book.ISBN == isbn)?.Book;
    }

    public bool RemoveBook(string isbn)
    {
        var existing = stocks.FirstOrDefault(b => b.Book.ISBN == isbn);
        if (existing != null)
        {
            stocks.Remove(existing);
            return true;
        }
        return false;
    }

    public List<Book> RemoveOutdatedBooks(int maxAgeInYears)
    {
        DateTime startingDate = DateTime.Now.AddYears(-maxAgeInYears);
        var outdatedBook =
            stocks
            .Where(stockItem => stockItem.Book.YearOfPublishment < startingDate)
            .ToList();

        // remove the outdated books from the inventory
        foreach (var stockItem in outdatedBook)
        {
            this.RemoveBook(stockItem.Book.ISBN);
        }
        return outdatedBook.Select(stockItem => stockItem.Book).ToList();
    }
}


static class Tests
{
    public static void RunAllTests()
    {
        AddBookTest();
        Divider();

        SellBookTest();
        Divider();

        SellBookInsufficientStockTest();
        Divider();

        RemoveBookTest();
        Divider();

        RemoveNonexistentBookTest();
        Divider();

        RemoveOutdatedBooksTest();
        Divider();

        FindBookTest();
        Divider();

        SendEBookMailTest();
        Divider();

        ShippingLabelTest();
        Divider();

        AddMixOfBooksTest();
        Divider();
    }

    private static void Divider() => Console.WriteLine(new string('-', 50));

    public static void AddBookTest()
    {
        Console.WriteLine("AddBookTest:");
        var inventory = new Inventory();
        var book = new PaperBook("The Pragmatic Programmer", "Andrew Hunt & David Thomas", 352, "9780135957059", new DateTime(2019, 9, 13), 39.99, 0.9);
        inventory.AddBook(book, 3);
        inventory.ListInventory();
    }

    public static void SellBookTest()
    {
        Console.WriteLine("SellBookTest:");
        var inventory = new Inventory();
        var ebook = new PayedEBook("Introduction to Algorithms", "Thomas H. Cormen", 1312, "9780262033848", new DateTime(2009, 7, 31), FileType.EPUB, 49.99);
        inventory.AddBook(ebook, 2);
        inventory.SellBook("9780262033848", 1);
        inventory.ListInventory();
    }

    public static void SellBookInsufficientStockTest()
    {
        Console.WriteLine("SellBookInsufficientStockTest:");
        var inventory = new Inventory();
        var book = new PaperBook("Refactoring", "Martin Fowler", 448, "9780201485677", new DateTime(1999, 7, 8), 44.99, 1.1);
        inventory.AddBook(book, 1);
        inventory.SellBook("9780201485677", 2);
        inventory.ListInventory();
    }

    public static void RemoveBookTest()
    {
        Console.WriteLine("RemoveBookTest:");
        var inventory = new Inventory();
        var book = new PaperBook("Code Complete", "Steve McConnell", 960, "9780735619678", new DateTime(2004, 6, 9), 54.99, 1.5);
        inventory.AddBook(book, 1);
        inventory.RemoveBook("9780735619678");
        inventory.ListInventory();
    }

    public static void RemoveNonexistentBookTest()
    {
        Console.WriteLine("RemoveNonexistentBookTest:");
        var inventory = new Inventory();
        bool removed = inventory.RemoveBook("9780000000000");
        Console.WriteLine(removed ? "Error: Should not remove nonexistent book" : "Correct: Book not found");
    }

    public static void RemoveOutdatedBooksTest()
    {
        Console.WriteLine("RemoveOutdatedBooksTest:");
        var inventory = new Inventory();
        var oldBook = new PaperBook("Structured Programming", "Dijkstra", 210, "9780138544713", new DateTime(1980, 1, 1), 18.50, 0.6);
        var newBook = new PaperBook("Modern Software Engineering", "David Farley", 320, "9780137314911", new DateTime(2022, 5, 10), 34.50, 0.8);

        inventory.AddBook(oldBook, 2);
        inventory.AddBook(newBook, 2);

        var removed = inventory.RemoveOutdatedBooks(10);
        foreach (var book in removed)
        {
            Console.WriteLine($"Removed: {book.Title} ({book.YearOfPublishment.Year})");
        }

        inventory.ListInventory();
    }

    public static void FindBookTest()
    {
        Console.WriteLine("FindBookTest:");
        var inventory = new Inventory();
        var book = new PayedEBook("Clean Architecture", "Robert C. Martin", 432, "9780134494166", new DateTime(2017, 9, 20), FileType.MOBI, 37.95);
        inventory.AddBook(book, 1);
        var found = inventory.FindBook("9780134494166");
        Console.WriteLine(found != null ? $"Found: {found.Title}" : "Book not found");
    }

    public static void SendEBookMailTest()
    {
        Console.WriteLine("SendEBookMailTest:");
        var demo = new DemoBook("Design Patterns Sample", "Erich Gamma et al.", 50, "9780201633610", new DateTime(1994, 10, 21), FileType.PDF);
        BookMAilService.SendMail("store@gmail.com", "markmagdy@gmail.com", "Free sample - Design Patterns", demo);
    }

    public static void ShippingLabelTest()
    {
        Console.WriteLine("ShippingLabelTest:");
        var book = new PaperBook("Distributed Systems", "Maarten van Steen", 500, "9781543057386", new DateTime(2017, 12, 15), 47.99, 1.4);
        PaperBookShippingService.Print(book, "Mark Magdy", "123 Nile St, Cairo, Egypt");
    }

    public static void AddMixOfBooksTest()
    {
        Console.WriteLine("AddMixOfBooksTest:");
        var inventory = new Inventory();

        var paper = new PaperBook("Effective Java", "Joshua Bloch", 416, "9780134685991", new DateTime(2018, 1, 6), 45.99, 1.0);
        var ebook = new PayedEBook("Learning SQL", "Alan Beaulieu", 408, "9781492057611", new DateTime(2020, 3, 10), FileType.EPUB, 29.99);
        var demo = new DemoBook("Agile Principles Preview", "Robert C. Martin", 70, "9780135974445", new DateTime(2021, 7, 14), FileType.MOBI);

        inventory.AddBook(paper, 2);
        inventory.AddBook(ebook, 5);
        inventory.AddBook(demo, 100);

        inventory.ListInventory();
    }
}


public class Program
{
    public static void Main(string[] args)
    {
        Tests.RunAllTests();
    }
}


