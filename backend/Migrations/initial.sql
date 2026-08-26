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
CREATE TABLE [Applications] (
    [Id] int NOT NULL IDENTITY,
    [Nom] nvarchar(150) NOT NULL,
    [Actif] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Applications] PRIMARY KEY ([Id])
);

CREATE TABLE [Criticites] (
    [Id] int NOT NULL IDENTITY,
    [Nom] nvarchar(150) NOT NULL,
    [Actif] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Criticites] PRIMARY KEY ([Id])
);

CREATE TABLE [Entites] (
    [Id] int NOT NULL IDENTITY,
    [Nom] nvarchar(150) NOT NULL,
    [Actif] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Entites] PRIMARY KEY ([Id])
);

CREATE TABLE [IncidentNumberSequences] (
    [Year] int NOT NULL IDENTITY,
    [LastValue] int NOT NULL,
    CONSTRAINT [PK_IncidentNumberSequences] PRIMARY KEY ([Year])
);

CREATE TABLE [Responsables] (
    [Id] int NOT NULL IDENTITY,
    [Nom] nvarchar(150) NOT NULL,
    [Actif] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Responsables] PRIMARY KEY ([Id])
);

CREATE TABLE [Risques] (
    [Id] int NOT NULL IDENTITY,
    [Nom] nvarchar(150) NOT NULL,
    [Actif] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Risques] PRIMARY KEY ([Id])
);

CREATE TABLE [Statuts] (
    [Id] int NOT NULL IDENTITY,
    [Nom] nvarchar(150) NOT NULL,
    [Actif] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Statuts] PRIMARY KEY ([Id])
);

CREATE TABLE [TypesIncident] (
    [Id] int NOT NULL IDENTITY,
    [Nom] nvarchar(150) NOT NULL,
    [Actif] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_TypesIncident] PRIMARY KEY ([Id])
);

CREATE TABLE [Incidents] (
    [Id] int NOT NULL IDENTITY,
    [Numero] nvarchar(20) NOT NULL,
    [DateDeclaration] datetime2 NOT NULL,
    [DateFin] datetime2 NULL,
    [TypeIncidentId] int NOT NULL,
    [ApplicationId] int NOT NULL,
    [Intitule] nvarchar(300) NOT NULL,
    [Description] nvarchar(max) NOT NULL,
    [EntiteId] int NOT NULL,
    [CriticiteId] int NOT NULL,
    [Impact] nvarchar(max) NULL,
    [Cause] nvarchar(max) NULL,
    [RisqueId] int NOT NULL,
    [ActionsMenees] nvarchar(max) NULL,
    [Solution] nvarchar(max) NULL,
    [ActionsEnCours] nvarchar(max) NULL,
    [ResponsableN1Id] int NOT NULL,
    [ResponsableN2Id] int NULL,
    [MesuresPreventives] nvarchar(max) NULL,
    [StatutId] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Incidents] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Incidents_Applications_ApplicationId] FOREIGN KEY ([ApplicationId]) REFERENCES [Applications] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Incidents_Criticites_CriticiteId] FOREIGN KEY ([CriticiteId]) REFERENCES [Criticites] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Incidents_Entites_EntiteId] FOREIGN KEY ([EntiteId]) REFERENCES [Entites] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Incidents_Responsables_ResponsableN1Id] FOREIGN KEY ([ResponsableN1Id]) REFERENCES [Responsables] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Incidents_Responsables_ResponsableN2Id] FOREIGN KEY ([ResponsableN2Id]) REFERENCES [Responsables] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Incidents_Risques_RisqueId] FOREIGN KEY ([RisqueId]) REFERENCES [Risques] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Incidents_Statuts_StatutId] FOREIGN KEY ([StatutId]) REFERENCES [Statuts] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Incidents_TypesIncident_TypeIncidentId] FOREIGN KEY ([TypeIncidentId]) REFERENCES [TypesIncident] ([Id]) ON DELETE NO ACTION
);

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Actif', N'CreatedAt', N'Nom', N'UpdatedAt') AND [object_id] = OBJECT_ID(N'[Applications]'))
    SET IDENTITY_INSERT [Applications] ON;
INSERT INTO [Applications] ([Id], [Actif], [CreatedAt], [Nom], [UpdatedAt])
VALUES (1, CAST(1 AS bit), '2026-01-01T00:00:00.0000000Z', N'ABM', '2026-01-01T00:00:00.0000000Z'),
(2, CAST(1 AS bit), '2026-01-01T00:00:00.0000000Z', N'AMPLITUDE', '2026-01-01T00:00:00.0000000Z'),
(3, CAST(1 AS bit), '2026-01-01T00:00:00.0000000Z', N'CARTHAGO', '2026-01-01T00:00:00.0000000Z'),
(4, CAST(1 AS bit), '2026-01-01T00:00:00.0000000Z', N'SMARTACCESS', '2026-01-01T00:00:00.0000000Z'),
(5, CAST(1 AS bit), '2026-01-01T00:00:00.0000000Z', N'SWIFT', '2026-01-01T00:00:00.0000000Z'),
(6, CAST(1 AS bit), '2026-01-01T00:00:00.0000000Z', N'TOUTES LES APPLICATIONS', '2026-01-01T00:00:00.0000000Z'),
(7, CAST(1 AS bit), '2026-01-01T00:00:00.0000000Z', N'VNEURON', '2026-01-01T00:00:00.0000000Z');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Actif', N'CreatedAt', N'Nom', N'UpdatedAt') AND [object_id] = OBJECT_ID(N'[Applications]'))
    SET IDENTITY_INSERT [Applications] OFF;

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Actif', N'CreatedAt', N'Nom', N'UpdatedAt') AND [object_id] = OBJECT_ID(N'[Criticites]'))
    SET IDENTITY_INSERT [Criticites] ON;
INSERT INTO [Criticites] ([Id], [Actif], [CreatedAt], [Nom], [UpdatedAt])
VALUES (1, CAST(1 AS bit), '2026-01-01T00:00:00.0000000Z', N'Fort', '2026-01-01T00:00:00.0000000Z'),
(2, CAST(1 AS bit), '2026-01-01T00:00:00.0000000Z', N'Moyenne', '2026-01-01T00:00:00.0000000Z');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Actif', N'CreatedAt', N'Nom', N'UpdatedAt') AND [object_id] = OBJECT_ID(N'[Criticites]'))
    SET IDENTITY_INSERT [Criticites] OFF;

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Actif', N'CreatedAt', N'Nom', N'UpdatedAt') AND [object_id] = OBJECT_ID(N'[Entites]'))
    SET IDENTITY_INSERT [Entites] ON;
INSERT INTO [Entites] ([Id], [Actif], [CreatedAt], [Nom], [UpdatedAt])
VALUES (1, CAST(1 AS bit), '2026-01-01T00:00:00.0000000Z', N'2-PLATEAUX', '2026-01-01T00:00:00.0000000Z'),
(2, CAST(1 AS bit), '2026-01-01T00:00:00.0000000Z', N'ABATTA', '2026-01-01T00:00:00.0000000Z'),
(3, CAST(1 AS bit), '2026-01-01T00:00:00.0000000Z', N'ADJAME', '2026-01-01T00:00:00.0000000Z'),
(4, CAST(1 AS bit), '2026-01-01T00:00:00.0000000Z', N'AGENCE ABATTA', '2026-01-01T00:00:00.0000000Z'),
(5, CAST(1 AS bit), '2026-01-01T00:00:00.0000000Z', N'AGENCE ANGRE', '2026-01-01T00:00:00.0000000Z'),
(6, CAST(1 AS bit), '2026-01-01T00:00:00.0000000Z', N'AGENCE DALOA', '2026-01-01T00:00:00.0000000Z'),
(7, CAST(1 AS bit), '2026-01-01T00:00:00.0000000Z', N'AGENCE KOUMASSI', '2026-01-01T00:00:00.0000000Z'),
(8, CAST(1 AS bit), '2026-01-01T00:00:00.0000000Z', N'ANGRE', '2026-01-01T00:00:00.0000000Z'),
(9, CAST(1 AS bit), '2026-01-01T00:00:00.0000000Z', N'BANQUE PRIVEE', '2026-01-01T00:00:00.0000000Z'),
(10, CAST(1 AS bit), '2026-01-01T00:00:00.0000000Z', N'BARDO', '2026-01-01T00:00:00.0000000Z'),
(11, CAST(1 AS bit), '2026-01-01T00:00:00.0000000Z', N'BOUAKE', '2026-01-01T00:00:00.0000000Z'),
(12, CAST(1 AS bit), '2026-01-01T00:00:00.0000000Z', N'CBICI', '2026-01-01T00:00:00.0000000Z'),
(13, CAST(1 AS bit), '2026-01-01T00:00:00.0000000Z', N'DALOA', '2026-01-01T00:00:00.0000000Z'),
(14, CAST(1 AS bit), '2026-01-01T00:00:00.0000000Z', N'DUEKOUE', '2026-01-01T00:00:00.0000000Z'),
(15, CAST(1 AS bit), '2026-01-01T00:00:00.0000000Z', N'KOUMASSI', '2026-01-01T00:00:00.0000000Z'),
(16, CAST(1 AS bit), '2026-01-01T00:00:00.0000000Z', N'PLATEAU', '2026-01-01T00:00:00.0000000Z'),
(17, CAST(1 AS bit), '2026-01-01T00:00:00.0000000Z', N'SAN PEDRO BARDOT', '2026-01-01T00:00:00.0000000Z'),
(18, CAST(1 AS bit), '2026-01-01T00:00:00.0000000Z', N'SAN PEDRO CITE', '2026-01-01T00:00:00.0000000Z'),
(19, CAST(1 AS bit), '2026-01-01T00:00:00.0000000Z', N'TREICHVILLE', '2026-01-01T00:00:00.0000000Z'),
(20, CAST(1 AS bit), '2026-01-01T00:00:00.0000000Z', N'YOP 3E PONT', '2026-01-01T00:00:00.0000000Z');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Actif', N'CreatedAt', N'Nom', N'UpdatedAt') AND [object_id] = OBJECT_ID(N'[Entites]'))
    SET IDENTITY_INSERT [Entites] OFF;

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Actif', N'CreatedAt', N'Nom', N'UpdatedAt') AND [object_id] = OBJECT_ID(N'[Responsables]'))
    SET IDENTITY_INSERT [Responsables] ON;
INSERT INTO [Responsables] ([Id], [Actif], [CreatedAt], [Nom], [UpdatedAt])
VALUES (1, CAST(1 AS bit), '2026-01-01T00:00:00.0000000Z', N'ARSENE', '2026-01-01T00:00:00.0000000Z'),
(2, CAST(1 AS bit), '2026-01-01T00:00:00.0000000Z', N'ARSERNE', '2026-01-01T00:00:00.0000000Z'),
(3, CAST(1 AS bit), '2026-01-01T00:00:00.0000000Z', N'DOUASSE', '2026-01-01T00:00:00.0000000Z'),
(4, CAST(1 AS bit), '2026-01-01T00:00:00.0000000Z', N'EMMANUEL', '2026-01-01T00:00:00.0000000Z'),
(5, CAST(1 AS bit), '2026-01-01T00:00:00.0000000Z', N'Equipe Exploitation', '2026-01-01T00:00:00.0000000Z'),
(6, CAST(1 AS bit), '2026-01-01T00:00:00.0000000Z', N'HOLDING', '2026-01-01T00:00:00.0000000Z'),
(7, CAST(1 AS bit), '2026-01-01T00:00:00.0000000Z', N'KARAMOKO', '2026-01-01T00:00:00.0000000Z'),
(8, CAST(1 AS bit), '2026-01-01T00:00:00.0000000Z', N'LEGRE', '2026-01-01T00:00:00.0000000Z'),
(9, CAST(1 AS bit), '2026-01-01T00:00:00.0000000Z', N'MATHURIN', '2026-01-01T00:00:00.0000000Z'),
(10, CAST(1 AS bit), '2026-01-01T00:00:00.0000000Z', N'NICAISE', '2026-01-01T00:00:00.0000000Z'),
(11, CAST(1 AS bit), '2026-01-01T00:00:00.0000000Z', N'PRESTATAIRE ELECTRIQUE', '2026-01-01T00:00:00.0000000Z'),
(12, CAST(1 AS bit), '2026-01-01T00:00:00.0000000Z', N'PRESTATAIRE SMART ACCESS', '2026-01-01T00:00:00.0000000Z'),
(13, CAST(1 AS bit), '2026-01-01T00:00:00.0000000Z', N'SOMTORE', '2026-01-01T00:00:00.0000000Z'),
(14, CAST(1 AS bit), '2026-01-01T00:00:00.0000000Z', N'BCEAO', '2026-01-01T00:00:00.0000000Z');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Actif', N'CreatedAt', N'Nom', N'UpdatedAt') AND [object_id] = OBJECT_ID(N'[Responsables]'))
    SET IDENTITY_INSERT [Responsables] OFF;

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Actif', N'CreatedAt', N'Nom', N'UpdatedAt') AND [object_id] = OBJECT_ID(N'[Risques]'))
    SET IDENTITY_INSERT [Risques] ON;
INSERT INTO [Risques] ([Id], [Actif], [CreatedAt], [Nom], [UpdatedAt])
VALUES (1, CAST(1 AS bit), '2026-01-01T00:00:00.0000000Z', N'Fort', '2026-01-01T00:00:00.0000000Z'),
(2, CAST(1 AS bit), '2026-01-01T00:00:00.0000000Z', N'Moyen', '2026-01-01T00:00:00.0000000Z');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Actif', N'CreatedAt', N'Nom', N'UpdatedAt') AND [object_id] = OBJECT_ID(N'[Risques]'))
    SET IDENTITY_INSERT [Risques] OFF;

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Actif', N'CreatedAt', N'Nom', N'UpdatedAt') AND [object_id] = OBJECT_ID(N'[Statuts]'))
    SET IDENTITY_INSERT [Statuts] ON;
INSERT INTO [Statuts] ([Id], [Actif], [CreatedAt], [Nom], [UpdatedAt])
VALUES (1, CAST(1 AS bit), '2026-01-01T00:00:00.0000000Z', N'En cours', '2026-01-01T00:00:00.0000000Z'),
(2, CAST(1 AS bit), '2026-01-01T00:00:00.0000000Z', N'Clôturé', '2026-01-01T00:00:00.0000000Z');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Actif', N'CreatedAt', N'Nom', N'UpdatedAt') AND [object_id] = OBJECT_ID(N'[Statuts]'))
    SET IDENTITY_INSERT [Statuts] OFF;

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Actif', N'CreatedAt', N'Nom', N'UpdatedAt') AND [object_id] = OBJECT_ID(N'[TypesIncident]'))
    SET IDENTITY_INSERT [TypesIncident] ON;
INSERT INTO [TypesIncident] ([Id], [Actif], [CreatedAt], [Nom], [UpdatedAt])
VALUES (1, CAST(1 AS bit), '2026-01-01T00:00:00.0000000Z', N'Infrastructure / électrique', '2026-01-01T00:00:00.0000000Z'),
(2, CAST(1 AS bit), '2026-01-01T00:00:00.0000000Z', N'application', '2026-01-01T00:00:00.0000000Z'),
(3, CAST(1 AS bit), '2026-01-01T00:00:00.0000000Z', N'réseaux', '2026-01-01T00:00:00.0000000Z');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Actif', N'CreatedAt', N'Nom', N'UpdatedAt') AND [object_id] = OBJECT_ID(N'[TypesIncident]'))
    SET IDENTITY_INSERT [TypesIncident] OFF;

CREATE INDEX [IX_Applications_Nom] ON [Applications] ([Nom]);

CREATE INDEX [IX_Criticites_Nom] ON [Criticites] ([Nom]);

CREATE INDEX [IX_Entites_Nom] ON [Entites] ([Nom]);

CREATE INDEX [IX_Incidents_ApplicationId] ON [Incidents] ([ApplicationId]);

CREATE INDEX [IX_Incidents_CriticiteId] ON [Incidents] ([CriticiteId]);

CREATE INDEX [IX_Incidents_EntiteId] ON [Incidents] ([EntiteId]);

CREATE UNIQUE INDEX [IX_Incidents_Numero] ON [Incidents] ([Numero]);

CREATE INDEX [IX_Incidents_ResponsableN1Id] ON [Incidents] ([ResponsableN1Id]);

CREATE INDEX [IX_Incidents_ResponsableN2Id] ON [Incidents] ([ResponsableN2Id]);

CREATE INDEX [IX_Incidents_RisqueId] ON [Incidents] ([RisqueId]);

CREATE INDEX [IX_Incidents_StatutId] ON [Incidents] ([StatutId]);

CREATE INDEX [IX_Incidents_TypeIncidentId] ON [Incidents] ([TypeIncidentId]);

CREATE INDEX [IX_Responsables_Nom] ON [Responsables] ([Nom]);

CREATE INDEX [IX_Risques_Nom] ON [Risques] ([Nom]);

CREATE INDEX [IX_Statuts_Nom] ON [Statuts] ([Nom]);

CREATE INDEX [IX_TypesIncident_Nom] ON [TypesIncident] ([Nom]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260826122238_InitialCreate', N'10.0.2');

COMMIT;
GO

BEGIN TRANSACTION;
CREATE TABLE [dbo].[IncidentNumberSequences_New]
(
    [Year] int NOT NULL,
    [LastValue] int NOT NULL,
    CONSTRAINT [PK_IncidentNumberSequences_New] PRIMARY KEY ([Year])
);

INSERT INTO [dbo].[IncidentNumberSequences_New] ([Year], [LastValue])
SELECT [Year], [LastValue]
FROM [dbo].[IncidentNumberSequences];

DROP TABLE [dbo].[IncidentNumberSequences];
EXEC sp_rename N'dbo.IncidentNumberSequences_New', N'IncidentNumberSequences';
EXEC sp_rename N'dbo.PK_IncidentNumberSequences_New', N'PK_IncidentNumberSequences', N'OBJECT';

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260826125835_FixIncidentNumberSequenceYear', N'10.0.2');

COMMIT;
GO

