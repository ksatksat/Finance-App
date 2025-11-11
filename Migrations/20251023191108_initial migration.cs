using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceApp.Migrations
{
    /// <inheritdoc />
    public partial class initialmigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Expenses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Amount = table.Column<double>(type: "float", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Expenses", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Expenses");
        }
    }
}
//остановился на 19:27
/*
 * this is an Entity Framework Core migration class that was generated for you. I’ll explain what each part does, what SQL it corresponds to, why EF generated those types, and a few practical notes & best-practices (especially about money types and how to change the migration if needed).

High-level purpose

A migration class describes one step of schema change. The Up method applies the change (creates the table), and the Down method undoes it (drops the table). When you run Update-Database (or dotnet ef database update), EF executes the Up methods of pending migrations against your database.

File structure — top-to-bottom
#nullable disable
namespace FinanceApp.Migrations
{
    public partial class initialmigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable( ... );
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "Expenses");
        }
    }
}


public partial class initialmigration : Migration
The generated migration class inherits from Migration. The class is partial because EF sometimes generates a designer file/snapshot alongside it.

#nullable disable
Tells the C# compiler that nullable reference type warnings are disabled for this file (EF generator emits this to avoid nullability warnings in generated code).

Up(MigrationBuilder migrationBuilder)
This method contains the operations EF will run to apply the migration. Here it creates a new table Expenses.

Down(MigrationBuilder migrationBuilder)
This method should reverse Up. EF will execute it if you roll back (migrate down).

The CreateTable call — details
migrationBuilder.CreateTable(
    name: "Expenses",
    columns: table => new
    {
        Id = table.Column<int>(type: "int", nullable: false)
            .Annotation("SqlServer:Identity", "1, 1"),
        Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
        Amount = table.Column<double>(type: "float", nullable: false),
        Category = table.Column<string>(type: "nvarchar(max)", nullable: false),
        Date = table.Column<DateTime>(type: "datetime2", nullable: false)
    },
    constraints: table =>
    {
        table.PrimaryKey("PK_Expenses", x => x.Id);
    });


name: "Expenses" — the DB table name.

columns: table => new { ... } — column definitions. Each table.Column<T> call specifies:

the CLR type (int, string, double, DateTime),

SQL data type as EF chose ("int", "nvarchar(max)", "float", "datetime2"),

nullable: false means the column is NOT NULL.

Id column has .Annotation("SqlServer:Identity", "1, 1") — this makes Id an IDENTITY column (auto-increment starting at 1, increment by 1).

constraints defines the primary key: PK_Expenses on Id.

Equivalent SQL

This migration will produce SQL similar to:

CREATE TABLE [Expenses] (
  [Id] INT IDENTITY(1,1) NOT NULL,
  [Description] nvarchar(max) NOT NULL,
  [Amount] float NOT NULL,
  [Category] nvarchar(max) NOT NULL,
  [Date] datetime2 NOT NULL,
  CONSTRAINT [PK_Expenses] PRIMARY KEY ([Id])
);


You can view the exact SQL EF would generate with:

dotnet ef migrations script --idempotent


or generate a script for a particular migration.

Practical notes & suggestions
1) Money — prefer decimal over double

EF mapped your Amount CLR type double → SQL float. double/float is binary floating-point and can introduce rounding errors for currency.

Recommended: use decimal in your model:

public decimal Amount { get; set; }


and then regenerate a migration (or edit this migration) so SQL column uses e.g. decimal(18,2):

Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false)


Best practice: change your model first, then scaffold a new migration.

2) String columns & lengths

EF created nvarchar(max) for Description and Category. For performance and validation you may prefer nvarchar(200) or apply [StringLength(200)] on the model so EF generates a constrained column.

3) Date column

DateTime maps to datetime2. Consider DateTimeOffset if you care about time zone/offset.

4) Editing a migration

If you need to change the generated migration (e.g., change float → decimal) you can edit this generated file before applying it. But long-term best practice: change the model and create a new migration so the model snapshot stays correct.

5) Applying / rolling back

Apply migrations to DB:

dotnet ef database update


Roll back to previous migration:

dotnet ef database update <previousMigrationName>


or run Update-Database in Package Manager Console.

6) Model snapshot

EF also maintains a snapshot file (YourContextModelSnapshot.cs) that represents the current model shape EF knows about. The snapshot is used to compare changes when you scaffold future migrations.

Why migrations matter (short recap)

They let you version schema changes, apply them across environments, and keep schema in sync with your C# model code — safely and repeatably.
 */