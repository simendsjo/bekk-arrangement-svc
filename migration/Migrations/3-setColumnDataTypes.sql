USE [arrangement-db]

ALTER TABLE [Events]
ALTER COLUMN
    StartDate Date NOT NULL;
ALTER TABLE [Events]
ALTER COLUMN
    StartTime Time NOT NULL;
ALTER TABLE [Events]
ALTER COLUMN
    EndDate Date NOT NULL;
ALTER TABLE [Events]
ALTER COLUMN
    EndTime Time NOT NULL;