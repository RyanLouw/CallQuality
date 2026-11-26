IF OBJECT_ID(N'dbo.AssessmentTrainingLink', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AssessmentTrainingLink
    (
        AssessmentId int NOT NULL,
        TrainingId bigint NULL,
        SubmissionId uniqueidentifier NOT NULL,
        TrainedByUserId uniqueidentifier NOT NULL,
        TrainedAtUtc datetime2 NOT NULL,
        CONSTRAINT PK_AssessmentTrainingLink PRIMARY KEY (AssessmentId),
        CONSTRAINT FK_AssessmentTrainingLink_Assessments
            FOREIGN KEY (AssessmentId) REFERENCES dbo.Assessments (AssessmentID)
    );

    CREATE INDEX IX_AssessmentTrainingLink_TrainingId
        ON dbo.AssessmentTrainingLink (TrainingId) WHERE TrainingId IS NOT NULL;

    CREATE INDEX IX_AssessmentTrainingLink_SubmissionId
        ON dbo.AssessmentTrainingLink (SubmissionId);
END;
