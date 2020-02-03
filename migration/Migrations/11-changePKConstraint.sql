ALTER TABLE [Participants]
DROP CONSTRAINT [Fk_Participant_EventId];

ALTER TABLE [Participants]
ADD CONSTRAINT [Fk_Participant_EventId]
FOREIGN KEY (EventId) REFERENCES [Events](Id)
ON DELETE CASCADE;