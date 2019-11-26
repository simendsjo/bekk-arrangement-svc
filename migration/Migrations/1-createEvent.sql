USE [arrangement-db]

CREATE TABLE [Events]
(
    Id INT PRIMARY KEY IDENTITY(1,1) NOT NULL,
    Title VARCHAR(255) NOT NULL,
    Description TEXT NULL,
    Location VARCHAR(255) NOT NULL,
    FromDate datetimeoffset(3) NOT NULL,
    ToDate datetimeoffset(3) NOT NULL,
    ResponsibleEmployee INT NOT NULL,
);

INSERT INTO [Events]
VALUES ('Test', 'Dette er et testarrangement', 'Skuret', '2019-11-07T10:00:00Z', '2019-11-07T10:30:00Z', 1388);