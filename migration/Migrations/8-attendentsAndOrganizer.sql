USE [arrangement-db];

ALTER TABLE [Events]
ADD 
    MaxParticipants INT NOT NULL DEFAULT 0,
    OrganizerName VARCHAR(255) NOT NULL DEFAULT '';