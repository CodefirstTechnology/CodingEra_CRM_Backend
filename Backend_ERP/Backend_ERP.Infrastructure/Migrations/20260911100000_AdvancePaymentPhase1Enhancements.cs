using ERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend_ERP.Infrastructure.Migrations
{
    [DbContext(typeof(ERPDbContext))]
    [Migration("20260911100000_AdvancePaymentPhase1Enhancements")]
    public partial class AdvancePaymentPhase1Enhancements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
DO $$
BEGIN
    -- 1. SalesOrder AdvanceAllocatedAmount column
    ALTER TABLE public.sales_orders 
    ADD COLUMN IF NOT EXISTS "AdvanceAllocatedAmount" decimal(18,2) NOT NULL DEFAULT 0;

    -- 2. AdvancePayment refund & forfeiture columns
    ALTER TABLE public.advance_payments 
    ADD COLUMN IF NOT EXISTS "RefundedAmount" decimal(18,2) NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS "ForfeitedAmount" decimal(18,2) NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS "RefundReferenceNumber" character varying(64) NULL,
    ADD COLUMN IF NOT EXISTS "RefundProcessedBy" character varying(100) NULL,
    ADD COLUMN IF NOT EXISTS "RefundProcessedOn" timestamp with time zone NULL;

    -- 3. AdvancePaymentApplication reversal columns
    ALTER TABLE public.advance_payment_applications 
    ADD COLUMN IF NOT EXISTS "IsReversal" boolean NOT NULL DEFAULT false,
    ADD COLUMN IF NOT EXISTS "ReversalReason" text NULL,
    ADD COLUMN IF NOT EXISTS "OriginalApplicationId" integer NULL REFERENCES public.advance_payment_applications("Id") ON DELETE RESTRICT;

    -- 4. Replace unique index on (AdvancePaymentId, SalesOrderId) with non-unique index to support reversals
    DROP INDEX IF EXISTS "IX_advance_payment_applications_AdvancePaymentId_SalesOrderId";
    CREATE INDEX IF NOT EXISTS "IX_advance_payment_applications_AdvancePaymentId_SalesOrderId" 
    ON public.advance_payment_applications ("AdvancePaymentId", "SalesOrderId");

    CREATE INDEX IF NOT EXISTS "IX_advance_payment_applications_OriginalApplicationId" 
    ON public.advance_payment_applications ("OriginalApplicationId");

    -- 5. Mathematical invariant check constraint
    ALTER TABLE public.advance_payments DROP CONSTRAINT IF EXISTS chk_adv_amounts;
    ALTER TABLE public.advance_payments 
    ADD CONSTRAINT chk_adv_amounts 
    CHECK (
        "AdvanceAmount" >= 0 AND 
        "AppliedAmount" >= 0 AND 
        "RefundedAmount" >= 0 AND 
        "ForfeitedAmount" >= 0 AND 
        ("AppliedAmount" + "RefundedAmount" + "ForfeitedAmount") <= "AdvanceAmount" AND 
        "RemainingAmount" = ("AdvanceAmount" - "AppliedAmount" - "RefundedAmount" - "ForfeitedAmount")
    );
END $$;
""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
DO $$
BEGIN
    ALTER TABLE public.advance_payments DROP CONSTRAINT IF EXISTS chk_adv_amounts;

    DROP INDEX IF EXISTS "IX_advance_payment_applications_OriginalApplicationId";
    DROP INDEX IF EXISTS "IX_advance_payment_applications_AdvancePaymentId_SalesOrderId";
    CREATE UNIQUE INDEX IF NOT EXISTS "IX_advance_payment_applications_AdvancePaymentId_SalesOrderId" 
    ON public.advance_payment_applications ("AdvancePaymentId", "SalesOrderId");

    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'advance_payment_applications' AND column_name = 'OriginalApplicationId') THEN
        ALTER TABLE public.advance_payment_applications DROP COLUMN "OriginalApplicationId";
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'advance_payment_applications' AND column_name = 'ReversalReason') THEN
        ALTER TABLE public.advance_payment_applications DROP COLUMN "ReversalReason";
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'advance_payment_applications' AND column_name = 'IsReversal') THEN
        ALTER TABLE public.advance_payment_applications DROP COLUMN "IsReversal";
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'advance_payments' AND column_name = 'RefundProcessedOn') THEN
        ALTER TABLE public.advance_payments DROP COLUMN "RefundProcessedOn";
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'advance_payments' AND column_name = 'RefundProcessedBy') THEN
        ALTER TABLE public.advance_payments DROP COLUMN "RefundProcessedBy";
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'advance_payments' AND column_name = 'RefundReferenceNumber') THEN
        ALTER TABLE public.advance_payments DROP COLUMN "RefundReferenceNumber";
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'advance_payments' AND column_name = 'ForfeitedAmount') THEN
        ALTER TABLE public.advance_payments DROP COLUMN "ForfeitedAmount";
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'advance_payments' AND column_name = 'RefundedAmount') THEN
        ALTER TABLE public.advance_payments DROP COLUMN "RefundedAmount";
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'sales_orders' AND column_name = 'AdvanceAllocatedAmount') THEN
        ALTER TABLE public.sales_orders DROP COLUMN "AdvanceAllocatedAmount";
    END IF;
END $$;
""");
        }
    }
}
