using System;
using System.Linq;
using BillingBackend.Data;
using BillingBackend.Data.Entities;
using BillingBackend.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BillingBackend.Migrator
{
    /// <summary>
    /// Development-only database reset.
    ///
    /// The Entity Framework model (<see cref="BillingDbContext"/>) is the single source of
    /// truth: reset creates the schema with EnsureCreated (which includes the composite
    /// tenant keys, CHECK constraints and indexes declared in OnModelCreating) and then
    /// runs <see cref="DatabaseSchemaInitializer"/> (additive columns, feature seeds).
    /// Reference data below mirrors the HasData seeds in BillingDbContext.
    ///
    /// Production must use the versioned scripts in BillingBackend.Database/Migrations
    /// through the release pipeline - never this reset workflow.
    /// </summary>
    class Program
    {
        static int Main(string[] args)
        {
            Console.WriteLine("==================================================");
            Console.WriteLine("BillCom Local SQL Server DB Fresh Setup & Purge");
            Console.WriteLine("Target: SQL Server @ localhost (development only)");
            Console.WriteLine("==================================================");

            if (args.Contains("--print-ddl"))
            {
                var ddlConnectionString = GetArgValue(args, "--connection")
                    ?? Environment.GetEnvironmentVariable("MIGRATOR_CONNECTION")
                    ?? "Data Source=.;Initial Catalog=SmartBillingDb;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;";

                var ddlServices = new ServiceCollection();
                ddlServices.AddLogging();
                ddlServices.AddDbContext<BillingDbContext>(options =>
                    options.UseSqlServer(ddlConnectionString));

                // Drift-detection aid: prints the DDL implied by the EF model without
                // touching the database. Throws here on mapping misconfiguration.
                using var ddlProvider = ddlServices.BuildServiceProvider();
                using var ddlScope = ddlProvider.CreateScope();
                var ddlContext = ddlScope.ServiceProvider.GetRequiredService<BillingDbContext>();
                Console.WriteLine(ddlContext.Database.GenerateCreateScript());
                return 0;
            }

            if (!args.Contains("--reset-database"))
            {
                Console.Error.WriteLine("Refusing destructive reset. Use versioned SQL files in BillingBackend.Database/Migrations for deployments. --reset-database is development-only.");
                return 2;
            }

            var connectionString = GetArgValue(args, "--connection")
                ?? Environment.GetEnvironmentVariable("MIGRATOR_CONNECTION")
                ?? "Data Source=.;Initial Catalog=SmartBillingDb;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;";

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddDbContext<BillingDbContext>(options =>
                options.UseSqlServer(connectionString));

            var serviceProvider = services.BuildServiceProvider();

            bool dropOnly = args.Contains("--drop-only") || args.Contains("drop");

            using (var scope = serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<BillingDbContext>();
                try
                {
                    context.Database.EnsureCreated();

                    Console.WriteLine("\n[1/4] Purging all SQL Server tables, views, and constraints...");
                    context.Database.ExecuteSqlRaw(@"
                        -- Drop all foreign key constraints first
                        DECLARE @sql NVARCHAR(MAX) = N'';
                        SELECT @sql += 'ALTER TABLE ' + QUOTENAME(s.name) + '.' + QUOTENAME(t.name) + ' DROP CONSTRAINT ' + QUOTENAME(f.name) + ';' + CHAR(13)
                        FROM sys.foreign_keys f
                        INNER JOIN sys.tables t ON f.parent_object_id = t.object_id
                        INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
                        WHERE s.name = 'dbo';
                        EXEC sp_executesql @sql;

                        -- Drop all tables
                        SET @sql = N'';
                        SELECT @sql += 'DROP TABLE ' + QUOTENAME(s.name) + '.' + QUOTENAME(t.name) + ';' + CHAR(13)
                        FROM sys.tables t
                        INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
                        WHERE s.name = 'dbo';
                        EXEC sp_executesql @sql;

                        -- Drop all views
                        SET @sql = N'';
                        SELECT @sql += 'DROP VIEW ' + QUOTENAME(s.name) + '.' + QUOTENAME(v.name) + ';' + CHAR(13)
                        FROM sys.views v
                        INNER JOIN sys.schemas s ON v.schema_id = s.schema_id
                        WHERE s.name = 'dbo';
                        EXEC sp_executesql @sql;

                        -- Drop all sequences
                        SET @sql = N'';
                        SELECT @sql += 'DROP SEQUENCE ' + QUOTENAME(s.name) + '.' + QUOTENAME(sq.name) + ';' + CHAR(13)
                        FROM sys.sequences sq
                        INNER JOIN sys.schemas s ON sq.schema_id = s.schema_id
                        WHERE s.name = 'dbo';
                        EXEC sp_executesql @sql;");
                    Console.WriteLine("Purged all SQL Server tables, views, sequences, and constraints cleanly.");

                    if (dropOnly)
                    {
                        Console.WriteLine("\n==================================================");
                        Console.WriteLine("SUCCESS: All SQL Server database tables completely purged!");
                        Console.WriteLine("==================================================");
                        return 0;
                    }

                    Console.WriteLine("\n[2/4] Creating schema from the Entity Framework model...");
                    context.Database.EnsureCreated();
                    Console.WriteLine("Schema created from the EF model (tenant keys, checks, indexes included).");

                    Console.WriteLine("\n[3/4] Running additive schema verification and feature seeds...");
                    DatabaseSchemaInitializer.EnsureDatabaseSchemaUpdated(serviceProvider);
                    Console.WriteLine("Schema verification completed.");

                    Console.WriteLine("\n[4/4] Seeding reference data (plans, tax, HSN/SAC, business types)...");
                    SeedReferenceData(context);
                    Console.WriteLine("Reference data seeded successfully.");

                    Console.WriteLine("\n[Verification] Tables and seed counts:");
                    using (var cmd = context.Database.GetDbConnection().CreateCommand())
                    {
                        if (cmd.Connection!.State != System.Data.ConnectionState.Open)
                            cmd.Connection.Open();
                        cmd.CommandText = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'";
                        Console.WriteLine($"  Tables: {cmd.ExecuteScalar()}");
                        cmd.CommandText = "SELECT COUNT(*) FROM [SubscriptionPlans]";
                        Console.WriteLine($"  SubscriptionPlans: {cmd.ExecuteScalar()}");
                        cmd.CommandText = "SELECT COUNT(*) FROM [TaxCategories]";
                        Console.WriteLine($"  TaxCategories: {cmd.ExecuteScalar()}");
                        cmd.CommandText = "SELECT COUNT(*) FROM [AppFeatures]";
                        Console.WriteLine($"  AppFeatures: {cmd.ExecuteScalar()}");
                    }

                    Console.WriteLine("\n==================================================");
                    Console.WriteLine("SUCCESS: SQL Server Database schema & seeds ready!");
                    Console.WriteLine("==================================================");
                    return 0;
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"\n[ERROR] Migration failed: {ex.Message}");
                    Console.WriteLine(ex.StackTrace);
                    Console.ResetColor();
                    return 1;
                }
            }
        }

        private static string? GetArgValue(string[] args, string name)
        {
            for (int i = 0; i + 1 < args.Length; i++)
            {
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
                    return args[i + 1];
            }
            return null;
        }

        /// <summary>
        /// Values mirror the HasData seeds in BillingDbContext. Only fills empty tables,
        /// so re-runs are safe. Explicit IDs require IDENTITY_INSERT on the same session,
        /// so the connection is held open for the whole seeding block.
        /// </summary>
        private static void SeedReferenceData(BillingDbContext context)
        {
            var conn = context.Database.GetDbConnection();
            bool wasClosed = conn.State != System.Data.ConnectionState.Open;
            if (wasClosed) conn.Open();
            try
            {
                SeedPlans(context);
                SeedTaxCategories(context);
                SeedHsn(context);
                SeedSac(context);
                SeedBusinessTypes(context);
            }
            finally
            {
                if (wasClosed && conn.State != System.Data.ConnectionState.Closed) conn.Close();
            }
        }

        private static void WithIdentityInsert(BillingDbContext context, string table, Action seed)
        {
            context.Database.ExecuteSqlRaw($"SET IDENTITY_INSERT [{table}] ON;");
            try
            {
                seed();
            }
            finally
            {
                context.Database.ExecuteSqlRaw($"SET IDENTITY_INSERT [{table}] OFF;");
            }
        }

        private static void SeedPlans(BillingDbContext context)
        {
            if (context.SubscriptionPlans.Any()) return;
            WithIdentityInsert(context, "SubscriptionPlans", () =>
            {
                context.SubscriptionPlans.AddRange(
                    new SubscriptionPlan
                    {
                        Id = 1,
                        Name = "Starter Shop",
                        Subtitle = "Ideal for Single Kirana, Small Cafes & Standalone Stores",
                        RazorpayPlanIdMonthly = "plan_starter_monthly",
                        RazorpayPlanIdYearly = "plan_starter_yearly",
                        MonthlyPrice = 499.00m,
                        YearlyPrice = 4999.00m,
                        MaxBranches = 1,
                        MaxStaff = 2,
                        IsPopular = false,
                        DisplayOrder = 1,
                        FeaturesJson = """[{"text":"Single Store & Counter POS","included":true},{"text":"2 Cashier Staff Accounts","included":true},{"text":"Thermal & A4 Tax Invoice Printing","included":true},{"text":"Customer Udhar Khata Ledger","included":true},{"text":"Stock Warning Alerts","included":true},{"text":"Multi-Branch Franchise Sync","included":false},{"text":"Stylist Commission Calculator","included":false}]""",
                        IsActive = true
                    },
                    new SubscriptionPlan
                    {
                        Id = 2,
                        Name = "Growth Business",
                        Subtitle = "Perfect for High-Volume Retailers, Salons & Restaurants",
                        RazorpayPlanIdMonthly = "plan_growth_monthly",
                        RazorpayPlanIdYearly = "plan_growth_yearly",
                        MonthlyPrice = 999.00m,
                        YearlyPrice = 9999.00m,
                        MaxBranches = 3,
                        MaxStaff = 10,
                        IsPopular = true,
                        DisplayOrder = 2,
                        FeaturesJson = """[{"text":"Up to 3 Store Outlets","included":true},{"text":"10 Staff Accounts & Role Controls","included":true},{"text":"Automated DLT SMS Receipts","included":true},{"text":"Barcode & Electronic Scale Integration","included":true},{"text":"Kitchen KOT & Table Layouts","included":true},{"text":"GST E-Invoicing & Tally Prime Sync","included":true},{"text":"Operating Expense & Profit Tracker","included":true}]""",
                        IsActive = true
                    },
                    new SubscriptionPlan
                    {
                        Id = 3,
                        Name = "Enterprise Chain",
                        Subtitle = "Custom Architecture for Large Multi-City Franchises",
                        RazorpayPlanIdMonthly = "plan_enterprise_monthly",
                        RazorpayPlanIdYearly = "plan_enterprise_yearly",
                        MonthlyPrice = 2499.00m,
                        YearlyPrice = 24999.00m,
                        MaxBranches = 25,
                        MaxStaff = 50,
                        IsPopular = false,
                        DisplayOrder = 3,
                        FeaturesJson = """[{"text":"Unlimited Outlets & Central Warehouse","included":true},{"text":"50 Staff Accounts with Role Controls","included":true},{"text":"Dedicated Account Manager & 24/7 SLA","included":true},{"text":"Custom ERP & Tally 2-Way Sync","included":true},{"text":"Multi-Branch Royalty & P&L Analytics","included":true},{"text":"High-Throughput Exotel DLT SMS","included":true}]""",
                        IsActive = true
                    });
                context.SaveChanges();
            });
        }

        private static void SeedTaxCategories(BillingDbContext context)
        {
            if (context.TaxCategories.Any()) return;
            WithIdentityInsert(context, "TaxCategories", () =>
            {
                context.TaxCategories.AddRange(
                    new TaxCategory { Id = 1, BusinessId = null, Name = "Standard Goods (18%)", TaxType = "Goods", HSNCode = "9999", GSTPercentage = 18.00m, CGSTPercentage = 9.00m, SGSTPercentage = 9.00m, IGSTPercentage = 18.00m, CessPercentage = 0.00m },
                    new TaxCategory { Id = 2, BusinessId = null, Name = "Reduced Goods (5%)", TaxType = "Goods", HSNCode = "1001", GSTPercentage = 5.00m, CGSTPercentage = 2.50m, SGSTPercentage = 2.50m, IGSTPercentage = 5.00m, CessPercentage = 0.00m },
                    new TaxCategory { Id = 3, BusinessId = null, Name = "Essential / Exempt (0%)", TaxType = "Goods", HSNCode = "0000", GSTPercentage = 0.00m, CGSTPercentage = 0.00m, SGSTPercentage = 0.00m, IGSTPercentage = 0.00m, CessPercentage = 0.00m },
                    new TaxCategory { Id = 4, BusinessId = null, Name = "Luxury Goods (28% + Cess)", TaxType = "Goods", HSNCode = "8703", GSTPercentage = 28.00m, CGSTPercentage = 14.00m, SGSTPercentage = 14.00m, IGSTPercentage = 28.00m, CessPercentage = 12.00m },
                    new TaxCategory { Id = 5, BusinessId = null, Name = "Restaurant Service (5%)", TaxType = "Services", SACCode = "996331", GSTPercentage = 5.00m, CGSTPercentage = 2.50m, SGSTPercentage = 2.50m, IGSTPercentage = 5.00m, CessPercentage = 0.00m },
                    new TaxCategory { Id = 6, BusinessId = null, Name = "IT & Professional Services (18%)", TaxType = "Services", SACCode = "998313", GSTPercentage = 18.00m, CGSTPercentage = 9.00m, SGSTPercentage = 9.00m, IGSTPercentage = 18.00m, CessPercentage = 0.00m },
                    new TaxCategory { Id = 7, BusinessId = null, Name = "Personal Care / Salon (18%)", TaxType = "Services", SACCode = "999721", GSTPercentage = 18.00m, CGSTPercentage = 9.00m, SGSTPercentage = 9.00m, IGSTPercentage = 18.00m, CessPercentage = 0.00m });
                context.SaveChanges();
            });
        }

        private static void SeedHsn(BillingDbContext context)
        {
            if (context.HSNMasters.Any()) return;
            WithIdentityInsert(context, "HSNMasters", () =>
            {
                context.HSNMasters.AddRange(
                    new HSNMaster { Id = 1, Code = "6109", Description = "T-shirts, singlets and other vests, knitted or crocheted", SearchTerms = "tshirt t-shirt shirt clothing garment apparel", UQC = "PCS", DefaultGSTPercentage = 5.00m },
                    new HSNMaster { Id = 2, Code = "1006", Description = "Rice, husk rice, husked rice, semi-milled or wholly milled rice", SearchTerms = "rice grocery grain kirana food basmati", UQC = "KGS", DefaultGSTPercentage = 5.00m },
                    new HSNMaster { Id = 3, Code = "1701", Description = "Cane or beet sugar and chemically pure sucrose, in solid form", SearchTerms = "sugar grocery sweet kirana", UQC = "KGS", DefaultGSTPercentage = 5.00m },
                    new HSNMaster { Id = 4, Code = "8517", Description = "Telephone sets, including smartphones and other apparatus for cellular networks", SearchTerms = "mobile phone smartphone electronics laptop computer accessory", UQC = "PCS", DefaultGSTPercentage = 18.00m },
                    new HSNMaster { Id = 5, Code = "3004", Description = "Medicaments consisting of mixed or unmixed products for therapeutic uses", SearchTerms = "medicine tablet capsule pharmacy medical drug doctor syrup", UQC = "BOX", DefaultGSTPercentage = 12.00m },
                    new HSNMaster { Id = 6, Code = "1905", Description = "Bread, pastry, cakes, biscuits and other bakers wares", SearchTerms = "bakery bread biscuit cake pastry cookies snack", UQC = "PCS", DefaultGSTPercentage = 18.00m });
                context.SaveChanges();
            });
        }

        private static void SeedSac(BillingDbContext context)
        {
            if (context.SACMasters.Any()) return;
            WithIdentityInsert(context, "SACMasters", () =>
            {
                context.SACMasters.AddRange(
                    new SACMaster { Id = 1, Code = "996331", Description = "Services provided by restaurants, cafes and other similar eating establishments", SearchTerms = "restaurant food idly dosa coffee mess tiffin dining cloud kitchen hotel", DefaultGSTPercentage = 5.00m },
                    new SACMaster { Id = 2, Code = "998313", Description = "Information technology (IT) design and development services for applications", SearchTerms = "software web development IT services consulting computer programming website app", DefaultGSTPercentage = 18.00m },
                    new SACMaster { Id = 3, Code = "999721", Description = "Hairdressing and beauty treatment services, salon, spa and personal care", SearchTerms = "salon barber haircut beauty parlour spa facial styling makeup massage", DefaultGSTPercentage = 18.00m },
                    new SACMaster { Id = 4, Code = "998713", Description = "Maintenance and repair services of computers, mobile phones and consumer electronics", SearchTerms = "repair maintenance laptop computer fixing service mobile hardware", DefaultGSTPercentage = 18.00m });
                context.SaveChanges();
            });
        }

        private static void SeedBusinessTypes(BillingDbContext context)
        {
            if (context.BusinessTypeMasters.Any()) return;
                context.Database.ExecuteSqlRaw(@"
                    SET IDENTITY_INSERT [BusinessTypeMasters] ON;
                    INSERT INTO [BusinessTypeMasters] ([Id], [Code], [Name], [Category], [IconName], [SellingModel], [AliasesJson], [DefaultFeaturesJson], [DefaultTerminologyJson], [IsActive], [CreatedAt])
                    VALUES (1, 'restaurant', 'Restaurant', 'Food & Hospitality', 'Utensils', 'GOODS_AND_SERVICES', '[""hotel"",""dine in"",""eatery"",""food court"",""dhaba""]', '{""products"":true,""services"":false,""inventory"":true,""appointments"":false,""customers"":true,""staff"":true,""khata"":true,""purchases"":true,""expenses"":true}', '{""product"":{""singular"":""Menu Item"",""plural"":""Menu Items""},""service"":{""singular"":""Dining Service"",""plural"":""Dining Services""},""customer"":{""singular"":""Customer"",""plural"":""Customers""},""invoice"":{""singular"":""Order / Bill"",""plural"":""Orders / Bills""},""inventory"":{""singular"":""Ingredient Stock"",""plural"":""Ingredient Stock""},""purchase"":{""singular"":""Ingredient Purchase"",""plural"":""Ingredient Purchases""},""supplier"":{""singular"":""Vendor / Supplier"",""plural"":""Vendors & Suppliers""},""staff"":{""singular"":""Staff / Waiter"",""plural"":""Staff & Waiters""}}', 1, GETUTCDATE());
                    SET IDENTITY_INSERT [BusinessTypeMasters] OFF;");

                context.Database.ExecuteSqlRaw(@"
                    SET IDENTITY_INSERT [BusinessTypeMasters] ON;
                    INSERT INTO [BusinessTypeMasters] ([Id], [Code], [Name], [Category], [IconName], [SellingModel], [AliasesJson], [DefaultFeaturesJson], [DefaultTerminologyJson], [IsActive], [CreatedAt])
                    VALUES (2, 'tiffin_center', 'Tiffin Center / Mess', 'Food & Hospitality', 'Soup', 'GOODS_ONLY', '[""mess"",""tiffin"",""canteen"",""fast food"",""food stall"",""tiffin service""]', '{""products"":true,""services"":false,""inventory"":false,""appointments"":false,""customers"":true,""staff"":false,""khata"":true,""purchases"":true,""expenses"":true}', '{""product"":{""singular"":""Food Item"",""plural"":""Food Items""},""customer"":{""singular"":""Customer"",""plural"":""Customers""},""invoice"":{""singular"":""Bill"",""plural"":""Bills""}}', 1, GETUTCDATE());
                    SET IDENTITY_INSERT [BusinessTypeMasters] OFF;");

                context.Database.ExecuteSqlRaw(@"
                    SET IDENTITY_INSERT [BusinessTypeMasters] ON;
                    INSERT INTO [BusinessTypeMasters] ([Id], [Code], [Name], [Category], [IconName], [SellingModel], [AliasesJson], [DefaultFeaturesJson], [DefaultTerminologyJson], [IsActive], [CreatedAt])
                    VALUES (3, 'grocery_kirana', 'Grocery / Kirana Store', 'Retail', 'ShoppingCart', 'GOODS_ONLY', '[""kirana"",""grocery"",""provision store"",""super market"",""general store"",""departmental store""]', '{""products"":true,""services"":false,""inventory"":true,""appointments"":false,""customers"":true,""staff"":true,""khata"":true,""purchases"":true,""expenses"":true}', '{""product"":{""singular"":""Product"",""plural"":""Products""},""customer"":{""singular"":""Customer"",""plural"":""Customers""},""invoice"":{""singular"":""Bill"",""plural"":""Bills""},""inventory"":{""singular"":""Stock"",""plural"":""Stock & Inventory""}}', 1, GETUTCDATE());
                    SET IDENTITY_INSERT [BusinessTypeMasters] OFF;");

                context.Database.ExecuteSqlRaw(@"
                    SET IDENTITY_INSERT [BusinessTypeMasters] ON;
                    INSERT INTO [BusinessTypeMasters] ([Id], [Code], [Name], [Category], [IconName], [SellingModel], [AliasesJson], [DefaultFeaturesJson], [DefaultTerminologyJson], [IsActive], [CreatedAt])
                    VALUES (4, 'general_retail', 'General Retail Store', 'Retail', 'Store', 'GOODS_AND_SERVICES', '[""general"",""other"",""retail"",""shop""]', '{""products"":true,""services"":true,""inventory"":true,""appointments"":false,""customers"":true,""staff"":true,""khata"":true,""purchases"":true,""expenses"":true}', '{""product"":{""singular"":""Product"",""plural"":""Products""},""customer"":{""singular"":""Customer"",""plural"":""Customers""},""invoice"":{""singular"":""Bill / Invoice"",""plural"":""Bills & Invoices""}}', 1, GETUTCDATE());
                    SET IDENTITY_INSERT [BusinessTypeMasters] OFF;");
        }
    }
}
