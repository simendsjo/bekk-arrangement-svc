USE [arrangement-db];

ALTER TABLE [Events]
ALTER COLUMN MaxParticipants INT NULL;

UPDATE Events
SET MaxParticipants = NULL
WHERE MaxParticipants = 0;

