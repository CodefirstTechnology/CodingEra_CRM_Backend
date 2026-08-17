# Phase 1 — Vendor Master Frontend Discovery Report

This discovery report documents all Vendor Master models, form controls, API routes, workflows, permissions, and validations found in the `CodingEra_CRM_Sales` frontend (`Frontend_Sales/src/app/features/vendors`).

---

## A. Frontend Files Inspected

1. **Models & Enums**:
   - [`Frontend_Sales/src/app/features/vendors/models/vendor.models.ts`](file:///e:/Codefirst/crmProject/CodingEra_CRM_Sales/Frontend_Sales/src/app/features/vendors/models/vendor.models.ts)
   - [`Frontend_Sales/src/app/features/vendors/models/vendor-phase2.models.ts`](file:///e:/Codefirst/crmProject/CodingEra_CRM_Sales/Frontend_Sales/src/app/features/vendors/models/vendor-phase2.models.ts)
   - [`Frontend_Sales/src/app/features/vendors/models/vendor-api.models.ts`](file:///e:/Codefirst/crmProject/CodingEra_CRM_Sales/Frontend_Sales/src/app/features/vendors/models/vendor-api.models.ts)
   - [`Frontend_Sales/src/app/features/vendors/models/vendor-permissions.ts`](file:///e:/Codefirst/crmProject/CodingEra_CRM_Sales/Frontend_Sales/src/app/features/vendors/models/vendor-permissions.ts)
   - [`Frontend_Sales/src/app/features/vendors/models/vendor-report.models.ts`](file:///e:/Codefirst/crmProject/CodingEra_CRM_Sales/Frontend_Sales/src/app/features/vendors/models/vendor-report.models.ts)

2. **Services & API Clients**:
   - [`Frontend_Sales/src/app/features/vendors/services/vendor-api.ts`](file:///e:/Codefirst/crmProject/CodingEra_CRM_Sales/Frontend_Sales/src/app/features/vendors/services/vendor-api.ts)
   - [`Frontend_Sales/src/app/features/vendors/services/vendor-http.service.ts`](file:///e:/Codefirst/crmProject/CodingEra_CRM_Sales/Frontend_Sales/src/app/features/vendors/services/vendor-http.service.ts)
   - [`Frontend_Sales/src/app/features/vendors/services/vendor-mock.service.ts`](file:///e:/Codefirst/crmProject/CodingEra_CRM_Sales/Frontend_Sales/src/app/features/vendors/services/vendor-mock.service.ts)
   - [`Frontend_Sales/src/app/features/vendors/services/vendor.service.ts`](file:///e:/Codefirst/crmProject/CodingEra_CRM_Sales/Frontend_Sales/src/app/features/vendors/services/vendor.service.ts)
   - [`Frontend_Sales/src/app/features/vendors/services/vendor.providers.ts`](file:///e:/Codefirst/crmProject/CodingEra_CRM_Sales/Frontend_Sales/src/app/features/vendors/services/vendor.providers.ts)

3. **Routes, Guards & Utilities**:
   - [`Frontend_Sales/src/app/features/vendors/vendor.routes.ts`](file:///e:/Codefirst/crmProject/CodingEra_CRM_Sales/Frontend_Sales/src/app/features/vendors/vendor.routes.ts)
   - [`Frontend_Sales/src/app/features/vendors/guards/vendor.guard.ts`](file:///e:/Codefirst/crmProject/CodingEra_CRM_Sales/Frontend_Sales/src/app/features/vendors/guards/vendor.guard.ts)
   - [`Frontend_Sales/src/app/features/vendors/utils/vendor-validation.util.ts`](file:///e:/Codefirst/crmProject/CodingEra_CRM_Sales/Frontend_Sales/src/app/features/vendors/utils/vendor-validation.util.ts)

---

## B. Frontend Entities & Models

### 1. `Vendor` (Main Aggregate Model)
- `id` (number, required)
- `vendorCode` (string, required)
- `vendorName` / `companyName` (string, required)
- `vendorType` (`Manufacturer`, `Supplier`, `Distributor`, `Contractor`, `Service Provider`, `Transporter`, `Consultant`, `Other`)
- `vendorCategory` (`Local`, `Domestic`, `International`)
- `status` (`Draft`, `Pending Approval`, `Approved`, `Rejected`, `Active`, `Inactive`, `Blocked`)
- `rating` (1 | 2 | 3 | 4 | 5)
- `primaryContact`, `designation`, `phone`, `mobile`, `email`, `website`
- `billingAddress`, `shippingAddress`, `sameAsBilling` (boolean)
- `gstNumber`, `panNumber`, `tanNumber`, `msmeRegistration`, `cinNumber`
- `paymentTerms` (`Net 15`, `Net 30`, `Net 45`, `Net 60`, `Advance 100%`, `50% Advance / 50% on Delivery`, `Due on Receipt`)
- `creditLimit` (number), `currency` (`INR`, `USD`, `EUR`, `AED`, `GBP`)
- `bank` (`bankName`, `branch`, `accountNumber`, `ifsc`, `swiftCode`)
- `industry`, `businessType`, `annualTurnover`, `yearsInBusiness`, `employeeCount`, `preferredCommunication`
- `remarks`, `internalNotes`
- Nested Collections: `contacts`, `addresses`, `bankAccounts`, `documents`, `tags`, `notes`, `approvalHistory`, `timeline`

---

## C. Frontend Form Controls & Validations

- **`vendorName`**: Required, Min length 3, Max length 256.
- **`vendorType`**: Required, Default `'Supplier'`.
- **`vendorCategory`**: Required, Default `'Domestic'`.
- **`status`**: Default `'Draft'`.
- **`email`**: Required, Valid email pattern.
- **`phone` / `mobile`**: Required, 10-15 digits regex.
- **`gstNumber`**: 15 characters uppercase regex `^[0-9]{2}[A-Z]{5}[0-9]{4}[A-Z]{1}[1-9A-Z]{1}Z[0-9A-Z]{1}$`.
- **`panNumber`**: 10 characters uppercase regex `^[A-Z]{5}[0-9]{4}[A-Z]{1}$`.
- **`creditLimit`**: Number $\ge 0$.

---

## D. Frontend API Inventory

| HTTP Verb | Frontend Route | Request Payload DTO | Response Payload DTO | Purpose |
|---|---|---|---|---|
| `GET` | `/api/vendors` | `VendorListQueryDto` | `VendorListItemDto[]` / `PagedResult` | Search & filter vendors |
| `GET` | `/api/vendors/{id}` | N/A | `VendorDto` | Fetch single vendor details |
| `POST` | `/api/vendors` | `VendorCreateRequestDto` | `VendorDto` | Create vendor |
| `PUT` | `/api/vendors/{id}` | `VendorUpdateRequestDto` | `VendorDto` | Update vendor |
| `DELETE` | `/api/vendors/{id}` | N/A | `void` | Soft-delete vendor |
| `POST` | `/api/vendors/{id}/activate` | `{ remarks }` | `VendorDto` | Activate vendor |
| `POST` | `/api/vendors/{id}/deactivate` | `{ remarks }` | `VendorDto` | Deactivate vendor |
| `POST` | `/api/vendors/{id}/workflow` | `VendorWorkflowRequestDto` | `VendorDto` | Apply status transition workflow |
| `POST` | `/api/vendors/check-duplicates` | `VendorDuplicateCheckDto` | `VendorDuplicateCheckResultDto` | Check code & GST uniqueness |
| `GET` | `/api/vendors/next-code` | N/A | `string` (`VEN-######`) | Auto-generate next vendor code |
| `GET` | `/api/vendors/dashboard` | N/A | `VendorDashboardSummaryDto` | Fetch dashboard KPI summary |

---

## E. Frontend Workflows & Status Transitions

- `Draft` $\rightarrow$ `Pending Approval` $\rightarrow$ `Approved` / `Rejected` $\rightarrow$ `Active` $\rightarrow$ `Inactive` / `Blocked` $\rightarrow$ `Active`.

---

## F. Frontend Permissions

- `vendors.view`, `vendors.create`, `vendors.update`, `vendors.delete`, `vendors.workflow`, `vendors.export`.

---

## G. Frontend Contract Extraction Table

| Frontend Property | Type | Required | API Payload Field | Backend DTO Field | PostgreSQL Column |
|---|---|---|---|---|---|
| `id` | number | Yes | `id` | `Id` | `"Id"` (integer PK) |
| `vendorCode` | string | Yes | `vendorCode` / `code` | `VendorCode` / `Code` | `"Code"` (varchar 64) |
| `vendorName` | string | Yes | `vendorName` / `name` | `VendorName` / `Name` | `"Name"` (varchar 256) |
| `companyName` | string | No | `companyName` / `legalName` | `LegalName` | `"LegalName"` (varchar 256) |
| `gstNumber` | string | No | `gstNumber` / `gstin` | `GSTIN` | `"GSTIN"` (varchar 32) |
| `panNumber` | string | No | `panNumber` / `pan` | `PAN` | `"PAN"` (varchar 32) |
| `email` | string | Yes | `email` | `Email` | `"Email"` (varchar 256) |
| `phone` | string | Yes | `phone` | `Phone` | `"Phone"` (varchar 64) |
| `status` | string | Yes | `status` | `Status` | `"Status"` (varchar 64) |
| `creditLimit` | number | No | `creditLimit` | `CreditLimit` | `"CreditLimit"` (numeric 18,4) |
