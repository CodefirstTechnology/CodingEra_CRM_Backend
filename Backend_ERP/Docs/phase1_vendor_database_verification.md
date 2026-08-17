# Phase 1 Vendor Master — Database Verification Report

This report verifies all PostgreSQL database tables, columns, data types, nullability, primary keys, foreign keys, and indexes for Phase 1 Vendor Master created by migration `20260823000000_AddProcurementVendorMaster`.

---

## 1. Table Verification

| Table Name | PostgreSQL Type | Nullable | Primary Key / Index / FK | Frontend Model Source | Purpose |
|---|---|---|---|---|---|
| `vendor_document_sequences` | `Id` (integer) | No | PK Identity | `vendor.models.ts` | Sequence generator for `VEN-######` codes |
| `vendor_document_sequences` | `Prefix` (varchar 64) | No | UNIQUE INDEX | `vendor.models.ts` | Sequence prefix (`VEN`) |
| `vendor_document_sequences` | `LastSequence` (integer) | No | None | `vendor.models.ts` | Last incremented integer sequence |
| `vendor_payment_terms` | `Id` (integer) | No | PK Identity | `vendor.models.ts` | Master table for payment terms |
| `vendor_payment_terms` | `Code` (varchar 64) | No | UNIQUE INDEX | `vendor.models.ts` | Term code (`NET30`, `ADV100`, etc.) |
| `vendor_payment_terms` | `Name` (varchar 128) | No | None | `vendor.models.ts` | Term label (`Net 30 Days`) |
| `vendors` | `Id` (integer) | No | PK Identity | `vendor.models.ts` | Main Vendor Master table |
| `vendors` | `VendorCode` (varchar 64) | No | UNIQUE INDEX | `vendor.models.ts` | Unique vendor code (`VEN-######`) |
| `vendors` | `Name` (varchar 256) | No | INDEX | `vendor.models.ts` | Vendor primary name (`vendorName`) |
| `vendors` | `LegalName` (varchar 256) | Yes | None | `vendor.models.ts` | Company legal name (`companyName`) |
| `vendors` | `GSTIN` (varchar 32) | Yes | INDEX | `vendor.models.ts` | GST registration number (`gstNumber`) |
| `vendors` | `PAN` (varchar 32) | Yes | INDEX | `vendor.models.ts` | Income tax PAN number (`panNumber`) |
| `vendors` | `Email` (varchar 256) | Yes | None | `vendor.models.ts` | Vendor primary email address |
| `vendors` | `Phone` (varchar 64) | Yes | None | `vendor.models.ts` | Vendor primary phone number |
| `vendors` | `PaymentTermId` (integer) | Yes | FK -> `vendor_payment_terms("Id")` | `vendor.models.ts` | Payment term foreign key |
| `vendors` | `CreditLimit` (numeric 18,4) | No | DEFAULT 0 | `vendor.models.ts` | Financial credit limit amount |
| `vendors` | `Status` (varchar 64) | No | INDEX | `vendor.models.ts` | Vendor status (`Draft`, `Active`, etc.) |
| `vendor_contacts` | `Id` (integer) | No | PK Identity | `vendor-phase2.models.ts` | Vendor contacts child table |
| `vendor_contacts` | `VendorId` (integer) | No | FK -> `vendors("Id")` ON DELETE CASCADE | `vendor-phase2.models.ts` | Link to parent vendor |
| `vendor_addresses` | `Id` (integer) | No | PK Identity | `vendor-phase2.models.ts` | Vendor address entries child table |
| `vendor_addresses` | `VendorId` (integer) | No | FK -> `vendors("Id")` ON DELETE CASCADE | `vendor-phase2.models.ts` | Link to parent vendor |
| `vendor_compliances` | `Id` (integer) | No | PK Identity | `vendor-phase2.models.ts` | Vendor compliance details table |
| `vendor_compliances` | `VendorId` (integer) | No | UNIQUE INDEX, FK -> `vendors("Id")` ON DELETE CASCADE | `vendor-phase2.models.ts` | 1-to-1 link to parent vendor |
| `vendor_status_histories` | `Id` (integer) | No | PK Identity | `vendor.models.ts` | Audit history of status changes |

---

## 2. Foreign Key & Constraint Rules
- **Header Delete Behavior**: `vendors.PaymentTermId` uses `ON DELETE RESTRICT` to prevent accidental deletion of referenced payment terms.
- **Child Collections**: `vendor_contacts`, `vendor_addresses`, `vendor_compliances`, `vendor_status_histories` use `ON DELETE CASCADE` so deleting a vendor cleanly purges child records.
- **Numeric Precision**: Financial `CreditLimit` uses `numeric(18,4)` to prevent floating-point precision loss.
