ALTER TABLE [ParticipantQuestions]
DROP CONSTRAINT UQ__Particip__FFF74FB7C7FB9369;

ALTER TABLE [ParticipantQuestions]
ALTER COLUMN Question NVARCHAR(300) NOT NULL;

ALTER TABLE [ParticipantAnswers]
ALTER COLUMN Answer NVARCHAR(1500) NOT NULL;
