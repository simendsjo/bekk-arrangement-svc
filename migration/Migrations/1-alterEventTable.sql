USE [arrangement-db]

ALTER TABLE [Events]
ADD 
    StartDate Date,
    StartTime Time,
    EndDate Date,
    EndTime Time,
    OpenForRegistrationDate Date,
    OpenForRegistrationTime Time;