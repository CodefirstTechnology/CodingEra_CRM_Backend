# ERP_SALES_ROADMAP.md

# CodingEra ERP Sales Backend Roadmap

Version: 1.0

---

# Purpose

This document defines the implementation order, module ownership,
dependencies, integrations and completion criteria for the ERP Sales
Backend.

All implementations must also follow

ERP_BACKEND_RULES.md

---

# Technology

Framework

- .NET 8
- ASP.NET Core Web API

Database

- PostgreSQL

Architecture

- Domain
- Application
- Infrastructure
- API
- Shared
- Tests

---

# Backend Ownership

## CRM owns

Users

Authentication

Authorization

Deals

Quotations

Item Master

Company Profile

Lead Management

Contact Management

Organization Management

## ERP owns

Sales Orders

Proforma Invoices

Advance Payments

Sales Targets

Performance Dashboard

Price Lists

Discount Approval

Quotation Approval

Future Sales Reports

Future Sales Invoice Bridge

ERP references CRM data whenever possible.

Never duplicate CRM master data unless explicitly approved.

---

# Sales Flow

CRM Deal

↓

CRM Quotation

↓

Discount Approval (if required)

↓

Quotation Approval

↓

Quotation Approved

↓

Sales Order

↓

Advance Payment

↓

Proforma Invoice

↓

Sales Invoice (Future ERP/Accounting)

↓

Accounting

---

# Module Dependency

Quotation
│
├── Discount Approval
│
├── Quotation Approval
│
└── Sales Order
      │
      ├── Advance Payment
      │
      ├── Proforma Invoice
      │
      ├── Performance Dashboard
      │
      └── Sales Targets

Price Lists

↓

Discount Approval

↓

Quotation

↓

Sales Order

---

# Phase Roadmap

## Phase 1

Sales Orders

Status

Completed

Purpose

Sales Order foundation

Includes

- CRUD
- Workflow
- Status History
- Numbering
- Migration
- Swagger
- Tests

---

## Phase 2

Proforma Invoice

Status

Completed

Depends On

Sales Orders

Includes

- CRUD
- Approval
- Conversion
- Communication
- Reports

---

## Phase 3

Advance Payments

Status

Completed

Depends On

Sales Orders

Includes

- CRUD
- Receive
- Verify
- Apply
- Timeline
- Ledger

---

## Phase 4

Sales Targets

Status

Completed

Depends On

Sales Orders

Includes

- Target Assignment
- Progress
- Dashboard
- Reports

---

## Phase 5

Performance Dashboard

Status

Completed

Depends On

Sales Orders

Advance Payments

Proforma Invoices

Sales Targets

Includes

- KPIs
- Analytics
- Leaderboards
- Reports

---

## Phase 6

Price Lists

Status

Completed

Includes

- CRUD
- Price Resolution
- Clone
- Activation
- Compare

---

## Phase 7

Discount Approval

Status

Completed

Depends On

Price Lists

Sales Orders

CRM Quotations

Includes

- Approval Workflow
- Comments
- History
- Statistics

---

## Phase 8

Quotation Approval

Status

Completed

Depends On

CRM Quotations

Discount Approval

Sales Orders

Includes

- Review
- Approve
- Reject
- Revision
- Reopen
- Comments
- History

---

## Phase 9

Sales Module Production Hardening

Status

Completed

Depends On

All Previous Phases

Objectives

- Shared workflow infrastructure
- Shared audit infrastructure
- Shared history infrastructure
- Shared comments
- Shared numbering
- Shared validators
- Shared lookup services
- Common response models
- Performance optimization
- Index optimization
- API consistency
- Security review
- Pagination review
- Logging improvements
- Integration cleanup

No new business module should be created.

---

## Phase 10

Sales Module Final Production Readiness

Status

Pending

Depends On

All Previous Phases

Objectives

- Cross-module testing
- API contract verification
- Swagger review
- Migration verification
- Database optimization
- Foreign key verification
- Index verification
- Build verification
- Test verification
- Performance review
- Security review
- Final production audit
- Backend readiness report

No new database tables unless required for production fixes.

---

# Numbering Standards

Sales Order

SO-YYYY-00001

Proforma Invoice

PI-YYYY-00001

Advance Payment

ADV-YYYY-00001

Sales Target

ST-YYYY-00001

Price List

PL-YYYY-00001

Discount Approval

DA-YYYY-00001

Quotation Approval

QA-YYYY-00001

Future modules should follow

PREFIX-YYYY-00001

---

# Common Workflow

Draft

↓

Submitted

↓

Under Review

↓

Approved

Alternative

Rejected

Returned

Cancelled

Revision Required

Reopened

Every workflow module should maintain

History

Audit

Comments

Timestamps

User Information

---

# Shared Infrastructure

All modules should reuse

- Repository pattern
- Service pattern
- DTO mapping
- Validation
- Workflow engine
- Audit
- History
- Comments
- Numbering
- Pagination
- Lookup services
- Logging
- Exception handling

Do not duplicate implementations.

---

# Database Standards

Every module

- Uses Fluent API
- Uses EF Core migrations
- Uses PostgreSQL
- Creates one migration per phase
- Creates idempotent migrations
- Uses indexes for searchable fields
- Uses foreign keys where applicable

---

# API Standards

REST endpoints

camelCase JSON

Swagger documented

Pagination

Filtering

Sorting

ValidationProblemDetails

ProblemDetails

CancellationToken support

Async only

---

# Testing Standards

Every completed phase should include

- Build success
- Migration success
- CRUD tests
- Workflow tests
- Validation tests
- Contract tests
- Swagger verification

---

# Demo Data

Temporary demo data is allowed only during development.

Requirements

- Use CRM User IDs 1, 2 and 3
- Insert realistic business records
- Remove all seed infrastructure after execution
- Keep only database records

Never commit temporary seed code.

---

# Definition of Done

A phase is considered complete only if

- Build succeeds
- Tests pass
- Migration applies successfully
- Swagger shows all endpoints
- Demo data inserted
- Temporary seed code removed
- No duplicate implementations introduced
- Existing phases remain functional
- No changes made to Backend_CRM
- Production-ready code quality maintained