USE [arrangement-db]

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
        (SELECT CONVERT (Date, ToDate)
FROM [Events] AS E2
WHERE E2.Id = E1.Id),
    EndTime = 
        (SELECT CONVERT (Time, ToDate)
FROM [Events] AS E2
WHERE E2.Id = E1.Id)
FROM [Events] AS E1;

