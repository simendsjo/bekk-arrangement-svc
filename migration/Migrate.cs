using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace migrator
{
    public static class Migrate
    {
        public static void Run(string connectionString)
        {

            var migrations = new List<Migration>
            {
                new Migration("CreateEvent",
                    "CREATE TABLE [Events] " +
                    "(Id INT PRIMARY KEY IDENTITY(1,1) NOT NULL," +
                    "Title VARCHAR(255) NOT NULL," +
                    "Description TEXT NULL," +
                    "Location VARCHAR(255) NOT NULL," +
                    "FromDate datetimeoffset(3) NOT NULL," +
                    "ToDate datetimeoffset(3) NOT NULL," +
                    "ResponsibleEmployee INT NOT NULL,);"),
            };

            var distinctMigrationsNames = migrations.Select(m => m.Name).Distinct();
            if (distinctMigrationsNames.Count() != migrations.Count())
            {
                throw new Exception("Du har to migrasjoner med samme navn");
            } 


            var list = new List<string>();

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                try
                {
                    using (var command = new SqlCommand("SELECT * FROM Migrations", connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    var t = reader.GetString(0);
                                    list.Add(t);
                                }
                            }
                        }
                    }

                    var notMigratedMigrations = migrations.Where(m => list.All(l => l != m.Name)).ToList();
                    Console.WriteLine($"There are {notMigratedMigrations.Count()} pending migrations");


                    foreach (var migration in notMigratedMigrations)
                    {
                        var command = connection.CreateCommand();
                        var transaction = connection.BeginTransaction(migration.Name);
                        command.Connection = connection;
                        command.Transaction = transaction;
                        try
                        {
                            command.CommandText = migration.Query;
                            command.ExecuteNonQuery();
                            command.CommandText = $"INSERT INTO Migrations (Name) VALUES ('{migration.Name}')";
                            command.ExecuteNonQuery();
                            transaction.Commit();
                            Console.WriteLine($"Added {migration.Name} to migrations table");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Commit Exception Type: {0}", ex.GetType());
                            Console.WriteLine("  Message: {0}", ex.Message);

                            try
                            {
                                transaction.Rollback();
                            }
                            catch (Exception ex2)
                            {
                                Console.WriteLine("Rollback Exception Type: {0}", ex2.GetType());
                                Console.WriteLine("  Message: {0}", ex2.Message);
                            }
                        }
   
                    }
                }
                catch(Exception e)
                {
                    Console.WriteLine(e.Message);
                    throw;
                }
                Console.WriteLine("Done.");
            }
        }
    }

 
}
