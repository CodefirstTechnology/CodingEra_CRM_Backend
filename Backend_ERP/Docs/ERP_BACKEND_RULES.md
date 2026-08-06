# ERP_BACKEND_RULES.md

# CodingEra ERP Backend Development Rules

Version: 1.0

---

# 1. Scope

These rules apply to every ERP backend implementation.

Always follow these rules unless a phase explicitly overrides them.

---

# 2. Solution

Only modify

Backend_ERP

Never modify

Backend_CRM

Never break existing CRM functionality.

---

# 3. Technology

- .NET 8
- ASP.NET Core Web API
- EF Core 8
- PostgreSQL
- Swagger
- Dependency Injection
- Fluent API
- Repository Pattern
- Service Layer
- DTO Pattern

---

# 4. Project Structure

Follow the existing solution structure.

Backend_ERP.API

Backend_ERP.Application

Backend_ERP.Domain

Backend_ERP.Infrastructure

Backend_ERP.Shared

Backend_ERP.Tests

Never introduce a new architecture.

---

# 5. Reuse First

Before creating

- Entity
- DTO
- Repository
- Service
- Mapper
- Validator
- Numbering Service
- Audit
- History
- Workflow
- Helper
- Extension Method
- Lookup
- Utility

Search the ERP solution.

If similar functionality exists

reuse or extend it.

Never duplicate code.

---

# 6. Coding Style

Follow the same coding style already used.

Keep classes small.

Keep methods focused.

Prefer composition over duplication.

Use constructor injection.

Never use static business logic.

---

# 7. Domain Rules

Business logic belongs in

Application

Persistence belongs in

Infrastructure

Controllers should only coordinate requests.

---

# 8. DTO Rules

All APIs return DTOs.

Never expose EF entities.

Use camelCase JSON.

Keep DTOs compatible with Angular frontend contracts.

---

# 9. Repository Rules

Repositories

- Data access only
- No business logic
- Async only
- CancellationToken supported

Read queries should use

AsNoTracking()

when entities are not modified.

---

# 10. Service Rules

Services contain

- validation
- workflow
- calculations
- business rules

Repositories must never contain business rules.

---

# 11. Controllers

Controllers should

- stay thin
- call services
- return ActionResult
- return ValidationProblemDetails
- support CancellationToken

No business logic inside controllers.

---

# 12. Validation

Validate

- required fields
- enums
- workflow transitions
- references
- duplicate records
- business rules

Return proper HTTP status codes.

---

# 13. Workflow

Every workflow module must

record

- history
- audit
- timestamps
- user

Reuse existing workflow implementation whenever possible.

---

# 14. Numbering

Reuse the existing numbering infrastructure.

Create a new numbering service only if required.

Document numbers follow

PREFIX-YYYY-00001

Examples

SO

PI

ADV

PL

DA

QA

---

# 15. Audit

Whenever data changes

store

CreatedBy

CreatedDate

UpdatedBy

UpdatedDate

Status History

Audit Trail

when applicable.

---

# 16. Comments

Modules supporting workflow should reuse the common comments pattern.

Avoid creating different comment implementations.

---

# 17. Lookups

Reuse existing lookup endpoints.

Never duplicate lookup logic.

---

# 18. Search

List endpoints should support

page

pageSize

search

sortBy

sortDirection

where appropriate.

---

# 19. Performance

Use

AsNoTracking()

for read-only queries.

Avoid N+1 queries.

Project directly to DTOs where practical.

Use Include() only when necessary.

Prefer server-side filtering.

---

# 20. Database

Use

PostgreSQL

EF Core

Fluent API

Never use Data Annotations for mapping.

---

# 21. Migrations

Every phase creates

ONE migration.

Migration must be

idempotent.

Every

table

column

foreign key

index

must check existence before creation.

Migration should safely run multiple times.

---

# 22. Seeding

Temporary seed data is allowed only for development.

Seed

10 realistic records

unless the phase specifies otherwise.

Use CRM User IDs

1

2

3

where user references are required.

Seed only realistic business data.

After seeding

remove

- seed class
- Program.cs hook
- helper
- SQL script
- temporary extension

Database keeps data.

Repository keeps no seed code.

Never commit seed infrastructure.

---

# 23. Testing

Every module should include

happy path

workflow

validation

integration contract

tests.

Keep tests independent.

---

# 24. Swagger

Every endpoint must appear in Swagger.

Keep route naming consistent.

Use REST conventions.

---

# 25. Build

Every phase must successfully execute

dotnet restore

dotnet build

dotnet test

dotnet ef database update

with

0 errors

0 warnings

---

# 26. API Design

Prefer REST.

Use nouns.

Avoid verbs except workflow endpoints.

Examples

POST /approve

POST /reject

POST /cancel

POST /activate

---

# 27. Cross Module Integration

Never duplicate data already owned by another module.

Reference by

Id

Number

or

Code

where appropriate.

Reuse existing services whenever possible.

---

# 28. CRM Integration

CRM remains the owner of

Users

Authentication

Authorization

Quotations

Deals

Item Master

Company Profile

ERP references CRM data.

ERP should not duplicate CRM master data unless explicitly required.

---

# 29. Logging

Reuse existing logging.

Log

errors

warnings

important workflow actions.

Never log sensitive information.

---

# 30. Error Handling

Return consistent API responses.

Use ValidationProblemDetails for validation.

Use ProblemDetails for server errors.

Never expose stack traces.

---

# 31. Output

Every implementation must report

1. Files Created

2. Files Modified

3. Tables Created

4. Migration Name

5. API Endpoints

6. Swagger Verification

7. Seed Summary

8. Seed Removal Confirmation

9. Build Status

10. Remaining Work

---

# 32. General Principles

Keep the ERP backend

- clean
- modular
- reusable
- production-ready
- scalable
- maintainable

Always improve existing code before creating new implementations.

Prefer extending shared infrastructure over creating module-specific infrastructure.