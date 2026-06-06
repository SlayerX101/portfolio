# Junior C# Developer Portfolio

This workspace contains an editable junior C# developer CV, nine ASP.NET Core Web API projects, and a static portfolio website that presents the work professionally.

## Structure

```text
junior-csharp-portfolio/
  docs/
    Junior_CSharp_Developer_CV.md
    Junior_CSharp_Developer_CV.html
  portfolio-site/
    index.html
    styles.css
    assets/
      api-dashboard.svg
  greendesk-ops/
    index.html
    styles.css
    app.js
  src/
    GreenDeskOpsApi/
    ServiceDeskProApi/
    InventoryOrdersApi/
    LearningProgressApi/
    TaskTrackerApi/
    ExpenseTrackerApi/
    LibraryApi/
    JobApplicationTrackerApi/
    WeatherJournalApi/
```

## Advanced Projects

| Project | Recruiter signal | Example endpoints |
| --- | --- | --- |
| GreenDesk Ops API | Service desk, green asset register, maintenance planning, SLA risk, dashboard analytics | `GET /dashboard`, `POST /tickets`, `PATCH /assets/{id}/health` |
| Service Desk Pro API | SLA rules, ticket workflow, assignment, comments, audit trail, dashboard metrics | `GET /dashboard`, `PATCH /tickets/{id}/status`, `GET /tickets/{id}/audit` |
| Inventory Orders API | Stock management, order totals, VAT, cancellation rules, low-stock alerts, sales reports | `POST /orders`, `POST /products/{id}/stock`, `GET /reports/sales` |
| Learning Progress API | Enrollments, module completion, quiz scoring, learner dashboards, course analytics | `POST /enrollments`, `POST /quiz-submissions`, `GET /reports/course-performance` |

## Foundation Projects

| Project | Focus | Example endpoints |
| --- | --- | --- |
| Task Tracker API | CRUD, filtering, summary reporting | `GET /tasks`, `POST /tasks`, `GET /tasks/summary` |
| Expense Tracker API | Budget entries and reports | `GET /expenses`, `POST /expenses`, `GET /reports/categories` |
| Library API | Books, members, loans, returns | `GET /books`, `POST /loans`, `POST /loans/{id}/return` |
| Job Application Tracker API | Job-search workflow tracking | `GET /applications`, `PATCH /applications/{id}/status`, `GET /applications/stats` |
| Weather Journal API | Weather logs and city summaries | `GET /entries`, `POST /entries`, `GET /cities/{city}/summary` |

## Run An API

From this folder:

```powershell
dotnet run --project .\src\TaskTrackerApi\TaskTrackerApi.csproj
```

Then open the shown local URL and test endpoints such as:

```text
GET /tasks
GET /tasks/summary
POST /tasks
```

The APIs use in-memory lists, so data resets when the app restarts. That keeps the samples simple and easy to review.

To run one of the advanced APIs:

```powershell
dotnet run --project .\src\GreenDeskOpsApi\GreenDeskOpsApi.csproj
dotnet run --project .\src\ServiceDeskProApi\ServiceDeskProApi.csproj
dotnet run --project .\src\InventoryOrdersApi\InventoryOrdersApi.csproj
dotnet run --project .\src\LearningProgressApi\LearningProgressApi.csproj
```

## Portfolio Website

Open `index.html` in a browser. The root-level `index.html`, `styles.css`, `app.js`, and `assets/` files are prepared for GitHub Pages hosting.

The older `portfolio-site/` folder is kept as the working source copy used during local development.

The website now includes two live browser labs. Visitors can add records and test the workflows for the task tracker, expense tracker, library manager, job application tracker, and weather journal directly on the page. The advanced lab also lets visitors try simulated Service Desk, Inventory Orders, and Learning Progress workflows with dashboard metrics, validation-style feedback, stock rules, SLA signals, and learner analytics.

The demos use front-end JavaScript state so the portfolio works online as a static GitHub Pages site; when the C# APIs are hosted, the same forms can be wired to real deployed Web API endpoints.

GreenDesk Ops is a standalone dark blue/green dashboard hosted from `greendesk-ops/index.html`. It demonstrates the newest C# Web API project with a JavaScript service desk, asset register, maintenance workflow, charts, metrics, filtering, and JSON export.

For local testing through a web server:

```powershell
python -m http.server 8765
```

Then open:

```text
http://localhost:8765
```

## Customization Checklist

- Replace the GitHub, LinkedIn, and portfolio placeholders when those links are ready.
- Add screenshots or deployment links when you host the APIs.
- Push the folder to GitHub and add the repository links to the website project cards.
- If you want database experience, extend one API with Entity Framework Core and SQLite or SQL Server.
