using CRM.DATA;
using CRM.models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace CRM.Helpers
{
    /// <summary>Builds <see cref="ActivityLog"/> rows from EF change tracking during SaveChanges.</summary>
    internal static class ActivityCapture
    {
        internal sealed class PendingActivity
        {
            public ActivityLog Activity { get; init; } = null!;
            public EntityEntry? SourceEntry { get; init; }
        }

        internal sealed class CaptureBatch
        {
            internal List<PendingActivity> Items { get; } = new();
        }

        public static CaptureBatch Capture(TaskDbcontext db)
        {
            var batch = new CaptureBatch();
            var actorId = db.AuditUserId;
            var lookup = new ActivityLookup(db);

            foreach (var entry in db.ChangeTracker.Entries<Lead>())
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        AddRange(batch, BuildLeadCreated(entry.Entity, actorId, lookup), entry);
                        break;
                    case EntityState.Modified:
                        AddRange(batch, BuildLeadUpdates(entry, actorId, lookup));
                        break;
                }
            }

            foreach (var entry in db.ChangeTracker.Entries<Deal>())
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        AddRange(batch, BuildDealCreated(entry.Entity, actorId, lookup), entry);
                        break;
                    case EntityState.Modified:
                        AddRange(batch, BuildDealUpdates(entry, actorId, lookup, db));
                        break;
                }
            }

            foreach (var entry in db.ChangeTracker.Entries<Contact>())
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        AddRange(batch, BuildContactCreated(entry.Entity, actorId, lookup), entry);
                        break;
                    case EntityState.Modified:
                        AddRange(batch, BuildContactUpdates(entry, actorId, lookup));
                        break;
                }
            }

            foreach (var entry in db.ChangeTracker.Entries<Organization>())
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        AddRange(batch, BuildOrganizationCreated(entry.Entity, actorId, lookup), entry);
                        break;
                    case EntityState.Modified:
                        AddRange(batch, BuildOrganizationUpdates(entry, actorId, lookup));
                        break;
                }
            }

            foreach (var entry in db.ChangeTracker.Entries<Note>())
            {
                if (entry.State == EntityState.Added)
                {
                    AddRange(batch, BuildNoteAdded(entry.Entity, actorId, lookup), entry);
                }
            }

            foreach (var entry in db.ChangeTracker.Entries<Comment>())
            {
                if (entry.State == EntityState.Added)
                {
                    AddRange(batch, BuildCommentAdded(entry.Entity, actorId, lookup), entry);
                }
            }

            foreach (var entry in db.ChangeTracker.Entries<TaskTable>())
            {
                if (entry.State == EntityState.Added)
                {
                    AddRange(batch, BuildTaskAdded(entry.Entity, actorId, lookup), entry);
                }
            }

            foreach (var entry in db.ChangeTracker.Entries<CallLog>())
            {
                if (entry.State == EntityState.Added)
                {
                    AddRange(batch, BuildCallLogged(entry.Entity, actorId, lookup), entry);
                }
            }

            foreach (var entry in db.ChangeTracker.Entries<Item>())
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        AddRange(batch, BuildItemCreated(entry.Entity, actorId, lookup), entry);
                        break;
                    case EntityState.Modified:
                        AddRange(batch, BuildItemUpdates(entry, actorId, lookup));
                        break;
                    case EntityState.Deleted:
                        AddRange(batch, BuildItemDeleted(entry.Entity, actorId, lookup));
                        break;
                }
            }

            foreach (var entry in db.ChangeTracker.Entries<ItemGroup>())
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        AddRange(batch, BuildItemGroupCreated(entry.Entity, actorId, lookup), entry);
                        break;
                    case EntityState.Modified:
                        AddRange(batch, BuildItemGroupUpdates(entry, actorId, lookup));
                        break;
                    case EntityState.Deleted:
                        AddRange(batch, BuildItemGroupDeleted(entry.Entity, actorId, lookup));
                        break;
                }
            }

            foreach (var entry in db.ChangeTracker.Entries<ItemAttribute>())
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        AddRange(batch, BuildItemAttributeCreated(entry.Entity, actorId, lookup), entry);
                        break;
                    case EntityState.Modified:
                        AddRange(batch, BuildItemAttributeUpdates(entry, actorId, lookup));
                        break;
                    case EntityState.Deleted:
                        AddRange(batch, BuildItemAttributeDeleted(entry.Entity, actorId, lookup));
                        break;
                }
            }

            return batch;
        }

        public static void Flush(TaskDbcontext db, CaptureBatch batch)
        {
            if (batch.Items.Count == 0)
            {
                return;
            }

            var utc = DateTime.UtcNow;
            foreach (var pending in batch.Items)
            {
                var activity = pending.Activity;

                if (activity.EntityId <= 0 && pending.SourceEntry != null)
                {
                    activity.EntityId = ReadEntityId(pending.SourceEntry);
                }

                if (activity.EntityId <= 0)
                {
                    continue;
                }

                if (activity.RelatedRecordId is <= 0 && pending.SourceEntry != null
                    && !string.IsNullOrEmpty(activity.RelatedRecordType))
                {
                    activity.RelatedRecordId = ReadEntityId(pending.SourceEntry);
                }

                activity.CreatedAt = utc;
                db.ActivityLogs.Add(activity);
            }
        }

        private static void AddRange(CaptureBatch batch, IEnumerable<ActivityLog> activities, EntityEntry? source = null)
        {
            foreach (var activity in activities)
            {
                batch.Items.Add(new PendingActivity { Activity = activity, SourceEntry = source });
            }
        }

        private static int ReadEntityId(EntityEntry entry) => entry.Entity switch
        {
            Lead l => l.Id,
            Deal d => d.Id,
            Contact c => c.Id,
            Organization o => o.Id,
            Note n => n.Id,
            Comment c => c.Id,
            TaskTable t => t.TaskId,
            CallLog cl => cl.CallId,
            Item i => i.Id,
            ItemGroup g => g.Id,
            ItemAttribute a => a.Id,
            _ => 0,
        };

        private static IEnumerable<ActivityLog> BuildLeadCreated(
            Lead lead, int? actorId, ActivityLookup lookup)
        {
            var actor = lookup.ActorName(actorId);
            yield return new ActivityLog
            {
                EntityType = ActivityEntityTypes.Lead,
                EntityId = 0,
                ActionType = ActivityActionTypes.Created,
                ActorUserId = actorId,
                ActorName = actor,
                Message = $"{actor} created this lead",
            };
        }

        private static IEnumerable<ActivityLog> BuildLeadUpdates(
            EntityEntry<Lead> entry, int? actorId, ActivityLookup lookup)
        {
            var lead = entry.Entity;
            var actor = lookup.ActorName(actorId);
            var display = lookup.LeadDisplayName(lead);

            foreach (var prop in entry.Properties.Where(p => p.IsModified))
            {
                var name = prop.Metadata.Name;
                if (name is nameof(Lead.UpdatedAt) or nameof(Lead.CreatedBy) or nameof(Lead.UpdatedBy))
                {
                    continue;
                }

                var oldRaw = prop.OriginalValue;
                var newRaw = prop.CurrentValue;
                if (Equals(oldRaw, newRaw))
                {
                    continue;
                }

                if (name == nameof(Lead.LeadStatusId))
                {
                    var oldName = lookup.LeadStatusName(oldRaw as int?);
                    var newName = lookup.LeadStatusName(newRaw as int?);
                    yield return new ActivityLog
                    {
                        EntityType = ActivityEntityTypes.Lead,
                        EntityId = lead.Id,
                        ActionType = ActivityActionTypes.StatusChanged,
                        ActorUserId = actorId,
                        ActorName = actor,
                        FieldName = "status",
                        OldValue = oldName,
                        NewValue = newName,
                        Message = $"{display} status moved to {newName ?? "—"}",
                    };
                    continue;
                }

                if (name == nameof(Lead.OrganizationId))
                {
                    var newOrg = lookup.OrganizationName(newRaw as int?);
                    yield return new ActivityLog
                    {
                        EntityType = ActivityEntityTypes.Lead,
                        EntityId = lead.Id,
                        ActionType = ActivityActionTypes.FieldUpdated,
                        ActorUserId = actorId,
                        ActorName = actor,
                        FieldName = "organization",
                        OldValue = lookup.OrganizationName(oldRaw as int?),
                        NewValue = newOrg,
                        Message = string.IsNullOrWhiteSpace(newOrg)
                            ? $"{actor} removed organization"
                            : $"Organization updated to {newOrg}",
                    };
                    continue;
                }

                var label = LeadFieldLabel(name);
                var oldText = lookup.FormatLeadValue(name, oldRaw);
                var newText = lookup.FormatLeadValue(name, newRaw);
                yield return new ActivityLog
                {
                    EntityType = ActivityEntityTypes.Lead,
                    EntityId = lead.Id,
                    ActionType = ActivityActionTypes.FieldUpdated,
                    ActorUserId = actorId,
                    ActorName = actor,
                    FieldName = label,
                    OldValue = oldText,
                    NewValue = newText,
                    Message = $"{actor} updated {label} to {newText ?? "—"}",
                };
            }
        }

        private static IEnumerable<ActivityLog> BuildDealCreated(
            Deal deal, int? actorId, ActivityLookup lookup)
        {
            var actor = lookup.ActorName(actorId);
            yield return new ActivityLog
            {
                EntityType = ActivityEntityTypes.Deal,
                EntityId = 0,
                ActionType = ActivityActionTypes.Created,
                ActorUserId = actorId,
                ActorName = actor,
                Message = $"{actor} created this deal",
            };
        }

        private static IEnumerable<ActivityLog> BuildDealUpdates(
            EntityEntry<Deal> entry, int? actorId, ActivityLookup lookup, TaskDbcontext db)
        {
            var deal = entry.Entity;
            var actor = lookup.ActorName(actorId);
            var display = DealDisplayName(deal);

            foreach (var prop in entry.Properties.Where(p => p.IsModified))
            {
                var name = prop.Metadata.Name;
                if (name is nameof(Deal.UpdatedAt) or nameof(Deal.LastModified)
                    or nameof(Deal.CreatedBy) or nameof(Deal.UpdatedBy) or nameof(Deal.CreatedAt)
                    or nameof(Deal.Status))
                {
                    continue;
                }

                var oldRaw = prop.OriginalValue;
                var newRaw = prop.CurrentValue;
                if (Equals(oldRaw, newRaw))
                {
                    continue;
                }

                if (name == nameof(Deal.DealStatusId))
                {
                    var oldName = lookup.DealStatusName(oldRaw as int?);
                    var newName = lookup.DealStatusName(newRaw as int?);
                    var message = FormatDealStageActivityMessage(newName);
                    var comment = db.DealStageChangeComment?.Trim();
                    if (!string.IsNullOrEmpty(comment))
                    {
                        message = $"{message} — {comment}";
                    }

                    yield return new ActivityLog
                    {
                        EntityType = ActivityEntityTypes.Deal,
                        EntityId = deal.Id,
                        ActionType = ActivityActionTypes.StatusChanged,
                        ActorUserId = actorId,
                        ActorName = actor,
                        FieldName = "status",
                        OldValue = oldName,
                        NewValue = newName,
                        Message = message,
                    };
                    continue;
                }

                var label = name switch
                {
                    nameof(Deal.FirstName) => "first name",
                    nameof(Deal.LastName) => "last name",
                    nameof(Deal.OrganizationName) => "organization",
                    _ => name,
                };
                yield return new ActivityLog
                {
                    EntityType = ActivityEntityTypes.Deal,
                    EntityId = deal.Id,
                    ActionType = ActivityActionTypes.FieldUpdated,
                    ActorUserId = actorId,
                    ActorName = actor,
                    FieldName = label,
                    OldValue = oldRaw?.ToString(),
                    NewValue = newRaw?.ToString(),
                    Message = $"{actor} updated {label} on {display}",
                };
            }
        }

        private static IEnumerable<ActivityLog> BuildContactCreated(
            Contact c, int? actorId, ActivityLookup lookup)
        {
            var actor = lookup.ActorName(actorId);
            yield return new ActivityLog
            {
                EntityType = ActivityEntityTypes.Contact,
                EntityId = 0,
                ActionType = ActivityActionTypes.Created,
                ActorUserId = actorId,
                ActorName = actor,
                Message = $"{actor} created this contact",
            };
        }

        private static IEnumerable<ActivityLog> BuildContactUpdates(
            EntityEntry<Contact> entry, int? actorId, ActivityLookup lookup)
        {
            var contact = entry.Entity;
            var actor = lookup.ActorName(actorId);
            var display = $"{contact.FirstName} {contact.LastName}".Trim();

            foreach (var prop in entry.Properties.Where(p => p.IsModified))
            {
                var name = prop.Metadata.Name;
                if (name is nameof(Contact.UpdatedAt) or nameof(Contact.LastModified)
                    or nameof(Contact.CreatedBy) or nameof(Contact.UpdatedBy) or nameof(Contact.CreatedAt))
                {
                    continue;
                }

                var oldRaw = prop.OriginalValue;
                var newRaw = prop.CurrentValue;
                if (Equals(oldRaw, newRaw))
                {
                    continue;
                }

                var label = name switch
                {
                    nameof(Contact.FirstName) => "first name",
                    nameof(Contact.LastName) => "last name",
                    nameof(Contact.OrganizationId) => "organization",
                    _ => name,
                };
                var newText = name == nameof(Contact.OrganizationId)
                    ? lookup.OrganizationName(newRaw as int?)
                    : newRaw?.ToString();
                yield return new ActivityLog
                {
                    EntityType = ActivityEntityTypes.Contact,
                    EntityId = contact.Id,
                    ActionType = ActivityActionTypes.FieldUpdated,
                    ActorUserId = actorId,
                    ActorName = actor,
                    FieldName = label,
                    NewValue = newText,
                    Message = $"{actor} updated {label} on {display}",
                };
            }
        }

        private static IEnumerable<ActivityLog> BuildOrganizationCreated(
            Organization org, int? actorId, ActivityLookup lookup)
        {
            var actor = lookup.ActorName(actorId);
            yield return new ActivityLog
            {
                EntityType = ActivityEntityTypes.Organization,
                EntityId = 0,
                ActionType = ActivityActionTypes.Created,
                ActorUserId = actorId,
                ActorName = actor,
                Message = $"{actor} created organization {org.Name}",
            };
        }

        private static IEnumerable<ActivityLog> BuildOrganizationUpdates(
            EntityEntry<Organization> entry, int? actorId, ActivityLookup lookup)
        {
            var org = entry.Entity;
            var actor = lookup.ActorName(actorId);

            foreach (var prop in entry.Properties.Where(p => p.IsModified))
            {
                var name = prop.Metadata.Name;
                if (name is nameof(Organization.UpdatedAt) or nameof(Organization.LastModified)
                    or nameof(Organization.CreatedBy) or nameof(Organization.UpdatedBy) or nameof(Organization.CreatedAt))
                {
                    continue;
                }

                var newRaw = prop.CurrentValue;
                var label = name switch
                {
                    nameof(Organization.Name) => "name",
                    nameof(Organization.IndustryId) => "industry",
                    nameof(Organization.TerritoryId) => "territory",
                    nameof(Organization.EmployeeCountId) => "employee count",
                    _ => name,
                };
                var newText = name switch
                {
                    nameof(Organization.IndustryId) => lookup.IndustryName(newRaw as int?),
                    nameof(Organization.TerritoryId) => lookup.TerritoryName(newRaw as int?),
                    nameof(Organization.EmployeeCountId) => lookup.EmployeeCountName(newRaw as int?),
                    _ => newRaw?.ToString(),
                };
                yield return new ActivityLog
                {
                    EntityType = ActivityEntityTypes.Organization,
                    EntityId = org.Id,
                    ActionType = ActivityActionTypes.FieldUpdated,
                    ActorUserId = actorId,
                    ActorName = actor,
                    FieldName = label,
                    NewValue = newText,
                    Message = $"{actor} updated {label} on {org.Name}",
                };
            }
        }

        private static IEnumerable<ActivityLog> BuildNoteAdded(
            Note note, int? actorId, ActivityLookup lookup)
        {
            var seen = new HashSet<(string, int)>();
            foreach (var target in ResolveNoteTargets(note))
            {
                if (!seen.Add(target))
                {
                    continue;
                }

                var isComment = string.Equals(note.RelatedType, "comment", StringComparison.OrdinalIgnoreCase);
                var actor = lookup.ActorName(actorId);
                var preview = Truncate(note.NoteText, 80);
                yield return new ActivityLog
                {
                    EntityType = target.EntityType,
                    EntityId = target.EntityId,
                    ActionType = isComment ? ActivityActionTypes.CommentAdded : ActivityActionTypes.NoteAdded,
                    ActorUserId = actorId,
                    ActorName = actor,
                    RelatedRecordType = isComment ? "comment" : "note",
                    RelatedRecordId = 0,
                    Message = isComment
                        ? $"{actor} added a comment: {preview}"
                        : $"{actor} added a note: {preview}",
                };
            }
        }

        private static IEnumerable<ActivityLog> BuildCommentAdded(
            Comment comment, int? actorId, ActivityLookup lookup)
        {
            var actor = lookup.ActorName(actorId);
            var preview = Truncate(comment.Body, 80);
            yield return new ActivityLog
            {
                EntityType = comment.EntityType.ToLowerInvariant(),
                EntityId = comment.EntityId,
                ActionType = ActivityActionTypes.CommentAdded,
                ActorUserId = actorId,
                ActorName = actor,
                RelatedRecordType = "comment",
                RelatedRecordId = 0,
                Message = $"{actor} added a comment: {preview}",
            };
        }

        private static IEnumerable<ActivityLog> BuildTaskAdded(
            TaskTable task, int? actorId, ActivityLookup lookup)
        {
            foreach (var target in ResolveTaskTargets(task))
            {
                var actor = lookup.ActorName(actorId);
                yield return new ActivityLog
                {
                    EntityType = target.EntityType,
                    EntityId = target.EntityId,
                    ActionType = ActivityActionTypes.TaskAdded,
                    ActorUserId = actorId,
                    ActorName = actor,
                    RelatedRecordType = "task",
                    RelatedRecordId = 0,
                    Message = $"{actor} added task: {task.TaskTitle}",
                };
            }
        }

        private static IEnumerable<ActivityLog> BuildCallLogged(
            CallLog call, int? actorId, ActivityLookup lookup)
        {
            foreach (var target in ResolveCallTargets(call))
            {
                var actor = lookup.ActorName(actorId);
                yield return new ActivityLog
                {
                    EntityType = target.EntityType,
                    EntityId = target.EntityId,
                    ActionType = ActivityActionTypes.CallLogged,
                    ActorUserId = actorId,
                    ActorName = actor,
                    RelatedRecordType = "call",
                    RelatedRecordId = 0,
                    Message = $"{actor} logged a call with {call.ContactName}",
                };
            }
        }

        private static IEnumerable<(string EntityType, int EntityId)> ResolveNoteTargets(Note note)
        {
            if (note.RelatedLeadId is > 0)
            {
                yield return (ActivityEntityTypes.Lead, note.RelatedLeadId.Value);
            }

            if (note.RelatedDealId is > 0)
            {
                yield return (ActivityEntityTypes.Deal, note.RelatedDealId.Value);
            }

            if (note.RelatedContactId is > 0)
            {
                yield return (ActivityEntityTypes.Contact, note.RelatedContactId.Value);
            }

            if (note.RelatedOrganizationId is > 0)
            {
                yield return (ActivityEntityTypes.Organization, note.RelatedOrganizationId.Value);
            }

            if (note.RecordId > 0 && string.Equals(note.RelatedType, ActivityEntityTypes.Lead, StringComparison.OrdinalIgnoreCase))
            {
                yield return (ActivityEntityTypes.Lead, note.RecordId);
            }
            else if (note.RecordId > 0 && string.Equals(note.RelatedType, ActivityEntityTypes.Deal, StringComparison.OrdinalIgnoreCase))
            {
                yield return (ActivityEntityTypes.Deal, note.RecordId);
            }
        }

        private static IEnumerable<(string EntityType, int EntityId)> ResolveTaskTargets(TaskTable task)
        {
            if (task.RelatedLeadId is > 0)
            {
                yield return (ActivityEntityTypes.Lead, task.RelatedLeadId.Value);
            }

            if (task.RelatedDealId is > 0)
            {
                yield return (ActivityEntityTypes.Deal, task.RelatedDealId.Value);
            }
        }

        private static IEnumerable<(string EntityType, int EntityId)> ResolveCallTargets(CallLog call)
        {
            if (call.RelatedLeadId is > 0)
            {
                yield return (ActivityEntityTypes.Lead, call.RelatedLeadId.Value);
            }

            if (call.RelatedDealId is > 0)
            {
                yield return (ActivityEntityTypes.Deal, call.RelatedDealId.Value);
            }
        }

        private static IEnumerable<ActivityLog> BuildItemCreated(Item item, int? actorId, ActivityLookup lookup)
        {
            var actor = lookup.ActorName(actorId);
            var label = ItemDisplayLabel(item);
            var kind = item.ParentItemId.HasValue ? "variant" : "item";
            yield return new ActivityLog
            {
                EntityType = ActivityEntityTypes.Item,
                EntityId = 0,
                ActionType = ActivityActionTypes.Created,
                ActorUserId = actorId,
                ActorName = actor,
                Message = $"{actor} created {kind} {label}",
            };
        }

        private static IEnumerable<ActivityLog> BuildItemUpdates(
            EntityEntry<Item> entry, int? actorId, ActivityLookup lookup)
        {
            var item = entry.Entity;
            var actor = lookup.ActorName(actorId);
            var label = ItemDisplayLabel(item);

            foreach (var prop in entry.Properties.Where(p => p.IsModified))
            {
                var name = prop.Metadata.Name;
                if (name is nameof(Item.UpdatedAt) or nameof(Item.CreatedBy) or nameof(Item.UpdatedBy)
                    or nameof(Item.CreatedAt))
                {
                    continue;
                }

                var oldRaw = prop.OriginalValue;
                var newRaw = prop.CurrentValue;
                if (Equals(oldRaw, newRaw))
                {
                    continue;
                }

                if (name == nameof(Item.Status))
                {
                    yield return new ActivityLog
                    {
                        EntityType = ActivityEntityTypes.Item,
                        EntityId = item.Id,
                        ActionType = ActivityActionTypes.StatusChanged,
                        ActorUserId = actorId,
                        ActorName = actor,
                        FieldName = "status",
                        OldValue = oldRaw?.ToString(),
                        NewValue = newRaw?.ToString(),
                        Message = $"{actor} changed status of {label} to {newRaw}",
                    };
                    continue;
                }

                if (name == nameof(Item.ItemGroupId))
                {
                    yield return new ActivityLog
                    {
                        EntityType = ActivityEntityTypes.Item,
                        EntityId = item.Id,
                        ActionType = ActivityActionTypes.FieldUpdated,
                        ActorUserId = actorId,
                        ActorName = actor,
                        FieldName = "item group",
                        OldValue = lookup.ItemGroupName(oldRaw as int?),
                        NewValue = lookup.ItemGroupName(newRaw as int?),
                        Message = $"{actor} updated item group on {label}",
                    };
                    continue;
                }

                var fieldLabel = ItemFieldLabel(name);
                yield return new ActivityLog
                {
                    EntityType = ActivityEntityTypes.Item,
                    EntityId = item.Id,
                    ActionType = ActivityActionTypes.FieldUpdated,
                    ActorUserId = actorId,
                    ActorName = actor,
                    FieldName = fieldLabel,
                    OldValue = oldRaw?.ToString(),
                    NewValue = newRaw?.ToString(),
                    Message = $"{actor} updated {fieldLabel} on {label}",
                };
            }
        }

        private static IEnumerable<ActivityLog> BuildItemDeleted(Item item, int? actorId, ActivityLookup lookup)
        {
            var actor = lookup.ActorName(actorId);
            var label = ItemDisplayLabel(item);
            var kind = item.ParentItemId.HasValue ? "variant" : "item";
            yield return new ActivityLog
            {
                EntityType = ActivityEntityTypes.Item,
                EntityId = item.Id,
                ActionType = ActivityActionTypes.Deleted,
                ActorUserId = actorId,
                ActorName = actor,
                Message = $"{actor} deleted {kind} {label}",
            };
        }

        private static IEnumerable<ActivityLog> BuildItemGroupCreated(
            ItemGroup group, int? actorId, ActivityLookup lookup)
        {
            var actor = lookup.ActorName(actorId);
            yield return new ActivityLog
            {
                EntityType = ActivityEntityTypes.ItemGroup,
                EntityId = 0,
                ActionType = ActivityActionTypes.Created,
                ActorUserId = actorId,
                ActorName = actor,
                Message = $"{actor} created item group {group.Name}",
            };
        }

        private static IEnumerable<ActivityLog> BuildItemGroupUpdates(
            EntityEntry<ItemGroup> entry, int? actorId, ActivityLookup lookup)
        {
            var group = entry.Entity;
            var actor = lookup.ActorName(actorId);

            foreach (var prop in entry.Properties.Where(p => p.IsModified))
            {
                var name = prop.Metadata.Name;
                if (name is nameof(ItemGroup.UpdatedAt) or nameof(ItemGroup.CreatedBy) or nameof(ItemGroup.UpdatedBy)
                    or nameof(ItemGroup.CreatedAt))
                {
                    continue;
                }

                var oldRaw = prop.OriginalValue;
                var newRaw = prop.CurrentValue;
                if (Equals(oldRaw, newRaw))
                {
                    continue;
                }

                var fieldLabel = ItemGroupFieldLabel(name);
                yield return new ActivityLog
                {
                    EntityType = ActivityEntityTypes.ItemGroup,
                    EntityId = group.Id,
                    ActionType = ActivityActionTypes.FieldUpdated,
                    ActorUserId = actorId,
                    ActorName = actor,
                    FieldName = fieldLabel,
                    OldValue = oldRaw?.ToString(),
                    NewValue = newRaw?.ToString(),
                    Message = $"{actor} updated {fieldLabel} on item group {group.Name}",
                };
            }
        }

        private static IEnumerable<ActivityLog> BuildItemGroupDeleted(
            ItemGroup group, int? actorId, ActivityLookup lookup)
        {
            var actor = lookup.ActorName(actorId);
            yield return new ActivityLog
            {
                EntityType = ActivityEntityTypes.ItemGroup,
                EntityId = group.Id,
                ActionType = ActivityActionTypes.Deleted,
                ActorUserId = actorId,
                ActorName = actor,
                Message = $"{actor} deleted item group {group.Name}",
            };
        }

        private static IEnumerable<ActivityLog> BuildItemAttributeCreated(
            ItemAttribute attribute, int? actorId, ActivityLookup lookup)
        {
            var actor = lookup.ActorName(actorId);
            yield return new ActivityLog
            {
                EntityType = ActivityEntityTypes.ItemAttribute,
                EntityId = 0,
                ActionType = ActivityActionTypes.Created,
                ActorUserId = actorId,
                ActorName = actor,
                Message = $"{actor} created attribute {attribute.Name}",
            };
        }

        private static IEnumerable<ActivityLog> BuildItemAttributeUpdates(
            EntityEntry<ItemAttribute> entry, int? actorId, ActivityLookup lookup)
        {
            var attribute = entry.Entity;
            var actor = lookup.ActorName(actorId);

            foreach (var prop in entry.Properties.Where(p => p.IsModified))
            {
                var name = prop.Metadata.Name;
                if (name is nameof(ItemAttribute.UpdatedAt) or nameof(ItemAttribute.CreatedBy)
                    or nameof(ItemAttribute.UpdatedBy) or nameof(ItemAttribute.CreatedAt))
                {
                    continue;
                }

                var oldRaw = prop.OriginalValue;
                var newRaw = prop.CurrentValue;
                if (Equals(oldRaw, newRaw))
                {
                    continue;
                }

                var fieldLabel = ItemAttributeFieldLabel(name);
                yield return new ActivityLog
                {
                    EntityType = ActivityEntityTypes.ItemAttribute,
                    EntityId = attribute.Id,
                    ActionType = ActivityActionTypes.FieldUpdated,
                    ActorUserId = actorId,
                    ActorName = actor,
                    FieldName = fieldLabel,
                    OldValue = oldRaw?.ToString(),
                    NewValue = newRaw?.ToString(),
                    Message = $"{actor} updated {fieldLabel} on attribute {attribute.Name}",
                };
            }
        }

        private static IEnumerable<ActivityLog> BuildItemAttributeDeleted(
            ItemAttribute attribute, int? actorId, ActivityLookup lookup)
        {
            var actor = lookup.ActorName(actorId);
            yield return new ActivityLog
            {
                EntityType = ActivityEntityTypes.ItemAttribute,
                EntityId = attribute.Id,
                ActionType = ActivityActionTypes.Deleted,
                ActorUserId = actorId,
                ActorName = actor,
                Message = $"{actor} deleted attribute {attribute.Name}",
            };
        }

        private static string ItemDisplayLabel(Item item)
        {
            var name = item.ItemName?.Trim();
            var code = item.ItemCode?.Trim();
            if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(code))
            {
                return $"{name} ({code})";
            }

            return !string.IsNullOrWhiteSpace(name) ? name : code ?? $"Item #{item.Id}";
        }

        private static string ItemFieldLabel(string propertyName) => propertyName switch
        {
            nameof(Item.ItemCode) => "item code",
            nameof(Item.ItemName) => "item name",
            nameof(Item.Description) => "description",
            nameof(Item.SteelRate) => "steel rate",
            nameof(Item.HasVariants) => "has variants",
            _ => propertyName,
        };

        private static string ItemGroupFieldLabel(string propertyName) => propertyName switch
        {
            nameof(ItemGroup.Name) => "name",
            nameof(ItemGroup.Description) => "description",
            nameof(ItemGroup.ParentId) => "parent group",
            nameof(ItemGroup.SortOrder) => "sort order",
            nameof(ItemGroup.IsActive) => "active",
            _ => propertyName,
        };

        private static string ItemAttributeFieldLabel(string propertyName) => propertyName switch
        {
            nameof(ItemAttribute.Name) => "name",
            nameof(ItemAttribute.Code) => "code",
            nameof(ItemAttribute.ValueType) => "value type",
            nameof(ItemAttribute.IsVariantAttribute) => "variant attribute",
            nameof(ItemAttribute.SortOrder) => "sort order",
            nameof(ItemAttribute.IsActive) => "active",
            _ => propertyName,
        };

        private static string LeadFieldLabel(string propertyName) => propertyName switch
        {
            nameof(Lead.FirstName) => "first name",
            nameof(Lead.LastName) => "last name",
            nameof(Lead.Email) => "email",
            nameof(Lead.Mobile) => "mobile",
            nameof(Lead.Gender) => "gender",
            nameof(Lead.LeadSource) => "source",
            nameof(Lead.LeadOwnerId) => "lead owner",
            nameof(Lead.SalutationId) => "salutation",
            nameof(Lead.RequestTypeId) => "request type",
            nameof(Lead.Notes) => "notes",
            _ => propertyName,
        };

        private static string DealDisplayName(Deal d) =>
            string.IsNullOrWhiteSpace(d.OrganizationName)
                ? $"{d.FirstName} {d.LastName}".Trim()
                : d.OrganizationName;

        private static string FormatDealStageActivityMessage(string? stageName)
        {
            if (string.IsNullOrWhiteSpace(stageName))
            {
                return "Deal stage updated";
            }

            return stageName.Trim() switch
            {
                "Lead Closed - Won" => "Deal Closed Won",
                "Lead Closed - Lost" => "Deal Closed Lost",
                "Site Visit / Meeting Done" => "Site Visit Completed",
                "Negotiation Stage" => "Negotiation Started",
                _ => stageName.Trim(),
            };
        }

        private static string Truncate(string? text, int max)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return "(empty)";
            }

            var t = text.Trim();
            return t.Length <= max ? t : t[..max] + "…";
        }

        private sealed class ActivityLookup
        {
            private readonly TaskDbcontext _db;
            private readonly Dictionary<int, string> _users = new();
            private readonly Dictionary<int, string> _orgs = new();
            private readonly Dictionary<int, string> _statuses = new();
            private readonly Dictionary<int, string> _dealStatuses = new();
            private readonly Dictionary<int, string> _industries = new();
            private readonly Dictionary<int, string> _territories = new();
            private readonly Dictionary<int, string> _employeeCounts = new();
            private readonly Dictionary<int, string> _salutations = new();
            private readonly Dictionary<int, string> _requestTypes = new();
            private readonly Dictionary<int, string> _itemGroups = new();

            public ActivityLookup(TaskDbcontext db) => _db = db;

            public string ActorName(int? userId)
            {
                if (userId is not > 0)
                {
                    return "System";
                }

                if (!_users.TryGetValue(userId.Value, out var name))
                {
                    name = _db.Users.AsNoTracking()
                        .Where(u => u.Id == userId.Value)
                        .Select(u => u.FullName)
                        .FirstOrDefault();
                    name = string.IsNullOrWhiteSpace(name) ? "User" : name;
                    _users[userId.Value] = name;
                }

                return name;
            }

            public string LeadDisplayName(Lead lead)
            {
                if (lead.OrganizationId is > 0)
                {
                    var org = OrganizationName(lead.OrganizationId);
                    if (!string.IsNullOrWhiteSpace(org))
                    {
                        return org;
                    }
                }

                var person = $"{lead.FirstName} {lead.LastName}".Trim();
                return string.IsNullOrWhiteSpace(person) ? $"Lead #{lead.Id}" : person;
            }

            public string? OrganizationName(int? id) => Resolve(_orgs, id, () =>
                _db.Organizations.AsNoTracking().Where(o => o.Id == id).Select(o => o.Name).FirstOrDefault());

            public string? ItemGroupName(int? id) => Resolve(_itemGroups, id, () =>
                _db.ItemGroups.AsNoTracking().Where(g => g.Id == id).Select(g => g.Name).FirstOrDefault());

            public string? LeadStatusName(int? id) => Resolve(_statuses, id, () =>
                _db.LeadStatuses.AsNoTracking().Where(s => s.Id == id).Select(s => s.Name).FirstOrDefault());

            public string? DealStatusName(int? id) => Resolve(_dealStatuses, id, () =>
                _db.DealStatuses.AsNoTracking().Where(s => s.Id == id).Select(s => s.Name).FirstOrDefault());

            public string? IndustryName(int? id) => Resolve(_industries, id, () =>
                _db.Industries.AsNoTracking().Where(i => i.Id == id).Select(i => i.Name).FirstOrDefault());

            public string? TerritoryName(int? id) => Resolve(_territories, id, () =>
                _db.Territories.AsNoTracking().Where(t => t.Id == id).Select(t => t.Name).FirstOrDefault());

            public string? EmployeeCountName(int? id) => Resolve(_employeeCounts, id, () =>
                _db.EmployeeCounts.AsNoTracking().Where(e => e.Id == id).Select(e => e.Name).FirstOrDefault());

            public string? FormatLeadValue(string propertyName, object? value) => propertyName switch
            {
                nameof(Lead.OrganizationId) => OrganizationName(value as int?),
                nameof(Lead.LeadStatusId) => LeadStatusName(value as int?),
                nameof(Lead.SalutationId) => Resolve(_salutations, value as int?, () =>
                    _db.Salutations.AsNoTracking().Where(s => s.Id == (value as int?)).Select(s => s.Name).FirstOrDefault()),
                nameof(Lead.RequestTypeId) => Resolve(_requestTypes, value as int?, () =>
                    _db.RequestTypes.AsNoTracking().Where(r => r.Id == (value as int?)).Select(r => r.Name).FirstOrDefault()),
                nameof(Lead.LeadOwnerId) => ActorName(value as int?),
                _ => value?.ToString(),
            };

            private static string? Resolve(
                Dictionary<int, string> cache,
                int? id,
                Func<string?> fetch)
            {
                if (id is not > 0)
                {
                    return null;
                }

                if (!cache.TryGetValue(id.Value, out var name))
                {
                    name = fetch();
                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        cache[id.Value] = name;
                    }
                }

                return name;
            }
        }
    }
}
