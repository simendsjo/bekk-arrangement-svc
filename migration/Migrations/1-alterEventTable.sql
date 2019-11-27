USE [arrangement-db]

--CREATE TABLE Migrations (Name VARCHAR (255) NOT NULL);

ALTER TABLE [Events]
ADD 
    StartDate Date,
    StartTime Time,
    EndDate Date,
    EndTime Time,
    OpenForRegistrationDate Date,
    OpenForRegistrationTime Time;