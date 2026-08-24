# Smart Resume Analyzer

**Smart Resume Analyzer** is an ASP.NET Core 8 MVC web application that analyzes resumes against target skills and provides ATS-style scoring and improvement feedback.

## Features

* Supabase authentication and user profiles
* PDF resume upload and text extraction
* ATS-style resume scoring
* Matched and missing skill analysis
* Actionable resume improvement suggestions
* Resume analysis history
* Profile and account management
* Responsive Razor-based interface
* Supabase Storage for uploaded resumes

## Technology Stack

* **Backend:** ASP.NET Core 8, C#
* **Frontend:** Razor Views, HTML, CSS, JavaScript
* **Authentication & Storage:** Supabase
* **PDF Processing:** UglyToad.PdfPig
* **Documents:** DocumentFormat.OpenXml
* **Machine Learning:** Microsoft.ML

## Project Structure

```text
Controllers/    MVC controllers
Models/         View models and data structures
Services/       Application and business logic
Views/          Razor UI views
wwwroot/        CSS, JavaScript and static assets
Program.cs      Application configuration and routing
```

## Getting Started

### Requirements

* .NET 8 SDK
* Git
* Supabase project for authentication and storage

### Run Locally

```bash
git clone https://github.com/cloudwiseOrg/resume_analyzer.git
cd resume_analyzer
dotnet restore
dotnet build
dotnet run
```

Open the HTTPS URL displayed in the terminal.

## Supabase Configuration

Configure the following settings in `appsettings.Development.json` or environment variables:

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

> **Security:** Never commit real Supabase credentials or secrets to the repository.

## Main Pages

| Page                | Purpose                      |
| ------------------- | ---------------------------- |
| `/Home/Index`       | Landing page                 |
| `/Home/Login`       | User authentication          |
| `/Home/Register`    | Account registration         |
| `/Home/Analyze`     | Resume upload and analysis   |
| `/Home/Result`      | Analysis results             |
| `/Home/Profile`     | Profile and analysis history |
| `/Home/EditProfile` | Update profile information   |
| `/Home/ViewRecord`  | View saved analysis          |

## Team

* tmafunisa24-sudo
* katlehoMalekeCUT
* Kananelo259
* Tsebano

## Future Improvements

* DOCX resume support
* Improved ATS scoring accuracy
* Enhanced machine learning recommendations
* Unit and integration testing
* History filtering and pagination
* Improved user feedback and progress indicators
* Secure environment-based configuration
* Learning model for more personalized resume recommendations
