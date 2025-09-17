// See https://aka.ms/new-console-template for more information

using Microsoft.EntityFrameworkCore;
using OpenWorker.Persistence;

var factory = new PersistenceContextFactory();
await using var context = factory.CreateDbContext(args);
await context.Database.MigrateAsync();
await context.SaveChangesAsync();