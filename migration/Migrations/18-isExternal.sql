USE [arrangement-db];

ALTER TABLE [Events]
ADD IsExternal BIT NOT NULL DEFAULT 0;