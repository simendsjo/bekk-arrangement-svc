USE [arrangement-db]

CREATE TABLE Migrations
(
    Name VARCHAR (255) NOT NULL
);

CREATE TABLE [Events]
(
    Id UNIQUEIDENTIFIER PRIMARY KEY NOT NULL DEFAULT newid(),
    Title VARCHAR(255) NOT NULL,
    Description TEXT NULL,
    Location VARCHAR(255) NOT NULL,
    FromDate datetimeoffset(3) NOT NULL,
    ToDate datetimeoffset(3) NOT NULL,
    OrganizerEmail VARCHAR (255) NOT NULL,
);