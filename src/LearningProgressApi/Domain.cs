namespace LearningProgressApi;

public enum CourseLevel
{
    Beginner,
    Intermediate,
    Advanced
}

public enum EnrollmentStatus
{
    Active,
    Completed,
    Failed,
    Withdrawn
}

public sealed record Course(
    int Id,
    string Code,
    string Title,
    CourseLevel Level,
    int PassMark,
    List<CourseModule> Modules,
    List<QuizQuestion> Quiz);

public sealed record CourseModule(int Id, string Title, int EstimatedMinutes);
public sealed record QuizQuestion(int Id, string Prompt, string Answer);

public sealed record Enrollment(
    int Id,
    int CourseId,
    string LearnerName,
    string LearnerEmail,
    EnrollmentStatus Status,
    DateTimeOffset EnrolledAt,
    DateTimeOffset? CompletedAt,
    List<int> CompletedModuleIds,
    List<QuizSubmission> QuizSubmissions);

public sealed record QuizSubmission(int Id, DateTimeOffset SubmittedAt, int Score, int TotalQuestions, bool Passed);

public sealed record CreateCourseRequest(string Code, string Title, string Level, int PassMark, IReadOnlyCollection<CreateModuleRequest> Modules, IReadOnlyCollection<CreateQuizQuestionRequest> Quiz);
public sealed record CreateModuleRequest(string Title, int EstimatedMinutes);
public sealed record CreateQuizQuestionRequest(string Prompt, string Answer);
public sealed record EnrollLearnerRequest(int CourseId, string LearnerName, string LearnerEmail);
public sealed record SubmitQuizRequest(IReadOnlyDictionary<int, string> Answers);

public sealed record CourseSummary(int Id, string Code, string Title, string Level, int ModuleCount, int EstimatedMinutes, int PassMark);
public sealed record LearnerDashboard(string LearnerName, string LearnerEmail, int ActiveCourses, int CompletedCourses, IReadOnlyCollection<LearnerCourseProgress> Courses);
public sealed record LearnerCourseProgress(int EnrollmentId, string CourseCode, string CourseTitle, string Status, double ProgressPercent, int? LatestScore, bool CertificateReady);
public sealed record CoursePerformance(string CourseCode, string CourseTitle, int Enrollments, int Completed, double CompletionRate, double AverageScore);

public sealed class OperationResult<T>
{
    private OperationResult(T? value, IReadOnlyCollection<string> errors, bool notFound)
    {
        Value = value;
        Errors = errors;
        NotFound = notFound;
    }

    public T? Value { get; }
    public IReadOnlyCollection<string> Errors { get; }
    public bool NotFound { get; }
    public bool IsSuccess => Errors.Count == 0;

    public static OperationResult<T> Success(T value) => new(value, Array.Empty<string>(), false);
    public static OperationResult<T> Failure(params string[] errors) => new(default, errors, false);
    public static OperationResult<T> Missing(params string[] errors) => new(default, errors, true);
}
