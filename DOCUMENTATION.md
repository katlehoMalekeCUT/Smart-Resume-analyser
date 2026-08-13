# Smart Resume Analyzer Documentation

## Overview

Smart Resume Analyzer is a resume review application built with ASP.NET Core MVC and Supabase. The app lets users upload a PDF resume, compare it against a target skills list, and receive a score with practical suggestions.

## Goals

- Enable secure Supabase registration and login
- Accept PDF resume uploads
- Extract text from uploaded PDF resumes
- Compare resume content against user-provided skills
- Calculate an ATS-style score
- Show missing skills and improvement suggestions
- Save analysis history for returning users
- Display profile and record details on a history page

## Architecture

The application follows a standard MVC design:

- Presentation layer: Razor views, HTML, CSS, JavaScript
- Application layer: controllers and service classes
- Data layer: Supabase Auth, Storage, and REST APIs

### Project structure

- `Controllers/` — routes and page actions
- `Models/` — view models and persisted record models
- `Services/` — business logic, PDF parsing, scoring, and Supabase calls
- `Views/` — Razor pages for user-facing screens
- `wwwroot/` — static assets such as CSS and JavaScript

## Core services

### `SupabaseAuthService`

Handles Supabase integration:

- user registration and login
- refresh token exchange
- profile metadata retrieval and updates
- file uploads to Supabase Storage
- public and signed file URL generation
- saving and retrieving analysis records
- deleting saved records

### `ResumeParserService`

Reads uploaded PDF files with `UglyToad.PdfPig` and returns the extracted text.

### `ATSScoringService`

Analyzes resume content and target skills to produce:

- ATS score
- matched skills
- missing skills
- improvement suggestions
- a career recommendation

### `KeywordAnalyzerService`

Extracts keyword lists from text and helps compare resume content to job descriptions.

## Authentication flow

1. User registers with email and password.
2. User logs in and receives access and refresh tokens.
3. The app stores tokens in secure cookie properties.
4. Token refresh logic keeps sessions active.
5. User profile claims are updated from Supabase metadata.

## Resume analysis flow

1. User uploads a PDF and enters target skills.
2. `ResumeParserService` parses text from the PDF.
3. `ATSScoringService` compares the text to the skill list.
4. The app generates a score, missing skills, and suggestions.
5. If the user is logged in, the file is uploaded to Supabase Storage.
6. The analysis record is saved to Supabase.
7. The result page displays the score, matched skills, and recommendations.

## Data model

### `ResumeResult`

Returned after analysis and displayed on result pages.

- `ATSScore`
- `MatchedSkills`
- `MissingSkills`
- `Suggestions`
- `CareerRecommendation`

### `UserRecord`

Represents a persisted analysis record in Supabase.

- `Id`
- `UserId`
- `UserEmail`
- `FilePath`
- `FileName`
- `Notes`
- `Suggestions`
- `ATSScore`
- `JobDescription`
- `CreatedAt`

## Required Supabase setup

Use this configuration in `appsettings.json`:

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

### Recommended `userRecords` table fields

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

### Requirements

- .NET 8 SDK
- Git
- Supabase project with Auth and Storage

### Start the app

```bash
git clone https://github.com/cloudwiseOrg/resume_analyzer.git
cd resume_analyzer-main
dotnet restore
dotnet build
dotnet run --project SmartResumeAnalyzer.csproj
```

Open the URL shown in the terminal.

## Recommended workflow

- Keep `main` stable
- Use descriptive feature branches
- Commit with meaningful messages
- Open pull requests for review

## Team

- `tmafunisa24-sudo` — lead developer
- Add collaborators here as needed

## Troubleshooting

- `dotnet restore` fails: ensure .NET 8 is installed
- Login issues: confirm Supabase `Url` and `AnonKey`
- Upload issues: use a valid PDF file
- History issues: verify Supabase storage and table schema

## Future improvements

- Add `.docx` resume upload support
- Add unit and integration tests
- Improve scoring and suggestion quality
- Secure Supabase secrets with environment variables
- Add history filtering and pagination
- Improve progress feedback and UI messaging

## Notes

Keep secrets out of Git and use secure configuration for production.
