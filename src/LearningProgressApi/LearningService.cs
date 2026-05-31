namespace LearningProgressApi;

public sealed class LearningService(LearningStore store)
{
    public IReadOnlyCollection<CourseSummary> SearchCourses(string? level, string? search)
    {
        var courses = store.Courses.AsEnumerable();

        if (Enum.TryParse(level, true, out CourseLevel parsedLevel))
        {
            courses = courses.Where(course => course.Level == parsedLevel);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            courses = courses.Where(course =>
                course.Code.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                course.Title.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        return courses.OrderBy(course => course.Level).ThenBy(course => course.Title).Select(ToSummary).ToList();
    }

    public Course? GetCourse(int id) => store.Courses.FirstOrDefault(course => course.Id == id);
    public IReadOnlyCollection<Enrollment> GetEnrollments() => store.Enrollments.OrderByDescending(enrollment => enrollment.EnrolledAt).ToList();

    public OperationResult<Course> CreateCourse(CreateCourseRequest request)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.Title))
        {
            errors.Add("Code and title are required.");
        }

        if (!Enum.TryParse(request.Level, true, out CourseLevel level))
        {
            errors.Add("Level must be Beginner, Intermediate, or Advanced.");
        }

        if (request.PassMark is < 1 or > 100)
        {
            errors.Add("Pass mark must be between 1 and 100.");
        }

        if (request.Modules.Count == 0 || request.Quiz.Count == 0)
        {
            errors.Add("At least one module and one quiz question are required.");
        }

        if (store.Courses.Any(course => course.Code.Equals(request.Code, StringComparison.OrdinalIgnoreCase)))
        {
            errors.Add("Course code already exists.");
        }

        if (errors.Count > 0)
        {
            return OperationResult<Course>.Failure([.. errors]);
        }

        var modules = request.Modules
            .Select(module => new CourseModule(store.NextModuleId(), module.Title.Trim(), Math.Max(module.EstimatedMinutes, 1)))
            .ToList();

        var quiz = request.Quiz
            .Select(question => new QuizQuestion(store.NextQuizQuestionId(), question.Prompt.Trim(), question.Answer.Trim()))
            .ToList();

        var course = new Course(store.NextCourseId(), request.Code.Trim().ToUpperInvariant(), request.Title.Trim(), level, request.PassMark, modules, quiz);
        store.AddCourse(course);
        return OperationResult<Course>.Success(course);
    }

    public OperationResult<Enrollment> Enroll(EnrollLearnerRequest request)
    {
        var course = GetCourse(request.CourseId);
        if (course is null)
        {
            return OperationResult<Enrollment>.Failure("Course does not exist.");
        }

        if (string.IsNullOrWhiteSpace(request.LearnerName) || string.IsNullOrWhiteSpace(request.LearnerEmail) || !request.LearnerEmail.Contains('@'))
        {
            return OperationResult<Enrollment>.Failure("Learner name and valid email are required.");
        }

        if (store.Enrollments.Any(enrollment =>
            enrollment.CourseId == request.CourseId &&
            enrollment.LearnerEmail.Equals(request.LearnerEmail, StringComparison.OrdinalIgnoreCase) &&
            enrollment.Status == EnrollmentStatus.Active))
        {
            return OperationResult<Enrollment>.Failure("Learner already has an active enrollment for this course.");
        }

        var enrollment = new Enrollment(
            store.NextEnrollmentId(),
            request.CourseId,
            request.LearnerName.Trim(),
            request.LearnerEmail.Trim().ToLowerInvariant(),
            EnrollmentStatus.Active,
            DateTimeOffset.UtcNow,
            null,
            [],
            []);

        store.AddEnrollment(enrollment);
        return OperationResult<Enrollment>.Success(enrollment);
    }

    public OperationResult<Enrollment> CompleteModule(int enrollmentId, int moduleId)
    {
        var enrollment = FindEnrollment(enrollmentId);
        if (enrollment is null)
        {
            return OperationResult<Enrollment>.Missing("Enrollment not found.");
        }

        var course = GetCourse(enrollment.CourseId)!;
        if (course.Modules.All(module => module.Id != moduleId))
        {
            return OperationResult<Enrollment>.Failure("Module does not belong to this course.");
        }

        if (!enrollment.CompletedModuleIds.Contains(moduleId))
        {
            enrollment.CompletedModuleIds.Add(moduleId);
        }

        var updated = RefreshCompletion(enrollment);
        store.ReplaceEnrollment(updated);
        return OperationResult<Enrollment>.Success(updated);
    }

    public OperationResult<Enrollment> SubmitQuiz(int enrollmentId, SubmitQuizRequest request)
    {
        var enrollment = FindEnrollment(enrollmentId);
        if (enrollment is null)
        {
            return OperationResult<Enrollment>.Missing("Enrollment not found.");
        }

        var course = GetCourse(enrollment.CourseId)!;
        var score = course.Quiz.Count(question =>
            request.Answers.TryGetValue(question.Id, out var answer) &&
            answer.Trim().Equals(question.Answer, StringComparison.OrdinalIgnoreCase));

        var percent = course.Quiz.Count == 0 ? 0 : (score * 100.0 / course.Quiz.Count);
        var passed = percent >= course.PassMark;

        enrollment.QuizSubmissions.Add(new QuizSubmission(store.NextSubmissionId(), DateTimeOffset.UtcNow, score, course.Quiz.Count, passed));

        var updated = RefreshCompletion(enrollment);
        store.ReplaceEnrollment(updated);
        return OperationResult<Enrollment>.Success(updated);
    }

    public OperationResult<LearnerDashboard> GetLearnerDashboard(string email)
    {
        var enrollments = store.Enrollments
            .Where(enrollment => enrollment.LearnerEmail.Equals(email, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (enrollments.Count == 0)
        {
            return OperationResult<LearnerDashboard>.Missing("Learner not found.");
        }

        var first = enrollments.First();
        var courses = enrollments.Select(enrollment =>
        {
            var course = GetCourse(enrollment.CourseId)!;
            var latestScore = enrollment.QuizSubmissions.OrderByDescending(submission => submission.SubmittedAt).FirstOrDefault()?.Score;
            return new LearnerCourseProgress(
                enrollment.Id,
                course.Code,
                course.Title,
                enrollment.Status.ToString(),
                CalculateProgress(enrollment, course),
                latestScore,
                enrollment.Status == EnrollmentStatus.Completed);
        }).ToList();

        return OperationResult<LearnerDashboard>.Success(new LearnerDashboard(
            first.LearnerName,
            first.LearnerEmail,
            enrollments.Count(enrollment => enrollment.Status == EnrollmentStatus.Active),
            enrollments.Count(enrollment => enrollment.Status == EnrollmentStatus.Completed),
            courses));
    }

    public IReadOnlyCollection<CoursePerformance> GetCoursePerformance()
    {
        return store.Courses.Select(course =>
        {
            var enrollments = store.Enrollments.Where(enrollment => enrollment.CourseId == course.Id).ToList();
            var submissions = enrollments.SelectMany(enrollment => enrollment.QuizSubmissions).ToList();
            var completed = enrollments.Count(enrollment => enrollment.Status == EnrollmentStatus.Completed);
            var completionRate = enrollments.Count == 0 ? 0 : completed * 100.0 / enrollments.Count;
            var averageScore = submissions.Count == 0 ? 0 : submissions.Average(submission => submission.Score * 100.0 / submission.TotalQuestions);
            return new CoursePerformance(course.Code, course.Title, enrollments.Count, completed, Math.Round(completionRate, 1), Math.Round(averageScore, 1));
        }).ToList();
    }

    private Enrollment? FindEnrollment(int id) => store.Enrollments.FirstOrDefault(enrollment => enrollment.Id == id);

    private Enrollment RefreshCompletion(Enrollment enrollment)
    {
        var course = GetCourse(enrollment.CourseId)!;
        var allModulesComplete = course.Modules.All(module => enrollment.CompletedModuleIds.Contains(module.Id));
        var passedQuiz = enrollment.QuizSubmissions.Any(submission => submission.Passed);

        return allModulesComplete && passedQuiz
            ? enrollment with { Status = EnrollmentStatus.Completed, CompletedAt = DateTimeOffset.UtcNow }
            : enrollment;
    }

    private static CourseSummary ToSummary(Course course) =>
        new(course.Id, course.Code, course.Title, course.Level.ToString(), course.Modules.Count, course.Modules.Sum(module => module.EstimatedMinutes), course.PassMark);

    private static double CalculateProgress(Enrollment enrollment, Course course)
    {
        var moduleWeight = 0.75;
        var quizWeight = 0.25;
        var moduleProgress = course.Modules.Count == 0 ? 0 : enrollment.CompletedModuleIds.Count * 100.0 / course.Modules.Count;
        var quizProgress = enrollment.QuizSubmissions.Any(submission => submission.Passed) ? 100 : 0;
        return Math.Round(moduleProgress * moduleWeight + quizProgress * quizWeight, 1);
    }
}
