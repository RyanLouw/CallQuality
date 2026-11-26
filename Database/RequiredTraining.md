# Required Training deployment

1. Run AssessmentTrainingLink.sql against the Call Quality database.
2. Update the Training Register API action at `POST CallQuality/SaveNewTrainingRegister`
   to return the persisted register ID as a positive integer or
   `{ "trainingId": 123 }` on success. The existing request and status contract stays in use.
3. Deploy Call Quality after both changes. Verify one register for an agent with several missed assessments
   creates one link per assessment and that the agent leaves the queue once all of their pending assessments are covered.

The list uses the existing current calendar month. A completed reassessment takes precedence for answers it supplies.
The assessor role includes secondary assignments whose start and end dates cover today.

## Recover an unconfirmed submission

A submission reserves every assessment in one save before requesting a register. A reservation
has a `SubmissionId` and a null `TrainingId`. Each assessment can belong to only one reservation
or completed link. This prevents a retry from creating a second register if the API response is lost
or the local confirmation fails.

Find the submission in the Call Quality logs. If a register was created, verify the actual register ID,
trainee, and scope, then record that ID against every null link for the submission in one transaction.
If the API confirms no register was created and the request has finished, remove only the unconfirmed
rows for that submission so the original form can be retried.
