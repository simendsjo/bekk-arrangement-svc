ALTER TABLE [ParticipantQuestions]
ADD CONSTRAINT UNIQUE_Question UNIQUE (EventId, Question);
