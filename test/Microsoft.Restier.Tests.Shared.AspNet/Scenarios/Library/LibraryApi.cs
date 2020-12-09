// <copyright file="LibraryApi.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.
// </copyright>

namespace Microsoft.Restier.Tests.Shared.AspNet.Scenarios.Library
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using Microsoft.AspNet.OData;
    using Microsoft.AspNet.OData.Query;
    using Microsoft.Restier.AspNet.Model;
    using Microsoft.Restier.EntityFramework;
    using Microsoft.Restier.Tests.Shared.EntityFramework.Scenarios.Library;
    using Microsoft.Restier.Tests.Shared.Scenarios.Library;

    /// <summary>
    /// A testable API that implements an Entity Framework model and has secondary operations
    /// against a SQL 2017 LocalDB database.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class LibraryApi : EntityFrameworkApi<LibraryContext>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LibraryApi"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        public LibraryApi(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
        }

        /// <summary>
        /// Publishes a book.
        /// </summary>
        /// <param name="isActive">Whether the publish is active.</param>
        /// <returns>A new book.</returns>
        [Operation]
        public Book PublishBook(bool isActive)
        {
            Console.WriteLine($"IsActive = {isActive}");
            return new Book
            {
                Id = Guid.NewGuid(),
                Title = "The Cat in the Hat",
            };
        }

        /// <summary>
        /// Publish a book with a count.
        /// </summary>
        /// <param name="count">The count.</param>
        /// <returns>A new book.</returns>
        [Operation]
        public Book PublishBooks(int count)
        {
            Console.WriteLine($"Count = {count}");
            return new Book
            {
                Id = Guid.NewGuid(),
                Title = "The Cat in the Hat Comes Back",
            };
        }

        /// <summary>
        /// Submits a transaction using the specified id.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <returns>A new book.</returns>
        [Operation]
        public Book SubmitTransaction(Guid id)
        {
            Console.WriteLine($"Id = {id}");
            return new Book
            {
                Id = id,
                Title = "Atlas Shrugged",
            };
        }

        /// <summary>
        /// Checks out a book.
        /// </summary>
        /// <param name="book">The book.</param>
        /// <returns>The same book.</returns>
        [Operation(OperationType = OperationType.Action, EntitySet = "Books")]
        public Book CheckoutBook(Book book)
        {
            if (book == null)
            {
                throw new ArgumentNullException(nameof(book));
            }

            Console.WriteLine($"Id = {book.Id}");
            book.Title += " | Submitted";
            return book;
        }

        /// <summary>
        /// Returns a queryable of favorite books.
        /// </summary>
        /// <returns>A queryable.</returns>
        [Operation]
        [EnableQuery(AllowedQueryOptions = AllowedQueryOptions.All)]
        public IQueryable<Book> FavoriteBooks()
        {
            var publisher = new Publisher
            {
                Id = "123",
                Addr = new Address
                {
                    Street = "Publisher Way",
                    Zip = "12345",
                },
            };

            foreach (var book in new Book[]
            {
                new Book
                {
                    Id = Guid.NewGuid(),
                    Title = "The Cat in the Hat Comes Back",
                    Publisher = publisher,
                },
                new Book
                {
                    Id = Guid.NewGuid(),
                    Title = "If You Give a Mouse a Cookie",
                    Publisher = publisher,
                },
            })
            {
                publisher.Books.Add(book);
            }

            return publisher.Books.AsQueryable();
        }

        /// <summary>
        /// Gets a list of published books for a publisher.
        /// </summary>
        /// <param name="publisher">the publisher.</param>
        /// <returns>The books.</returns>
        [Operation(IsBound = true, IsComposable = true, EntitySet = "publisher/Books")]
        public IQueryable<Book> PublishedBooks(Publisher publisher)
        {
            return this.FavoriteBooks();
        }

        /// <summary>
        /// A method that discontinues a set of books.
        /// </summary>
        /// <param name="books">The books.</param>
        /// <returns>The same set of books, now discontinued.</returns>
        [Operation(IsBound = true, IsComposable = true)]
        public IQueryable<Book> DiscontinueBooks(IQueryable<Book> books)
        {
            if (books == null)
            {
                throw new ArgumentNullException(nameof(books));
            }

            books.ToList().ForEach(c =>
            {
                Console.WriteLine($"Id = {c.Id}");
                c.Title += " | Discontinued";
            });
            return books;
        }

        /// <summary>
        /// Method that gets executed before a book is discontinued.
        /// </summary>
        /// <param name="books">The books.</param>
        protected internal void OnExecutingDiscontinueBooks(IQueryable<Book> books)
        {
            books.ToList().ForEach(c =>
            {
                Console.WriteLine($"Id = {c.Id}");
                c.Title += " | Intercepted";
            });
        }

        /// <summary>
        /// Method that gets executed after a book is discontinued.
        /// </summary>
        /// <param name="books">The books.</param>
        protected internal void OnExecutedDiscontinueBooks(IQueryable<Book> books)
        {
            books.ToList().ForEach(c =>
            {
                Console.WriteLine($"Id = {c.Id}");
                c.Title += " | Intercepted";
            });
        }

        /// <summary>
        /// Method that checks whether the employee can be updated.
        /// </summary>
        /// <returns>Whether the employee can be updated.</returns>
        protected internal bool CanUpdateEmployee() => false;
    }
}