USE [arrangement-db]

ALTER TABLE [Events]
DROP COLUMN 
    FromDate,
    ToDate;

-- Rename column Guid to Id
-- GO
-- EXEC sp_rename 'dbo.Events.Guid', 'Id', 'COLUMN';  
-- GO 
