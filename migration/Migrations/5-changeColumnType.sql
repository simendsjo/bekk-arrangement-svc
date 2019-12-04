USE [arrangement-db]
ALTER TABLE [Events]
ALTER COLUMN
    OpenForRegistrationTime Time NOT NULL;
ALTER TABLE [Events]
ALTER COLUMN OpenForRegistrationDate Date NOT NULL;