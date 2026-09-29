using System;
using System.Collections.Generic;

namespace Backend_ERP.Application.DTOs.Inventory
{
    public enum TransactionType
    {
        GRN_IN,
        PROD_OUT,
        BRANCH_IN,
        CUST_RET_IN,
        ADJ_POS_IN,
        PROD_ISSUE_OUT,
        SALES_DISP_OUT,
        BRANCH_OUT,
        RTV_OUT,
        SCRAP_OUT,
        ADJ_NEG_OUT
    }

    public record StockMovementLineDto(
        Guid ItemId,
        Guid WarehouseId,
        Guid BinId,
        string? BatchNo,
        decimal Quantity,
        decimal? SecondaryQuantity, // Additive: Catch-weight secondary quantity
        decimal UnitCost
    );

    public record BatchStockMovementRequestDto(
        Guid IdempotencyKey, // Additive: Unique network retry protection key
        TransactionType TransactionType,
        string ReferenceType,
        Guid ReferenceId,
        string ReferenceDocumentNo,
        Guid UserId,
        List<StockMovementLineDto> Lines
    );

    public record StockTransactionResponseDto(
        List<string> TransactionNumbers,
        string Message
    );
}
