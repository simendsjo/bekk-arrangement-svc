USE [arrangement-db];

ALTER TABLE [Events]
ADD ParticipantQuestion NVARCHAR(MAX) NULL;

ALTER TABLE [Participants]
ALTER COLUMN [Comment] NVARCHAR(MAX) NULL;
