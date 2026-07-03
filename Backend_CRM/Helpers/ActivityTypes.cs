namespace CRM.Helpers
{
    public static class ActivityEntityTypes
    {
        public const string Lead = "lead";
        public const string Deal = "deal";
        public const string Contact = "contact";
        public const string Organization = "organization";
        public const string Item = "item";
        public const string ItemGroup = "item_group";
        public const string ItemAttribute = "item_attribute";
    }

    public static class ActivityActionTypes
    {
        public const string Created = "created";
        public const string Updated = "updated";
        public const string StatusChanged = "status_changed";
        public const string FieldUpdated = "field_updated";
        public const string NoteAdded = "note_added";
        public const string CommentAdded = "comment_added";
        public const string TaskAdded = "task_added";
        public const string CallLogged = "call_logged";
        public const string EmailSent = "email_sent";
        public const string AttachmentAdded = "attachment_added";
        public const string Deleted = "deleted";
    }
}
