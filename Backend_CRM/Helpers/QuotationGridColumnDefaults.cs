namespace CRM.Helpers
{
    /// <summary>Default quotation item grid columns (JSON array stored in DB). Dynamic attribute/spec columns are merged at runtime.</summary>
    public static class QuotationGridColumnDefaults
    {
        public const string DefaultColumnsJson = """
            [
              {"key":"srNo","label":"Sr No","visible":true,"order":0,"width":56,"editable":false},
              {"key":"itemName","label":"Item Name","visible":true,"order":1,"width":180,"editable":true},
              {"key":"description","label":"Description","visible":true,"order":2,"width":200,"editable":true},
              {"key":"quantity","label":"Quantity","visible":true,"order":3,"width":96,"editable":true},
              {"key":"unitRate","label":"Rate","visible":true,"order":4,"width":104,"editable":false},
              {"key":"amount","label":"Total","visible":true,"order":5,"width":112,"editable":false},
              {"key":"unit","label":"Unit","visible":false,"order":90,"width":72,"editable":true},
              {"key":"weight","label":"Weight","visible":false,"order":91,"width":88,"editable":false},
              {"key":"unitWeight","label":"Unit Weight","visible":false,"order":92,"width":96,"editable":false},
              {"key":"discountPercent","label":"Discount %","visible":false,"order":93,"width":96,"editable":true},
              {"key":"gstPercent","label":"GST %","visible":false,"order":94,"width":80,"editable":false}
            ]
            """;
    }
}
