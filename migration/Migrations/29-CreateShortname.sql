USE [arrangement-db];
ALTER TABLE [Events] ADD Shortname NVARCHAR (255) NULL DEFAULT NULL;

CREATE UNIQUE NONCLUSTERED INDEX UQ_Event_Shortname
    ON Events(Shortname)
    WHERE Shortname IS NOT NULL;
