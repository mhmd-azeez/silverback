﻿using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Silverback.Domain;
using Silverback.Extensions;
using Silverback.Messaging.Publishing;

namespace Silverback.EntityFrameworkCore
{
    /// <summary>
    /// Exposes some extension methods for the <see cref="DbContext"/> that handle domain events as part 
    /// of the SaveChanges transaction.
    /// </summary>
    public static class DbContextEventPublisherExtensions
    {
        /// <summary>
        /// Publishes the domain events generated by the tracked entities and then calls the regular <see cref="DbContext.SaveChanges(bool)"/> methods.
        /// </summary>
        /// <param name="dbContext">The <see cref="DbContext"/> tracking the entities that must publish the events.</param>
        /// <param name="eventPublisher">The event publisher.</param>
        /// <param name="acceptAllChangesOnSuccess">
        ///     Indicates whether <see cref="Microsoft.EntityFrameworkCore.ChangeTracking.ChangeTracker.AcceptAllChanges" /> is called after the changes have
        ///     been sent successfully to the database.
        /// </param>
        /// <returns></returns>
        public static int SaveChanges(this DbContext dbContext, IEventPublisher<IDomainEvent<IDomainEntity>> eventPublisher, bool acceptAllChangesOnSuccess = true)
        {
            var events = GetPendingEvents(dbContext);

            // Keep publishing events that are fired from inside the event handlers
            while (events.Any())
            {
                events.ForEach(eventPublisher.Publish);
                events = GetPendingEvents(dbContext);
            }

            return dbContext.SaveChanges(acceptAllChangesOnSuccess);
        }

        /// <summary>
        /// Publishes the domain events generated by the tracked entities and then calls the regular <see cref="DbContext.SaveChangesAsync(bool, System.Threading.CancellationToken)"/> methods.
        /// </summary>
        /// <param name="dbContext">The <see cref="DbContext"/> tracking the entities that must publish the events.</param>
        /// <param name="eventPublisher">The event publisher.</param>
        /// <param name="acceptAllChangesOnSuccess">
        ///     Indicates whether <see cref="Microsoft.EntityFrameworkCore.ChangeTracking.ChangeTracker.AcceptAllChanges" /> is called after the changes have
        ///     been sent successfully to the database.
        /// </param>
        /// <param name="cancellationToken">A <see cref="T:System.Threading.CancellationToken" /> to observe while waiting for the task to complete.</param>
        /// <returns></returns>
        public static async Task<int> SaveChangesAsync(this DbContext dbContext, IEventPublisher<IDomainEvent<IDomainEntity>> eventPublisher, bool acceptAllChangesOnSuccess = true, CancellationToken cancellationToken = default)
        {
            var events = GetPendingEvents(dbContext);

            // Keep publishing events fired inside the event handlers
            while (events.Any())
            {
                await events.ForEachAsync(eventPublisher.PublishAsync);
                events = GetPendingEvents(dbContext);
            }

            return await dbContext.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        /// <summary>
        /// Gets the events to be published.
        /// </summary>
        /// <returns></returns>
        private static List<IDomainEvent<IDomainEntity>> GetPendingEvents(DbContext dbContext)
        {
            var events = dbContext.ChangeTracker.Entries<IDomainEntity>()
                .Where(e => e.Entity.GetDomainEvents() != null)
                .SelectMany(e => e.Entity.GetDomainEvents())
                .ToList();

            // Clear all events to avoid firing the same event multiple times during the recursion
            events.ForEach(e => e.Source.ClearEvents());

            return events;
        }
    }
}
