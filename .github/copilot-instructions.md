# Copilot Instructions

This file contains project-specific instructions and context to help GitHub Copilot provide better assistance for this workspace.

## Project Overview

**Workspace:** Vilog  
**Location:** F:\Temp\Vilog\

This project is a blogging platform that allows users to create, manage, and share blog posts. It includes features such as user authentication, post categorization, commenting, and social sharing.
It is also designed to support markdown formatting and media embedding.
The public part of the blog is statically rendered, while the admin is dynamic.

Blog components:
- Blog posts
- Categories
- History
- Search
- Tags
- Optional
	- Video links
	- Image galleries

## Technology Stack

- .NET and C#
- ASP.NET Core Blazor, Statically rendered for the public part, dynamic server for the administration UI.
- Blogging application built with Docker
- CosmosDB for data storage
- Azure Blob Storage for media files
- Telerik UI components for Blazor

## Architecture & Design Patterns

- Uses the Display-Facade-Repository pattern for data access and presentation
- The least amount of logic in the views the better

## Code Style & Conventions

### Naming Conventions
- Classes: PascalCase
- Methods: PascalCase
- Variables: camelCase
- Constants: PascalCase
- Private fields: _camelCase (with underscore prefix)

### Formatting
- Always use `CultureInfo.InvariantCulture` when calling `ToString()` with format specifiers (e.g., `.ToString("format", CultureInfo.InvariantCulture)`) to ensure consistent output regardless of system locale. This is especially important for month/date formatting, number formatting, and any culture-dependent string operations in Viblog library code.

## Development Guidelines

### Testing Requirements
- All logic features should be covered by unit tests
- Use protected for virtual methods to facilitate mocking
- xUnit, AutoMoq. Do Not use Fluent Assertions as there may be a licensing issue

### Error Handling
- Throw exceptions, but display a friendly error message to the user
- Log errors using Exceptionless
- Log detailed error information for debugging purposes
- 
## Dependencies & Libraries

### Preferred Libraries
- Telerik UI for Blazor

## Database & Data Access

- Use Entity Framework Core for data access, even with CosmosDB
- Use transactions where possible

## API Conventions

- TBD

## Important Notes for Copilot

- Always ensure backward compatibility unless explicitly told otherwise
- Follow existing patterns found in the codebase
- Add XML documentation comments for public methods and classes, including API endpoints
- Include appropriate error handling in all methods
- Write unit tests for new functionality

## Specific Do's and Don'ts

### Do:
- Follow the existing project structure
- Use async/await for I/O operations
- Validate inputs and handle edge cases
- Keep methods focused and single-purpose
- Use SASS for styling

### Don't:
- Mix different architectural patterns
- Add dependencies without consideration
- Ignore existing conventions
- Leave commented-out code
- Use bootstrap or any other CSS framework