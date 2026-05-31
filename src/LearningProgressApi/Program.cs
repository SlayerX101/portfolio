using LearningProgressApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<LearningStore>();
builder.Services.AddSingleton<LearningService>();

var app = builder.Build();

app.MapGet("/", () => Results.Ok(new
{
    name = "Learning Progress API",
    description = "Advanced learning platform API with courses, enrollments, module completion, quiz scoring, certificates, and progress analytics.",
    endpoints = new[]
    {
        "GET /courses",
        "POST /courses",
        "POST /enrollments",
        "POST /enrollments/{id}/modules/{moduleId}/complete",
        "POST /enrollments/{id}/quiz-submissions",
        "GET /learners/{email}/dashboard",
        "GET /reports/course-performance"
    }
}));

var courses = app.MapGroup("/courses");

courses.MapGet("/", (LearningService service, string? level, string? search) =>
    Results.Ok(service.SearchCourses(level, search)));

courses.MapGet("/{id:int}", (LearningService service, int id) =>
{
    var course = service.GetCourse(id);
    return course is null ? Results.NotFound(new { message = "Course not found." }) : Results.Ok(course);
});

courses.MapPost("/", (LearningService service, CreateCourseRequest request) =>
{
    var result = service.CreateCourse(request);
    return result.IsSuccess ? Results.Created($"/courses/{result.Value!.Id}", result.Value) : Results.BadRequest(new { errors = result.Errors });
});

var enrollments = app.MapGroup("/enrollments");

enrollments.MapGet("/", (LearningService service) => Results.Ok(service.GetEnrollments()));

enrollments.MapPost("/", (LearningService service, EnrollLearnerRequest request) =>
{
    var result = service.Enroll(request);
    return result.IsSuccess ? Results.Created($"/enrollments/{result.Value!.Id}", result.Value) : Results.BadRequest(new { errors = result.Errors });
});

enrollments.MapPost("/{id:int}/modules/{moduleId:int}/complete", (LearningService service, int id, int moduleId) =>
{
    var result = service.CompleteModule(id, moduleId);
    return ToHttpResult(result);
});

enrollments.MapPost("/{id:int}/quiz-submissions", (LearningService service, int id, SubmitQuizRequest request) =>
{
    var result = service.SubmitQuiz(id, request);
    return ToHttpResult(result);
});

app.MapGet("/learners/{email}/dashboard", (LearningService service, string email) =>
{
    var result = service.GetLearnerDashboard(email);
    return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(new { errors = result.Errors });
});

app.MapGet("/reports/course-performance", (LearningService service) => Results.Ok(service.GetCoursePerformance()));

app.Run();

static IResult ToHttpResult<T>(OperationResult<T> result)
{
    if (result.IsSuccess)
    {
        return Results.Ok(result.Value);
    }

    return result.NotFound ? Results.NotFound(new { errors = result.Errors }) : Results.BadRequest(new { errors = result.Errors });
}
