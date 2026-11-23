IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122095506_InitialCreate'
)
BEGIN
    CREATE TABLE [AdminAuditLogs] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(450) NULL,
        [UserName] nvarchar(256) NULL,
        [Action] nvarchar(100) NOT NULL,
        [EntityType] nvarchar(100) NOT NULL,
        [EntityId] nvarchar(100) NULL,
        [Details] nvarchar(4000) NULL,
        [IpAddress] varchar(45) NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_AdminAuditLogs] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122095506_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetRoles] (
        [Id] nvarchar(450) NOT NULL,
        [Name] nvarchar(256) NULL,
        [NormalizedName] nvarchar(256) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122095506_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUsers] (
        [Id] nvarchar(450) NOT NULL,
        [FullName] nvarchar(max) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UserName] nvarchar(256) NULL,
        [NormalizedUserName] nvarchar(256) NULL,
        [Email] nvarchar(256) NULL,
        [NormalizedEmail] nvarchar(256) NULL,
        [EmailConfirmed] bit NOT NULL,
        [PasswordHash] nvarchar(max) NULL,
        [SecurityStamp] nvarchar(max) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        [PhoneNumber] nvarchar(256) NULL,
        [PhoneNumberConfirmed] bit NOT NULL,
        [TwoFactorEnabled] bit NOT NULL,
        [LockoutEnd] datetimeoffset NULL,
        [LockoutEnabled] bit NOT NULL,
        [AccessFailedCount] int NOT NULL,
        CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122095506_InitialCreate'
)
BEGIN
    CREATE TABLE [ContactMessages] (
        [Id] int NOT NULL IDENTITY,
        [FullName] nvarchar(100) NOT NULL,
        [Mobile] varchar(11) NOT NULL,
        [Email] nvarchar(150) NULL,
        [Message] nvarchar(2000) NOT NULL,
        [IsRead] bit NOT NULL,
        [IpAddress] varchar(45) NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_ContactMessages] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122095506_InitialCreate'
)
BEGIN
    CREATE TABLE [CourseCategories] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(100) NOT NULL,
        [Slug] nvarchar(120) NOT NULL,
        [SortOrder] int NOT NULL,
        CONSTRAINT [PK_CourseCategories] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122095506_InitialCreate'
)
BEGIN
    CREATE TABLE [FaqItems] (
        [Id] int NOT NULL IDENTITY,
        [Question] nvarchar(300) NOT NULL,
        [Answer] nvarchar(3000) NOT NULL,
        [SortOrder] int NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_FaqItems] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122095506_InitialCreate'
)
BEGIN
    CREATE TABLE [FeatureItems] (
        [Id] int NOT NULL IDENTITY,
        [Title] nvarchar(150) NOT NULL,
        [Description] nvarchar(500) NULL,
        [Icon] nvarchar(50) NOT NULL,
        [SortOrder] int NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_FeatureItems] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122095506_InitialCreate'
)
BEGIN
    CREATE TABLE [Instructors] (
        [Id] int NOT NULL IDENTITY,
        [FullName] nvarchar(150) NOT NULL,
        [Title] nvarchar(200) NULL,
        [Bio] nvarchar(4000) NULL,
        [PhotoPath] nvarchar(300) NULL,
        [IsActive] bit NOT NULL,
        [ShowOnHomePage] bit NOT NULL,
        [SortOrder] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_Instructors] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122095506_InitialCreate'
)
BEGIN
    CREATE TABLE [Orders] (
        [Id] int NOT NULL IDENTITY,
        [OrderNumber] nvarchar(20) NOT NULL,
        [PublicId] uniqueidentifier NOT NULL,
        [CustomerName] nvarchar(100) NOT NULL,
        [Mobile] varchar(11) NOT NULL,
        [Email] nvarchar(150) NULL,
        [CustomerNote] nvarchar(1000) NULL,
        [ShippingAddress] nvarchar(500) NULL,
        [PostalCode] nvarchar(20) NULL,
        [TotalAmount] bigint NOT NULL,
        [Status] int NOT NULL,
        [PaidAt] datetime2 NULL,
        [TrackingCode] nvarchar(50) NULL,
        [AdminNote] nvarchar(4000) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_Orders] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Orders_TotalAmount] CHECK ([TotalAmount] >= 0)
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122095506_InitialCreate'
)
BEGIN
    CREATE TABLE [ProductCategories] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(100) NOT NULL,
        [Slug] nvarchar(120) NOT NULL,
        [SortOrder] int NOT NULL,
        CONSTRAINT [PK_ProductCategories] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122095506_InitialCreate'
)
BEGIN
    CREATE TABLE [SiteSettings] (
        [Id] int NOT NULL,
        [SiteName] nvarchar(100) NOT NULL,
        [Tagline] nvarchar(200) NULL,
        [HeroTitle] nvarchar(200) NULL,
        [HeroText] nvarchar(1000) NULL,
        [LogoPath] nvarchar(300) NULL,
        [HeroImagePath] nvarchar(300) NULL,
        [Phone] nvarchar(30) NULL,
        [SupportMobile] nvarchar(30) NULL,
        [Email] nvarchar(150) NULL,
        [Address] nvarchar(500) NULL,
        [WorkingHours] nvarchar(200) NULL,
        [AboutText] nvarchar(4000) NULL,
        [FooterText] nvarchar(1000) NULL,
        [TermsText] nvarchar(max) NULL,
        [Instagram] nvarchar(300) NULL,
        [Telegram] nvarchar(300) NULL,
        [WhatsApp] nvarchar(300) NULL,
        [LinkedIn] nvarchar(300) NULL,
        [Aparat] nvarchar(300) NULL,
        [EnamadHtmlCode] nvarchar(2000) NULL,
        [PaymentEnabled] bit NOT NULL,
        [UseSandbox] bit NOT NULL,
        [PaymentDescription] nvarchar(200) NOT NULL,
        [SeatReservationMinutes] int NOT NULL,
        [ShowInstructorsOnHome] bit NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_SiteSettings] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_SiteSettings_Singleton] CHECK ([Id] = 1)
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122095506_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetRoleClaims] (
        [Id] int NOT NULL IDENTITY,
        [RoleId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122095506_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUserClaims] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122095506_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUserLogins] (
        [LoginProvider] nvarchar(128) NOT NULL,
        [ProviderKey] nvarchar(128) NOT NULL,
        [ProviderDisplayName] nvarchar(max) NULL,
        [UserId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
        CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122095506_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUserPasskeys] (
        [CredentialId] varbinary(1024) NOT NULL,
        [UserId] nvarchar(450) NOT NULL,
        [Data] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_AspNetUserPasskeys] PRIMARY KEY ([CredentialId]),
        CONSTRAINT [FK_AspNetUserPasskeys_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122095506_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUserRoles] (
        [UserId] nvarchar(450) NOT NULL,
        [RoleId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
        CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122095506_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUserTokens] (
        [UserId] nvarchar(450) NOT NULL,
        [LoginProvider] nvarchar(128) NOT NULL,
        [Name] nvarchar(128) NOT NULL,
        [Value] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
        CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122095506_InitialCreate'
)
BEGIN
    CREATE TABLE [Courses] (
        [Id] int NOT NULL IDENTITY,
        [Title] nvarchar(200) NOT NULL,
        [Slug] nvarchar(150) NOT NULL,
        [ShortDescription] nvarchar(500) NULL,
        [Description] nvarchar(max) NULL,
        [Syllabus] nvarchar(max) NULL,
        [Prerequisites] nvarchar(4000) NULL,
        [RegistrationTerms] nvarchar(4000) NULL,
        [CoverImagePath] nvarchar(300) NULL,
        [CategoryId] int NULL,
        [InstructorId] int NULL,
        [Duration] nvarchar(100) NULL,
        [StartDate] date NULL,
        [EndDate] date NULL,
        [RegistrationStartsOn] date NULL,
        [RegistrationEndsOn] date NULL,
        [DeliveryMode] int NOT NULL,
        [Location] nvarchar(500) NULL,
        [OnlinePlatform] nvarchar(200) NULL,
        [SupportPhone] nvarchar(30) NULL,
        [Capacity] int NULL,
        [Price] bigint NOT NULL,
        [DiscountPrice] bigint NULL,
        [Status] int NOT NULL,
        [IsFeatured] bit NOT NULL,
        [AllowDuplicateEnrollment] bit NOT NULL,
        [SortOrder] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_Courses] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Courses_Capacity] CHECK ([Capacity] IS NULL OR [Capacity] > 0),
        CONSTRAINT [CK_Courses_DiscountPrice] CHECK ([DiscountPrice] IS NULL OR [DiscountPrice] >= 0),
        CONSTRAINT [CK_Courses_Price] CHECK ([Price] >= 0),
        CONSTRAINT [FK_Courses_CourseCategories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [CourseCategories] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_Courses_Instructors_InstructorId] FOREIGN KEY ([InstructorId]) REFERENCES [Instructors] ([Id]) ON DELETE SET NULL
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122095506_InitialCreate'
)
BEGIN
    CREATE TABLE [OrderStatusHistories] (
        [Id] int NOT NULL IDENTITY,
        [OrderId] int NOT NULL,
        [FromStatus] int NOT NULL,
        [ToStatus] int NOT NULL,
        [Reason] nvarchar(500) NOT NULL,
        [ChangedBy] nvarchar(256) NULL,
        [ChangedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_OrderStatusHistories] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_OrderStatusHistories_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122095506_InitialCreate'
)
BEGIN
    CREATE TABLE [PaymentTransactions] (
        [Id] int NOT NULL IDENTITY,
        [OrderId] int NOT NULL,
        [Gateway] nvarchar(30) NOT NULL,
        [IsSandbox] bit NOT NULL,
        [Amount] bigint NOT NULL,
        [GatewayAmount] bigint NOT NULL,
        [Authority] varchar(100) NULL,
        [Status] int NOT NULL,
        [RefId] varchar(50) NULL,
        [CardPan] varchar(30) NULL,
        [GatewayCode] int NULL,
        [GatewayMessage] nvarchar(500) NULL,
        [CallbackStatus] varchar(10) NULL,
        [VerifiedAt] datetime2 NULL,
        [VerifyAttempts] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_PaymentTransactions] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_PaymentTransactions_Amount] CHECK ([Amount] > 0 AND [GatewayAmount] > 0),
        CONSTRAINT [FK_PaymentTransactions_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122095506_InitialCreate'
)
BEGIN
    CREATE TABLE [Products] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(200) NOT NULL,
        [Slug] nvarchar(150) NOT NULL,
        [Sku] nvarchar(50) NOT NULL,
        [ShortDescription] nvarchar(500) NULL,
        [Description] nvarchar(max) NULL,
        [ImagePath] nvarchar(300) NULL,
        [ProductType] int NOT NULL,
        [CategoryId] int NULL,
        [Price] bigint NOT NULL,
        [DiscountPrice] bigint NULL,
        [Stock] int NULL,
        [IsAvailable] bit NOT NULL,
        [Status] int NOT NULL,
        [IsFeatured] bit NOT NULL,
        [SortOrder] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_Products] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Products_DiscountPrice] CHECK ([DiscountPrice] IS NULL OR [DiscountPrice] >= 0),
        CONSTRAINT [CK_Products_Price] CHECK ([Price] >= 0),
        CONSTRAINT [CK_Products_Stock] CHECK ([Stock] IS NULL OR [Stock] >= 0),
        CONSTRAINT [FK_Products_ProductCategories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [ProductCategories] ([Id]) ON DELETE SET NULL
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122095506_InitialCreate'
)
BEGIN
    CREATE TABLE [CourseImages] (
        [Id] int NOT NULL IDENTITY,
        [CourseId] int NOT NULL,
        [Path] nvarchar(300) NOT NULL,
        [Caption] nvarchar(200) NULL,
        [SortOrder] int NOT NULL,
        CONSTRAINT [PK_CourseImages] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CourseImages_Courses_CourseId] FOREIGN KEY ([CourseId]) REFERENCES [Courses] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122095506_InitialCreate'
)
BEGIN
    CREATE TABLE [CourseRegistrationFields] (
        [Id] int NOT NULL IDENTITY,
        [CourseId] int NOT NULL,
        [Label] nvarchar(150) NOT NULL,
        [FieldType] int NOT NULL,
        [Options] nvarchar(2000) NULL,
        [IsRequired] bit NOT NULL,
        [SortOrder] int NOT NULL,
        CONSTRAINT [PK_CourseRegistrationFields] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CourseRegistrationFields_Courses_CourseId] FOREIGN KEY ([CourseId]) REFERENCES [Courses] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122095506_InitialCreate'
)
BEGIN
    CREATE TABLE [CourseSchedules] (
        [Id] int NOT NULL IDENTITY,
        [CourseId] int NOT NULL,
        [DayOfWeek] int NOT NULL,
        [StartTime] time NOT NULL,
        [EndTime] time NOT NULL,
        [Note] nvarchar(200) NULL,
        CONSTRAINT [PK_CourseSchedules] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CourseSchedules_Courses_CourseId] FOREIGN KEY ([CourseId]) REFERENCES [Courses] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122095506_InitialCreate'
)
BEGIN
    CREATE TABLE [OrderItems] (
        [Id] int NOT NULL IDENTITY,
        [OrderId] int NOT NULL,
        [ItemType] int NOT NULL,
        [CourseId] int NULL,
        [ProductId] int NULL,
        [Title] nvarchar(200) NOT NULL,
        [UnitPrice] bigint NOT NULL,
        [DiscountPerUnit] bigint NOT NULL,
        [Quantity] int NOT NULL,
        [TotalPrice] bigint NOT NULL,
        CONSTRAINT [PK_OrderItems] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_OrderItems_Amounts] CHECK ([UnitPrice] >= 0 AND [TotalPrice] >= 0 AND [DiscountPerUnit] >= 0),
        CONSTRAINT [CK_OrderItems_Quantity] CHECK ([Quantity] > 0),
        CONSTRAINT [CK_OrderItems_Target] CHECK (([ItemType] = 0 AND [CourseId] IS NOT NULL) OR ([ItemType] = 1 AND [ProductId] IS NOT NULL)),
        CONSTRAINT [FK_OrderItems_Courses_CourseId] FOREIGN KEY ([CourseId]) REFERENCES [Courses] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_OrderItems_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_OrderItems_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122095506_InitialCreate'
)
BEGIN
    CREATE TABLE [ProductImages] (
        [Id] int NOT NULL IDENTITY,
        [ProductId] int NOT NULL,
        [Path] nvarchar(300) NOT NULL,
        [SortOrder] int NOT NULL,
        CONSTRAINT [PK_ProductImages] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ProductImages_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122095506_InitialCreate'
)
BEGIN
    CREATE TABLE [Enrollments] (
        [Id] int NOT NULL IDENTITY,
        [CourseId] int NOT NULL,
        [OrderId] int NOT NULL,
        [OrderItemId] int NULL,
        [FullName] nvarchar(100) NOT NULL,
        [Mobile] varchar(11) NOT NULL,
        [Email] nvarchar(150) NULL,
        [Notes] nvarchar(1000) NULL,
        [ExtraFieldsJson] nvarchar(max) NULL,
        [Status] int NOT NULL,
        [ConfirmedAt] datetime2 NULL,
        [ReservedUntil] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_Enrollments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Enrollments_Courses_CourseId] FOREIGN KEY ([CourseId]) REFERENCES [Courses] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Enrollments_OrderItems_OrderItemId] FOREIGN KEY ([OrderItemId]) REFERENCES [OrderItems] ([Id]),
        CONSTRAINT [FK_Enrollments_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122095506_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AdminAuditLogs_CreatedAt] ON [AdminAuditLogs] ([CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122095506_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AdminAuditLogs_EntityType_EntityId] ON [AdminAuditLogs] ([EntityType], [EntityId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122095506_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122095506_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122095506_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122095506_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122095506_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AspNetUserPasskeys_UserId] ON [AspNetUserPasskeys] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122095506_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122095506_InitialCreate'
)
BEGIN
    CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122095506_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122095506_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ContactMessages_IpAddress_CreatedAt] ON [ContactMessages] ([IpAddress], [CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122095506_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ContactMessages_IsRead_CreatedAt] ON [ContactMessages] ([IsRead], [CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122095506_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_CourseCategories_Slug] ON [CourseCategories] ([Slug]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122095506_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_CourseImages_CourseId] ON [CourseImages] ([CourseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122095506_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_CourseRegistrationFields_CourseId] ON [CourseRegistrationFields] ([CourseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122095506_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Courses_CategoryId] ON [Courses] ([CategoryId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122095506_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Courses_InstructorId] ON [Courses] ([InstructorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122095506_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Courses_Slug] ON [Courses] ([Slug]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122095506_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Courses_Status_IsFeatured_SortOrder] ON [Courses] ([Status], [IsFeatured], [SortOrder]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122095506_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_CourseSchedules_CourseId] ON [CourseSchedules] ([CourseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122095506_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Enrollments_CourseId_Mobile] ON [Enrollments] ([CourseId], [Mobile]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122095506_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Enrollments_CourseId_Status_ReservedUntil] ON [Enrollments] ([CourseId], [Status], [ReservedUntil]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122095506_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Enrollments_CreatedAt] ON [Enrollments] ([CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122095506_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Enrollments_OrderId] ON [Enrollments] ([OrderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122095506_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Enrollments_OrderItemId] ON [Enrollments] ([OrderItemId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122095506_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_OrderItems_CourseId] ON [OrderItems] ([CourseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122095506_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_OrderItems_OrderId] ON [OrderItems] ([OrderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122095506_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_OrderItems_ProductId] ON [OrderItems] ([ProductId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122095506_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Orders_Mobile] ON [Orders] ([Mobile]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122095506_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Orders_OrderNumber] ON [Orders] ([OrderNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122095506_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Orders_PublicId] ON [Orders] ([PublicId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122095506_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Orders_Status_CreatedAt] ON [Orders] ([Status], [CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122095506_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Orders_TrackingCode] ON [Orders] ([TrackingCode]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122095506_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_OrderStatusHistories_OrderId] ON [OrderStatusHistories] ([OrderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122095506_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_PaymentTransactions_Authority] ON [PaymentTransactions] ([Authority]) WHERE [Authority] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122095506_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_PaymentTransactions_OrderId] ON [PaymentTransactions] ([OrderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122095506_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_PaymentTransactions_RefId] ON [PaymentTransactions] ([RefId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122095506_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_PaymentTransactions_Status_CreatedAt] ON [PaymentTransactions] ([Status], [CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122095506_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ProductCategories_Slug] ON [ProductCategories] ([Slug]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122095506_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ProductImages_ProductId] ON [ProductImages] ([ProductId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122095506_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Products_CategoryId] ON [Products] ([CategoryId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122095506_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Products_Sku] ON [Products] ([Sku]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122095506_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Products_Slug] ON [Products] ([Slug]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122095506_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Products_Status_IsFeatured_SortOrder] ON [Products] ([Status], [IsFeatured], [SortOrder]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122095506_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261122095506_InitialCreate', N'10.0.15');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123082130_CardToCardAndTickets'
)
BEGIN
    UPDATE [SiteSettings] SET [SiteName] = N'فروشگاه آزما' WHERE [SiteName] = N'آموزشگاه آزما';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123082130_CardToCardAndTickets'
)
BEGIN
    ALTER TABLE [SiteSettings] ADD [BankName] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123082130_CardToCardAndTickets'
)
BEGIN
    ALTER TABLE [SiteSettings] ADD [CardHolderName] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123082130_CardToCardAndTickets'
)
BEGIN
    ALTER TABLE [SiteSettings] ADD [CardNumber] varchar(30) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123082130_CardToCardAndTickets'
)
BEGIN
    ALTER TABLE [SiteSettings] ADD [CardToCardEnabled] bit NOT NULL DEFAULT CAST(1 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123082130_CardToCardAndTickets'
)
BEGIN
    ALTER TABLE [SiteSettings] ADD [ShebaNumber] varchar(40) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123082130_CardToCardAndTickets'
)
BEGIN
    CREATE TABLE [PaymentReceipts] (
        [Id] int NOT NULL IDENTITY,
        [OrderId] int NOT NULL,
        [PayerName] nvarchar(100) NOT NULL,
        [PayerMobile] varchar(11) NOT NULL,
        [CardLast4] varchar(4) NULL,
        [BankReference] nvarchar(50) NULL,
        [CustomerNote] nvarchar(500) NULL,
        [Amount] bigint NOT NULL,
        [ImagePath] nvarchar(300) NOT NULL,
        [Status] int NOT NULL,
        [ReviewedBy] nvarchar(256) NULL,
        [ReviewedAt] datetime2 NULL,
        [RejectReason] nvarchar(500) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_PaymentReceipts] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_PaymentReceipts_Amount] CHECK ([Amount] > 0),
        CONSTRAINT [FK_PaymentReceipts_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123082130_CardToCardAndTickets'
)
BEGIN
    CREATE TABLE [Tickets] (
        [Id] int NOT NULL IDENTITY,
        [TicketNumber] varchar(20) NOT NULL,
        [PublicId] uniqueidentifier NOT NULL,
        [OrderId] int NULL,
        [FullName] nvarchar(100) NOT NULL,
        [Mobile] varchar(11) NOT NULL,
        [Subject] nvarchar(150) NOT NULL,
        [Status] int NOT NULL,
        [LastActivityAt] datetime2 NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_Tickets] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Tickets_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE SET NULL
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123082130_CardToCardAndTickets'
)
BEGIN
    CREATE TABLE [TicketMessages] (
        [Id] int NOT NULL IDENTITY,
        [TicketId] int NOT NULL,
        [Body] nvarchar(4000) NOT NULL,
        [IsFromStaff] bit NOT NULL,
        [AuthorName] nvarchar(256) NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_TicketMessages] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TicketMessages_Tickets_TicketId] FOREIGN KEY ([TicketId]) REFERENCES [Tickets] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123082130_CardToCardAndTickets'
)
BEGIN
    CREATE INDEX [IX_PaymentReceipts_OrderId] ON [PaymentReceipts] ([OrderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123082130_CardToCardAndTickets'
)
BEGIN
    CREATE INDEX [IX_PaymentReceipts_Status_CreatedAt] ON [PaymentReceipts] ([Status], [CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123082130_CardToCardAndTickets'
)
BEGIN
    CREATE INDEX [IX_TicketMessages_TicketId] ON [TicketMessages] ([TicketId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123082130_CardToCardAndTickets'
)
BEGIN
    CREATE INDEX [IX_Tickets_Mobile] ON [Tickets] ([Mobile]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123082130_CardToCardAndTickets'
)
BEGIN
    CREATE INDEX [IX_Tickets_OrderId] ON [Tickets] ([OrderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123082130_CardToCardAndTickets'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Tickets_PublicId] ON [Tickets] ([PublicId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123082130_CardToCardAndTickets'
)
BEGIN
    CREATE INDEX [IX_Tickets_Status_LastActivityAt] ON [Tickets] ([Status], [LastActivityAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123082130_CardToCardAndTickets'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Tickets_TicketNumber] ON [Tickets] ([TicketNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123082130_CardToCardAndTickets'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261123082130_CardToCardAndTickets', N'10.0.15');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123104444_HeroSlidesAndSections'
)
BEGIN
    ALTER TABLE [SiteSettings] ADD [CoursesEnabled] bit NOT NULL DEFAULT CAST(1 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123104444_HeroSlidesAndSections'
)
BEGIN
    ALTER TABLE [SiteSettings] ADD [ShopEnabled] bit NOT NULL DEFAULT CAST(1 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123104444_HeroSlidesAndSections'
)
BEGIN
    ALTER TABLE [SiteSettings] ADD [SliderIntervalSeconds] int NOT NULL DEFAULT 6;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123104444_HeroSlidesAndSections'
)
BEGIN
    CREATE TABLE [HeroSlides] (
        [Id] int NOT NULL IDENTITY,
        [Title] nvarchar(150) NULL,
        [Subtitle] nvarchar(400) NULL,
        [ImagePath] nvarchar(300) NOT NULL,
        [ButtonText] nvarchar(50) NULL,
        [ButtonUrl] nvarchar(300) NULL,
        [SortOrder] int NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_HeroSlides] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123104444_HeroSlidesAndSections'
)
BEGIN
    CREATE INDEX [IX_HeroSlides_IsActive_SortOrder] ON [HeroSlides] ([IsActive], [SortOrder]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123104444_HeroSlidesAndSections'
)
BEGIN
    UPDATE [SiteSettings] SET [TermsText] = NULL WHERE [TermsText] LIKE N'%پس از پرداخت موفق و دریافت کد پیگیری قطعی می%' AND LEN([TermsText]) < 400;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123104444_HeroSlidesAndSections'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261123104444_HeroSlidesAndSections', N'10.0.15');
END;

COMMIT;
GO

