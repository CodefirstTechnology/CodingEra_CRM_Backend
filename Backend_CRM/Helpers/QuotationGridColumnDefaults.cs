namespace CRM.Helpers
{
    /// <summary>Default ERP-style quotation item grid columns (JSON array stored in DB).</summary>
    public static class QuotationGridColumnDefaults
    {
        public const string DefaultColumnsJson = """
            [
              {"key":"srNo","label":"Sr No","visible":true,"order":0,"width":56,"editable":false},
              {"key":"itemName","label":"Item Name","visible":true,"order":1,"width":160,"editable":true},
              {"key":"description","label":"Description","visible":true,"order":2,"width":200,"editable":true},
              {"key":"quantity","label":"Quantity","visible":true,"order":3,"width":96,"editable":true},
              {"key":"unit","label":"Unit","visible":true,"order":4,"width":72,"editable":true},
              {"key":"weight","label":"Weight","visible":true,"order":5,"width":88,"editable":true},
              {"key":"unitWeight","label":"Unit Weight","visible":true,"order":6,"width":96,"editable":true},
              {"key":"unitRate","label":"Unit Rate","visible":true,"order":7,"width":104,"editable":true},
              {"key":"discountPercent","label":"Discount %","visible":true,"order":8,"width":96,"editable":true},
              {"key":"gstPercent","label":"GST %","visible":true,"order":9,"width":80,"editable":true},
              {"key":"amount","label":"Amount","visible":true,"order":10,"width":112,"editable":false}
            ]
            """;
    }
}
