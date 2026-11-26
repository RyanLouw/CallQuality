namespace CallQuality.Core.DataAccess.Context.Entities;

public sealed class AssessmentTrainingLink
{
    public int AssessmentId { get; set; }
    public long? TrainingId { get; set; }
    public Guid SubmissionId { get; set; }
    public Guid TrainedByUserId { get; set; }
    public DateTime TrainedAtUtc { get; set; }
}
