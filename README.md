# Smart Resume Analyzer

An automated, enterprise-grade resume evaluation and optimization web application built with ASP.NET Core 8 MVC and Supabase BaaS (Backend-as-a-Service).Smart Resume Analyzer bridges the gap between job seekers and Applicant Tracking Systems (ATS). By programmatic extraction and algorithmic analysis, the platform parses PDF resumes, cross-references content against target technical matrices, and delivers deterministic scoring alongside actionable, data-driven optimization strategies.

## What this project does

- Lets users register and log in with Supabase email/password authentication
- Upload PDF resumes and store them in Supabase Storage
- Extract plain text from resume PDFs with `UglyToad.PdfPig`
- Compare resume content against user-provided skills
- Generate an ATS-style score, matched skills, missing skills, and improvement tips
- Save analysis history so users can review past checks
- Show profile details and record history for returning users

## Why it matters

Many resumes include strong experience but still miss key words that recruiters and applicant tracking systems expect. This app makes it easier to see where a resume is strong and which skills are worth emphasizing.

## Key features

- Secure Supabase authentication and session handling
- Resume upload with PDF parsing
- Skill matching and scoring
- Suggestions and career guidance based on resume content
- Saved analysis history for each user
- Profile page with recent resume records
- Responsive Razor-based UI

## Technology stack

- ASP.NET Core 8
- C# with MVC and Razor views
- Supabase Auth and Storage
- `UglyToad.PdfPig` for PDF parsing
- `DocumentFormat.OpenXml`
- `Microsoft.ML`

## Project structure

- `Controllers/` — handles web requests and page navigation
- `Models/` — data structures for views and records
- `Services/` — core logic, PDF parsing, scoring, and Supabase calls
- `Views/` — Razor templates for the UI
- `wwwroot/` — static assets such as CSS and JavaScript
- `Program.cs` — service registration and routing

## Team

- tmafunisa24-sudo 
- katlehoMalekeCUT
- Kananelo259
- Tsebano

## Supabase setup

Create a Supabase project and enable:

- Email/password authentication
- A storage bucket for resume files
- REST access to save analysis records(database)

Add these settings to `appsettings.json` or `appsettings.Development.json`:

```json
{
  "Supabase": {
    "Url": "https://YOUR-PROJECT.supabase.co",
    "AnonKey": "YOUR_SUPABASE_ANON_KEY",
    "BucketName": "documents",
    "UserRecordsTable": "userRecords"
  }
}
```

### Recommended table schema

Create a `userRecords` table with these fields:

- `id`
- `user_id`
- `user_email`
- `file_path`
- `file_name`
- `notes`
- `suggestions`
- `ats_score`
- `job_description`
- `created_at`

## Running locally

### Prerequisites

- .NET 8 SDK installed (check with `dotnet --version`)
- Git installed
- (Optional) Supabase project with Auth and Storage if you want full functionality

### Clone, build and run

```bash
git clone https://github.com/cloudwiseOrg/resume_analyzer.git
cd resume_analyzer
dotnet restore
dotnet build
dotnet test
dotnet run --project SmartResumeAnalyzer.csproj
```

Open the local URL shown in the terminal (usually https://localhost:xxxx).

### Configuration (Supabase)

This project requires Supabase settings for authentication and storage. You can provide these via `appsettings.Development.json` or environment variables.

Create `appsettings.Development.json` (copy from `appsettings.Development.json.example`) and fill in your values, or set env vars:

PowerShell
```powershell
$env:Supabase__Url = "https://your-project.supabase.co"
$env:Supabase__AnonKey = "YOUR_ANON_KEY"
$env:Supabase__BucketName = "documents"
dotnet run --project SmartResumeAnalyzer.csproj
```

bash
```bash
export Supabase__Url="https://your-project.supabase.co"
export Supabase__BucketName="documents"
dotnet run -- SmartResumeAnalyzer.csproj
```

If Supabase configuration is missing, the app will run in a limited local stub mode (no external uploads or persistent history), which is convenient for reviewers.

### Tests

Run unit and integration tests:

```bash
dotnet test
```

To run a specific test class or method use the `--filter` option. For example:

```bash
dotnet test --filter "ClassName=ATSScoringServiceTests"
```

### Notes

- Do not commit real Supabase secret keys to the repository. Use the example file or environment variables.
- If you get a build file locked error, stop any running `dotnet` processes and run `dotnet clean`.
- The `improvement/ready-for-production` branch contains minor improvements and safety fallbacks to make local runs easier for reviewers.

## Important pages

- `/Home/Index` — landing page
- `/Home/Login` — login form
- `/Home/Register` — registration page
- `/Home/Analyze` — resume upload page
- `/Home/Result` — analysis results page
- `/Home/Profile` — profile and history page
- `/Home/EditProfile` — update profile details
- `/Home/ViewRecord?id={recordId}` — view saved analysis details
- `/Home/Privacy` — privacy page

## Contributing

- Keep `main` stable
- Use feature branches such as `main/xxx`
- Commit with clear, meaningful messages
- Open pull requests for review

## Troubleshooting

- Build issues: verify .NET 8 SDK is installed
- Authentication issues: confirm Supabase URL and anon key
- Upload errors: use a valid PDF resume
- History issues: confirm Supabase storage permissions and record schema

## Future improvements

- Add `.docx` resume upload support
- Add unit and integration tests
- Improve scoring and suggestion accuracy
- Secure Supabase secrets with environment variables
- Add filtering and pagination for history
- Improve user progress feedback and UI messaging
