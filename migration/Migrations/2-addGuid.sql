-- DROP TABLE [Events];

--CREATE TABLE [Events]
--(
--    Id UNIQUEIDENTIFIER PRIMARY KEY NOT NULL DEFAULT newid(),
--    Title VARCHAR(255) NOT NULL,
--    Description TEXT NULL,
--    Location VARCHAR(255) NOT NULL,
--    FromDate datetimeoffset(3) NOT NULL,
--    ToDate datetimeoffset(3) NOT NULL,
--    OrganizerEmail VARCHAR(255) NOT NULL,
--);


INSERT INTO [Events]
VALUES ('Test', 'Dette er et testarrangement', 'Skuret', '2019-11-07T10:00:00Z', '2019-11-07T10:30:00Z', 1388);
