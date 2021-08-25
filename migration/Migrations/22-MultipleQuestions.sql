USE [arrangement-db];

BEGIN TRANSACTION;

CREATE TABLE [ParticipantQuestions]
(
	Id INT IDENTITY(1,1) PRIMARY KEY,
	EventId UNIQUEIDENTIFIER NOT NULL,

	Question VARCHAR(200) NOT NULL,

	FOREIGN KEY (EventId) REFERENCES [Events] (Id),
	UNIQUE (EventId, Question)
);

CREATE TABLE [ParticipantAnswers]
(
	QuestionId INT NOT NULL,
	-- Disse to er bare her som fremmednøkkel til Participants:
	EventId UNIQUEIDENTIFIER NOT NULL,
	Email VARCHAR (255) NOT NULL,

	Answer VARCHAR(1000) NOT NULL,

	PRIMARY KEY (QuestionId, EventId, Email),
	FOREIGN KEY (QuestionId) REFERENCES [ParticipantQuestions] (Id),
	FOREIGN KEY (Email, EventId) REFERENCES [Participants] (Email, EventId)
);

INSERT INTO ParticipantQuestions
	(EventId, Question)
SELECT Id, ParticipantQuestion
FROM Events
WHERE ParticipantQuestion IS NOT NULL;

INSERT INTO ParticipantAnswers
	(QuestionId, EventId, Email, Answer)
SELECT q.Id, p.EventId, p.Email, p.Comment
FROM Participants p
	INNER JOIN ParticipantQuestions q
	ON p.EventId = q.EventId
WHERE p.Comment IS NOT NULL;

ALTER TABLE Events DROP COLUMN ParticipantQuestion;
ALTER TABLE Participants DROP COLUMN Comment;

COMMIT TRANSACTION 