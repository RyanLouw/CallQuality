namespace CallQuality.Core.DataAccess.Context.Entities.StoredProcedureResults;

public sealed class RequiredTrainingAssessment
{
    public int AssessmentId { get; init; }
    public string OperatorId { get; init; } = string.Empty;
    public double Percentage { get; init; }
    public List<string> MissedQuestions { get; init; } = [];
}
