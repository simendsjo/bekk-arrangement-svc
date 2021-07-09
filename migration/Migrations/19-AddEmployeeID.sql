USE [arrangement-db];

ALTER TABLE [Events]
ADD OrganizerId INT NOT NULL DEFAULT 0;

ALTER TABLE [Participants]
ADD EmployeeId INT NULL;
