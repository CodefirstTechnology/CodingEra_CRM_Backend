using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Backend_ERP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountingFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "bank_recon_document_sequences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Prefix = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    LastSequence = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bank_recon_document_sequences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "bank_reconciliations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ReconciliationNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    BankName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    AccountNumber = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    StatementDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OpeningBalance = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    ClosingBalance = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    BookClosingBalance = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Difference = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    MatchedCount = table.Column<int>(type: "integer", nullable: false),
                    UnmatchedCount = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PaymentIdsJson = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    ReceiptIdsJson = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    Remarks = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bank_reconciliations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "customer_ledger_document_sequences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Prefix = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    LastSequence = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customer_ledger_document_sequences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "customer_ledger_entries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LedgerNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    CustomerName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    OpeningBalance = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Debit = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Credit = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    RunningBalance = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Outstanding = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    InvoiceId = table.Column<int>(type: "integer", nullable: true),
                    InvoiceNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ReceiptId = table.Column<int>(type: "integer", nullable: true),
                    ReceiptNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    SalesOrderId = table.Column<int>(type: "integer", nullable: true),
                    SalesOrderNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ProformaInvoiceId = table.Column<int>(type: "integer", nullable: true),
                    ProformaInvoiceNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    EntryType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    TransactionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AgeingDays = table.Column<int>(type: "integer", nullable: false),
                    AgeingBucket = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Remarks = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customer_ledger_entries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "gst_document_sequences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Prefix = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    LastSequence = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gst_document_sequences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "gst_returns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ReturnPeriod = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    GstCollected = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    GstPaid = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    NetPayable = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    TransactionCount = table.Column<int>(type: "integer", nullable: false),
                    FiledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FiledBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Remarks = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gst_returns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "gst_transactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GstNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    InvoiceId = table.Column<int>(type: "integer", nullable: true),
                    InvoiceNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CustomerId = table.Column<int>(type: "integer", nullable: true),
                    CustomerName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    VendorId = table.Column<int>(type: "integer", nullable: true),
                    VendorName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    TxnType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    TaxableValue = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Cgst = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Sgst = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Igst = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Cess = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    InvoiceDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReturnPeriod = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Remarks = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gst_transactions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "outstanding_records",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PartyType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    PartyId = table.Column<int>(type: "integer", nullable: false),
                    PartyName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    DocumentId = table.Column<int>(type: "integer", nullable: false),
                    DocumentNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    DocumentDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OriginalAmount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    PaidAmount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Outstanding = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    AgeingDays = table.Column<int>(type: "integer", nullable: false),
                    Bucket0To30 = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Bucket31To60 = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Bucket61To90 = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Bucket90Plus = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outstanding_records", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "payment_document_sequences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Prefix = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    LastSequence = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_document_sequences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "payment_entries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PaymentNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    VendorId = table.Column<int>(type: "integer", nullable: false),
                    VendorName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    PurchaseBillId = table.Column<int>(type: "integer", nullable: false),
                    PurchaseBillNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PaymentMode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    BankName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    BankAccount = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ChequeNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ReferenceNumber = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Currency = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Tds = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    NetAmount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Remarks = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_entries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "receipt_document_sequences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Prefix = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    LastSequence = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_receipt_document_sequences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "receipt_entries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ReceiptNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    CustomerName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    InvoiceId = table.Column<int>(type: "integer", nullable: false),
                    InvoiceNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    SalesOrderId = table.Column<int>(type: "integer", nullable: true),
                    SalesOrderNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ReceiptDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReceiptMode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    BankName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    BankAccount = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ReferenceNumber = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Currency = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Tds = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    NetAmount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Remarks = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_receipt_entries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "accounting_attachments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    SizeKb = table.Column<int>(type: "integer", nullable: false),
                    UploadedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CustomerLedgerEntryId = table.Column<int>(type: "integer", nullable: true),
                    GstTransactionId = table.Column<int>(type: "integer", nullable: true),
                    PaymentEntryId = table.Column<int>(type: "integer", nullable: true),
                    ReceiptEntryId = table.Column<int>(type: "integer", nullable: true),
                    BankReconciliationId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_accounting_attachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_accounting_attachments_bank_reconciliations_BankReconciliat~",
                        column: x => x.BankReconciliationId,
                        principalTable: "bank_reconciliations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_accounting_attachments_customer_ledger_entries_CustomerLedg~",
                        column: x => x.CustomerLedgerEntryId,
                        principalTable: "customer_ledger_entries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_accounting_attachments_gst_transactions_GstTransactionId",
                        column: x => x.GstTransactionId,
                        principalTable: "gst_transactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_accounting_attachments_payment_entries_PaymentEntryId",
                        column: x => x.PaymentEntryId,
                        principalTable: "payment_entries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_accounting_attachments_receipt_entries_ReceiptEntryId",
                        column: x => x.ReceiptEntryId,
                        principalTable: "receipt_entries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "accounting_notes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Text = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CustomerLedgerEntryId = table.Column<int>(type: "integer", nullable: true),
                    GstTransactionId = table.Column<int>(type: "integer", nullable: true),
                    PaymentEntryId = table.Column<int>(type: "integer", nullable: true),
                    ReceiptEntryId = table.Column<int>(type: "integer", nullable: true),
                    BankReconciliationId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_accounting_notes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_accounting_notes_bank_reconciliations_BankReconciliationId",
                        column: x => x.BankReconciliationId,
                        principalTable: "bank_reconciliations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_accounting_notes_customer_ledger_entries_CustomerLedgerEntr~",
                        column: x => x.CustomerLedgerEntryId,
                        principalTable: "customer_ledger_entries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_accounting_notes_gst_transactions_GstTransactionId",
                        column: x => x.GstTransactionId,
                        principalTable: "gst_transactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_accounting_notes_payment_entries_PaymentEntryId",
                        column: x => x.PaymentEntryId,
                        principalTable: "payment_entries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_accounting_notes_receipt_entries_ReceiptEntryId",
                        column: x => x.ReceiptEntryId,
                        principalTable: "receipt_entries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "accounting_timeline_events",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Action = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    User = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FromStatus = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ToStatus = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Remarks = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CustomerLedgerEntryId = table.Column<int>(type: "integer", nullable: true),
                    GstTransactionId = table.Column<int>(type: "integer", nullable: true),
                    GstReturnId = table.Column<int>(type: "integer", nullable: true),
                    PaymentEntryId = table.Column<int>(type: "integer", nullable: true),
                    ReceiptEntryId = table.Column<int>(type: "integer", nullable: true),
                    BankReconciliationId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_accounting_timeline_events", x => x.Id);
                    table.ForeignKey(
                        name: "FK_accounting_timeline_events_bank_reconciliations_BankReconci~",
                        column: x => x.BankReconciliationId,
                        principalTable: "bank_reconciliations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_accounting_timeline_events_customer_ledger_entries_Customer~",
                        column: x => x.CustomerLedgerEntryId,
                        principalTable: "customer_ledger_entries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_accounting_timeline_events_gst_returns_GstReturnId",
                        column: x => x.GstReturnId,
                        principalTable: "gst_returns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_accounting_timeline_events_gst_transactions_GstTransactionId",
                        column: x => x.GstTransactionId,
                        principalTable: "gst_transactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_accounting_timeline_events_payment_entries_PaymentEntryId",
                        column: x => x.PaymentEntryId,
                        principalTable: "payment_entries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_accounting_timeline_events_receipt_entries_ReceiptEntryId",
                        column: x => x.ReceiptEntryId,
                        principalTable: "receipt_entries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_accounting_attachments_BankReconciliationId",
                table: "accounting_attachments",
                column: "BankReconciliationId");

            migrationBuilder.CreateIndex(
                name: "IX_accounting_attachments_CustomerLedgerEntryId",
                table: "accounting_attachments",
                column: "CustomerLedgerEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_accounting_attachments_GstTransactionId",
                table: "accounting_attachments",
                column: "GstTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_accounting_attachments_PaymentEntryId",
                table: "accounting_attachments",
                column: "PaymentEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_accounting_attachments_ReceiptEntryId",
                table: "accounting_attachments",
                column: "ReceiptEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_accounting_notes_BankReconciliationId",
                table: "accounting_notes",
                column: "BankReconciliationId");

            migrationBuilder.CreateIndex(
                name: "IX_accounting_notes_CustomerLedgerEntryId",
                table: "accounting_notes",
                column: "CustomerLedgerEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_accounting_notes_GstTransactionId",
                table: "accounting_notes",
                column: "GstTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_accounting_notes_PaymentEntryId",
                table: "accounting_notes",
                column: "PaymentEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_accounting_notes_ReceiptEntryId",
                table: "accounting_notes",
                column: "ReceiptEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_accounting_timeline_events_BankReconciliationId",
                table: "accounting_timeline_events",
                column: "BankReconciliationId");

            migrationBuilder.CreateIndex(
                name: "IX_accounting_timeline_events_CustomerLedgerEntryId",
                table: "accounting_timeline_events",
                column: "CustomerLedgerEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_accounting_timeline_events_GstReturnId",
                table: "accounting_timeline_events",
                column: "GstReturnId");

            migrationBuilder.CreateIndex(
                name: "IX_accounting_timeline_events_GstTransactionId",
                table: "accounting_timeline_events",
                column: "GstTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_accounting_timeline_events_PaymentEntryId",
                table: "accounting_timeline_events",
                column: "PaymentEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_accounting_timeline_events_ReceiptEntryId",
                table: "accounting_timeline_events",
                column: "ReceiptEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_bank_recon_document_sequences_Prefix",
                table: "bank_recon_document_sequences",
                column: "Prefix",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_bank_reconciliations_ReconciliationNumber",
                table: "bank_reconciliations",
                column: "ReconciliationNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_bank_reconciliations_StatementDate",
                table: "bank_reconciliations",
                column: "StatementDate");

            migrationBuilder.CreateIndex(
                name: "IX_bank_reconciliations_Status",
                table: "bank_reconciliations",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_customer_ledger_document_sequences_Prefix",
                table: "customer_ledger_document_sequences",
                column: "Prefix",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_customer_ledger_entries_CustomerId",
                table: "customer_ledger_entries",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_customer_ledger_entries_EntryType",
                table: "customer_ledger_entries",
                column: "EntryType");

            migrationBuilder.CreateIndex(
                name: "IX_customer_ledger_entries_LedgerNumber",
                table: "customer_ledger_entries",
                column: "LedgerNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_customer_ledger_entries_TransactionDate",
                table: "customer_ledger_entries",
                column: "TransactionDate");

            migrationBuilder.CreateIndex(
                name: "IX_gst_document_sequences_Prefix",
                table: "gst_document_sequences",
                column: "Prefix",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_gst_returns_ReturnPeriod",
                table: "gst_returns",
                column: "ReturnPeriod",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_gst_transactions_GstNumber",
                table: "gst_transactions",
                column: "GstNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_gst_transactions_ReturnPeriod",
                table: "gst_transactions",
                column: "ReturnPeriod");

            migrationBuilder.CreateIndex(
                name: "IX_gst_transactions_Status",
                table: "gst_transactions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_gst_transactions_TxnType",
                table: "gst_transactions",
                column: "TxnType");

            migrationBuilder.CreateIndex(
                name: "IX_outstanding_records_DocumentNumber",
                table: "outstanding_records",
                column: "DocumentNumber");

            migrationBuilder.CreateIndex(
                name: "IX_outstanding_records_PartyType_PartyId",
                table: "outstanding_records",
                columns: new[] { "PartyType", "PartyId" });

            migrationBuilder.CreateIndex(
                name: "IX_outstanding_records_Status",
                table: "outstanding_records",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_payment_document_sequences_Prefix",
                table: "payment_document_sequences",
                column: "Prefix",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_payment_entries_PaymentDate",
                table: "payment_entries",
                column: "PaymentDate");

            migrationBuilder.CreateIndex(
                name: "IX_payment_entries_PaymentNumber",
                table: "payment_entries",
                column: "PaymentNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_payment_entries_PurchaseBillId",
                table: "payment_entries",
                column: "PurchaseBillId");

            migrationBuilder.CreateIndex(
                name: "IX_payment_entries_Status",
                table: "payment_entries",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_payment_entries_VendorId",
                table: "payment_entries",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_receipt_document_sequences_Prefix",
                table: "receipt_document_sequences",
                column: "Prefix",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_receipt_entries_CustomerId",
                table: "receipt_entries",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_receipt_entries_InvoiceId",
                table: "receipt_entries",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_receipt_entries_ReceiptDate",
                table: "receipt_entries",
                column: "ReceiptDate");

            migrationBuilder.CreateIndex(
                name: "IX_receipt_entries_ReceiptNumber",
                table: "receipt_entries",
                column: "ReceiptNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_receipt_entries_Status",
                table: "receipt_entries",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "accounting_attachments");

            migrationBuilder.DropTable(
                name: "accounting_notes");

            migrationBuilder.DropTable(
                name: "accounting_timeline_events");

            migrationBuilder.DropTable(
                name: "bank_recon_document_sequences");

            migrationBuilder.DropTable(
                name: "customer_ledger_document_sequences");

            migrationBuilder.DropTable(
                name: "gst_document_sequences");

            migrationBuilder.DropTable(
                name: "outstanding_records");

            migrationBuilder.DropTable(
                name: "payment_document_sequences");

            migrationBuilder.DropTable(
                name: "receipt_document_sequences");

            migrationBuilder.DropTable(
                name: "bank_reconciliations");

            migrationBuilder.DropTable(
                name: "customer_ledger_entries");

            migrationBuilder.DropTable(
                name: "gst_returns");

            migrationBuilder.DropTable(
                name: "gst_transactions");

            migrationBuilder.DropTable(
                name: "payment_entries");

            migrationBuilder.DropTable(
                name: "receipt_entries");
        }
    }
}
