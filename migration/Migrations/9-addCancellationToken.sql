USE [arrangement-db];

ALTER TABLE [Participants]
ADD
  CancellationToken UNIQUEIDENTIFIER DEFAULT NEWID() NOT NULL;
