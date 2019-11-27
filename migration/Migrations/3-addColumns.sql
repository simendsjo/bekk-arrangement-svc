USE [arrangement-db]

ALTER TABLE [Events]
ADD 
    Guid UNIQUEIDENTIFIER NOT NULL default NEWID(),
    StartDate Date,
    StartTime Time,
    EndDate Date,
    EndTime Time,
    OpenForRegistrationDate Date,
    OpenForRegistrationTime Time;

UPDATE [Events]
SET 
    StartDate = 
        (SELECT CONVERT (Date, FromDate)
FROM [Events] AS E2
WHERE E2.Id = E1.Id),
    StartTime = 
        (SELECT CONVERT (Time, FromDate)
FROM [Events] AS E2
WHERE E2.Id = E1.Id),
    EndDate = 
        (SELECT CONVERT (Date, FromDate)
FROM [Events] AS E2
WHERE E2.Id = E1.Id),
    EndTime = 
        (SELECT CONVERT (Time, FromDate)
FROM [Events] AS E2
WHERE E2.Id = E1.Id)
FROM [Events] AS E1;

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