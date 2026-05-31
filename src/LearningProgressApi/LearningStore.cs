namespace LearningProgressApi;

public sealed class LearningStore
{
    private readonly List<Course> courses;
    private readonly List<Enrollment> enrollments;
    private int nextCourseId = 4;
    private int nextModuleId = 10;
    private int nextQuizQuestionId = 10;
    private int nextEnrollmentId = 4;
    private int nextSubmissionId = 4;

    public LearningStore()
    {
        courses =
        [
            new Course(
                1,
                "CSHARP-API",
                "ASP.NET Core Web API Fundamentals",
                CourseLevel.Beginner,
                70,
                [new CourseModule(1, "HTTP and REST basics", 45),
                 new CourseModule(2, "Minimal API route design", 60),
                 new CourseModule(3, "Validation and responses", 50)],
                [new QuizQuestion(1, "Which HTTP verb creates a resource?", "POST"),
                 new QuizQuestion(2, "What format do most Web APIs return?", "JSON"),
                 new QuizQuestion(3, "What status code means created?", "201")]),
            new Course(
                2,
                "LINQ-101",
                "LINQ for Data Queries",
                CourseLevel.Beginner,
                75,
                [new CourseModule(4, "Filtering collections", 35),
                 new CourseModule(5, "Grouping and summaries", 45)],
                [new QuizQuestion(4, "Which LINQ method filters records?", "Where"),
                 new QuizQuestion(5, "Which LINQ method groups records?", "GroupBy")]),
            new Course(
                3,
                "ARCH-API",
                "Practical API Architecture",
                CourseLevel.Intermediate,
                80,
                [new CourseModule(6, "DTOs and services", 50),
                 new CourseModule(7, "Business rules", 55),
                 new CourseModule(8, "Reporting endpoints", 45),
                 new CourseModule(9, "Testing strategy", 40)],
                [new QuizQuestion(6, "What object carries data into an endpoint?", "DTO"),
                 new QuizQuestion(7, "Where should business rules usually live?", "Service"),
                 new QuizQuestion(8, "What helps verify behavior?", "Tests")])
        ];

        var now = DateTimeOffset.UtcNow;
        enrollments =
        [
            new Enrollment(1, 1, "Oageng Tsumaki", "oageng@example.com", EnrollmentStatus.Active, now.AddDays(-5), null, [1, 2], [new QuizSubmission(1, now.AddDays(-1), 2, 3, false)]),
            new Enrollment(2, 2, "Oageng Tsumaki", "oageng@example.com", EnrollmentStatus.Completed, now.AddDays(-12), now.AddDays(-8), [4, 5], [new QuizSubmission(2, now.AddDays(-8), 2, 2, true)]),
            new Enrollment(3, 1, "Amina Jacobs", "amina@example.com", EnrollmentStatus.Completed, now.AddDays(-20), now.AddDays(-15), [1, 2, 3], [new QuizSubmission(3, now.AddDays(-15), 3, 3, true)])
        ];
    }

    public IReadOnlyCollection<Course> Courses => courses;
    public IReadOnlyCollection<Enrollment> Enrollments => enrollments;

    public int NextCourseId() => nextCourseId++;
    public int NextModuleId() => nextModuleId++;
    public int NextQuizQuestionId() => nextQuizQuestionId++;
    public int NextEnrollmentId() => nextEnrollmentId++;
    public int NextSubmissionId() => nextSubmissionId++;

    public void AddCourse(Course course) => courses.Add(course);
    public void AddEnrollment(Enrollment enrollment) => enrollments.Add(enrollment);
    public void ReplaceEnrollment(Enrollment enrollment)
    {
        var index = enrollments.FindIndex(item => item.Id == enrollment.Id);
        enrollments[index] = enrollment;
    }
}
