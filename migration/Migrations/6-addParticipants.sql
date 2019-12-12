USE [arrangement-db];

CREATE TABLE Participants
(
    Email VARCHAR (255) NOT NULL,
    EventId UNIQUEIDENTIFIER NOT NULL,
    CONSTRAINT Pk_Participant PRIMARY KEY (Email, EventId),
    CONSTRAINT Fk_Participant_EventId FOREIGN KEY (EventId) REFERENCES Events(Id)
);