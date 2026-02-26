# Live/Draft Refactoring — Status Report

**Date:** July 2025
**Branch:** `stream/060208`
**Build Status:** ✅ Green — 586 passed, 0 failed, 10 skipped (9 Phase 3 stubs + 1 pre-existing Auth0)

---

## 🔧 Design Decisions

### Decision 1 — Consolidate `AuditAction` enum from entity-specific to generic `Content*`

**Decision:** Replace the separate `Post*` and `Page*` audit action variants with a single
set of `Content*` actions. The existing `EntityType` enum (`BlogPost`, `Page`, etc.)
already carries sufficient context to distinguish what was affected.

**Current enum (to remove):**
```
PostCreated, PostUpdated, PostDeleted, PostPublished, PostUnpublished,
PostScheduled, PostScheduleUpdated, PostScheduleCancelled,
PageCreated, PageUpdated, PageDeleted, PagePublished, PageUnpublished
```

**Replacement:**
```csharp
// Content (BlogPost, Page — EntityType distinguishes which)
ContentCreated,
ContentUpdated,
ContentDeleted,
ContentPublished,
ContentUnpublished,
ContentScheduled,
ContentScheduleUpdated,
ContentScheduleCancelled,
```

**Also add** the missing page scheduling variants that were never in the enum:
`PageScheduled`, `PageScheduleUpdated`, `PageScheduleCancelled` — these are made
redundant by the consolidation and should simply not be added.

**Files to update when implementing:**
- `AuditLog.cs` — enum definition
- `AuditLogService.cs` — `GetDefaultDescription` switch (use the already-built `entity`
  string which includes `EntityType + name` for natural language, e.g.
  `"Published BlogPost 'My Post'"`)
- `PostsAdminFacade.cs` — 3 call sites (Create/Update/Delete)
- `PagesAdminFacade.cs` — 5 call sites (Create/Update/Delete/Publish/Unpublish),
  plus 3 new ones once Schedule/CancelSchedule methods are added
- `BlogPostAuditIntegrationTests.cs` — 3 assertions
- `PageAuditIntegrationTests.cs` — 3 assertions

> **Note:** This is a breaking change for any persisted audit log records using the old
> enum values. Since the project is pre-production, now is the correct time to make it.
> Enum values are serialised as integers in CosmosDB, so existing records would silently
> mis-classify unless explicitly migrated or the enum uses explicit integer assignments.
> **Recommendation:** Assign explicit integer values to all enum members to prevent
> future serialisation drift.

---

### Decision 3 — Content authorship: always stamp creator; allow ownership transfer via Adopt

**Context:** `BlogPost` and `Page` already have `AuthorId` and `AuthorName` properties.
Neither facade currently populates these fields at creation time, so they default to
empty strings. `PublishedBy` exists only on `BlogPostVersion`/`PageVersion` snapshots,
not on the main entity.

**Decision:**

1. **Stamp author at creation** — `CreatePostAsync` and `CreatePageAsync` in their
   respective facades set `AuthorId` and `AuthorName` from the current user’s claims
   via `IHttpContextAccessor`. This happens once; subsequent updates do not change the author.

2. **Adopt button** — When an admin is editing content whose `AuthorId` does not match
   their own user ID, an **Adopt** button appears in the edit view header. Clicking it
   transfers `AuthorId`/`AuthorName` to the current user and logs an audit entry.
   Use case: importing content, seeding, or taking over from a departed author.

3. **Audit action** — Add `ContentOwnershipTransferred` to the `AuditAction` enum
   (as part of the Phase 3 `Content*` consolidation). The description should record
   both the previous and new owner: `"Ownership of BlogPost 'My Post' transferred from
   Alice to Bob."`

4. **`PublishedBy` on the main entity** — The existing `AuthorId`/`AuthorName` represent
   the *content author*, not the *publisher* (who may be an editor or admin). Rather than
   adding a separate `PublishedById` field, rely on `ContentVersionService` snapshots
   for per-publish attribution and expose the last snapshot’s `PublishedByName` in the
   `AuditLogPanel` activity timeline. No new property is needed on `BlogPost`/`Page`.

**New facade methods:**
```csharp
Task AdoptPostAsync(string id, CancellationToken ct = default);
Task AdoptPageAsync(string id, CancellationToken ct = default);
```
Both read the current user from `IHttpContextAccessor`, update `AuthorId`/`AuthorName`,
save, and log `ContentOwnershipTransferred`.

**UI placement:** In the edit view header, alongside the status badge and publish
action buttons. The button is only visible when `currentUserId != post.AuthorId`.
No confirmation dialog — the action is reversible (any admin can adopt again).

---

### Decision 2 — Live/Draft embedded in document; version history in separate container

**Confirmed design:**

| Content | Storage |
|---------|---------|
| `BlogPost.Draft` | Embedded in the `BlogPosts` CosmosDB container (same document) |
| `BlogPost.Live` | Embedded in the `BlogPosts` CosmosDB container (same document) |
| `Page.Draft` | Embedded in the `Pages` CosmosDB container (same document) |
| `Page.Live` | Embedded in the `Pages` CosmosDB container (same document) |
| `BlogPostVersion` | Separate `BlogPostVersions` container |
| `PageVersion` | Separate `PageVersions` container |

**Rationale:** Admin UI always needs both Draft and Live simultaneously (to show
diff, current state, and allow promotion). A single document read delivers both
at one RU cost. Version history is read rarely — only on the history panel — so
the extra lookup there is acceptable and avoids bloating the main document.

**Current gap:** `ApplicationDbContext` does not register `Draft`/`Live` as
`OwnsOne` on either entity, and `BlogPostVersion`/`PageVersion` are not mapped
to their own containers. Both must be added before CosmosDB queries on nested
properties (`p.Live != null`, `p.Draft.SearchIndex`) will translate correctly.
See **Issue B** in the Addendum below for the exact configuration needed.

---

## 🎨 Edit View — State Display and Publish Workflow

### Current problems

The current `PostEdit.razor` header region has:
- A status badge driven from `_model.IsPublished` (a flat `bool` — loses nuance)
- "Publish Now", "Schedule", and "Unpublish" buttons whose visibility logic uses
  the same flat bool, so a scheduled-but-not-yet-live post shows the wrong set
- No distinction between "first publish scheduled" vs "update to live content scheduled"
- No "Deleted" state visible when navigating to a soft-deleted post's edit URL

### State model for display

All states are derived from two sources: `post.Live != null` and `post.Schedule.Status`.
The full matrix with the correct badge and button set is:

| Scenario | `Live` | `Schedule.Status` | Badge | Primary action | Secondary actions |
|----------|--------|-------------------|-------|----------------|-------------------|
| New draft | null | Draft | `Draft` | **Publish Now** | Schedule |
| Scheduled first publish | null | Scheduled | `Scheduled — [date]` | **Cancel Schedule** | Publish Now |
| Published, no pending change | not null | Draft | `Published — [date]` | **Unpublish** | Schedule Update |
| Published, update scheduled | not null | Scheduled | `Published · Update [date]` | **Cancel Schedule** | Publish Now, Unpublish |
| Soft-deleted | — | — | `Deleted` | *(none — read-only)* | *(restore button, future)* |

> The "Publish Now" action when content is already published means "promote the current
> Draft to Live immediately, replacing the Live version". The button label should read
> **"Publish Update Now"** in that context to make the intent clear.

### Badge design

Each state maps to a distinct CSS class and icon:

```
status-draft         → grey, pencil icon     → "Draft"
status-scheduled     → blue, clock icon      → "Scheduled — Jan 15, 2025 at 09:00"
status-published     → green, check icon     → "Published — Jan 10, 2025"
status-update        → teal, refresh icon    → "Published · Update Jan 15, 2025"
status-deleted       → red, bin icon         → "Deleted"
```

The badge is a single component (`ContentStatusBadge.razor`) accepting a `BlogPost`
or `Page` and computing display from it — not from the view-model bool.

### Button visibility rules (simplified)

```
isDeleted         → show nothing (read-only warning banner instead)
Live == null
  Status == Draft     → [Publish Now]  [Schedule...]
  Status == Scheduled → [Publish Now]  [Change Time]  [Cancel Schedule]
Live != null
  Status == Draft     → [Publish Update Now]  [Schedule Update...]  [Unpublish]
  Status == Scheduled → [Publish Update Now]  [Change Time]         [Cancel Schedule]  [Unpublish]
```

"Publish Now" and "Publish Update Now" are the same facade call — the label just
changes depending on whether Live already exists, so the user understands what
will happen.

**Change Time** replaces a separate Cancel + re-Schedule flow. The button opens
the same `ScheduleTimePicker` dialog pre-populated with the current scheduled date;
confirming simply calls `SchedulePostAsync` / `SchedulePageAsync` with the new value
(the facade method overwrites the existing `ScheduledPublishDate` in place). No
cancellation is needed first. The audit action logged is `ContentScheduleUpdated`
(or `PostScheduleUpdated` / `PageScheduleUpdated` until the enum is consolidated).

### Schedule time picker dialog

Used for both **Schedule** (new schedule) and **Change Time** (update existing schedule).
Because the operation is identical at the facade level, a single shared component
`ScheduleTimePicker.razor` covers both entry points:

- Opens as a **small Telerik dialog** (not a full-page modal — keep it compact).
- Shows a Telerik `TelerikDateTimePicker` defaulting to:
  - *New schedule:* tomorrow at 09:00 UTC.
  - *Change time:* the current `Schedule.ScheduledPublishDate`, pre-filled.
- Displays a small note inside the dialog: **"All times are UTC."**
- Validates that the selected time is in the future before enabling the confirm button.
- On confirm: calls facade → updates `ScheduledPublishDate` → closes dialog → badge
  and button set refresh automatically from the updated entity.
- On cancel: dismisses without calling the facade.

### Workflow intent (no modal overload)

Prefer a lighter pattern than the current dialogs-for-everything approach:

- **Publish Now / Publish Update Now** → inline confirmation in the button
  (button turns into "Click to confirm" on first press, executes on second)
  or a small inline banner. No modal.
- **Schedule / Change Time** → `ScheduleTimePicker` dialog (small, not full-screen).
- **Unpublish** → modal confirmation, because it removes Live content from the public site.
- **Cancel Schedule** → no confirmation needed; low-risk and reversible.

### Scope for `Pages` admin view

`Pages.razor` (the list) and the page edit view need the same badge component and
button logic as `PostEdit.razor`. Both `ContentStatusBadge.razor` and
`ScheduleTimePicker.razor` should be shared between posts and pages.



Two additional issues were identified beyond the original compile errors. Neither produces
a build failure but both cause silent runtime crashes in any CosmosDB-connected environment.

### Issue A — `p.IsPublished` inside EF Core LINQ expressions

`BlogPost.IsPublished` is a **computed C# property** (`=> Live != null`). EF Core's
CosmosDB LINQ translator only knows about mapped model properties. When it encounters
`p.IsPublished` inside a `.Where()` or `.OrderBy()` expression, it has no
corresponding stored field to translate to CosmosDB SQL and will throw:

```
InvalidOperationException: The LINQ expression '...' could not be translated.
```

**This is a silent runtime crash — it compiles fine.**

| File | Location | Expression | Safe? |
|------|----------|------------|-------|
| `CosmosDbBlogPostRepository.cs` | Line 58 | `p.IsPublished` | ❌ CosmosDB crash |
| `CosmosDbBlogPostRepository.cs` | Lines 83, 109, 133, 157, 190 | `p.IsPublished` | ❌ CosmosDB crash |
| `CosmosDbBlogPostRepository.cs` | Lines 235, 258 | `p.IsPublished` | ❌ CosmosDB crash |
| `FileSystemBlogPostRepository.cs` | Lines 30, 50, 70, 88, 106, 143, 181, 203 | `p.IsPublished` | ✅ Safe (LINQ-to-objects) |
| `PostsAdminFacade.cs` | Lines 43–44 | `p => p.IsPublished` / `p => !p.IsPublished` | ❌ CosmosDB crash |
| `PostsAdminFacade.cs` | Line 64 | `p => p.IsPublished` (sort key) | ❌ CosmosDB crash |

**Fix:** Replace `p.IsPublished` with `p.Live != null` in all EF Core query expressions.
For the `PostSortField.IsPublished` sort case, see note below.

> **Filesystem note:** `FileSystemBlogPostRepository` loads all entities into memory then
> runs LINQ-to-objects, so `p.IsPublished` evaluates the C# property directly and works
> correctly. No change needed there.

> **Sort by IsPublished:** `p => p.IsPublished` as an `orderBy` expression is also
> problematic. `p => p.Live != null` produces a `bool` sort which CosmosDB supports
> (true > false). This should translate correctly once `Live` is properly configured
> (see Issue B below). As a fallback, this sort can be done client-side.

---

### Issue B — `Draft` and `Live` are not registered as owned types in `ApplicationDbContext`

`ApplicationDbContext.ConfigureBlogEntities` configures scalar lists (`Tags`, `CategoryIds`,
etc.) but has **no `OwnsOne` or `ComplexProperty` mapping for `Draft` or `Live`**.

Without explicit configuration, EF Core CosmosDB will fall back to convention-based
discovery for `BlogPostContent Draft` and `BlogPostContent? Live`. In EF Core 8+/10 this
may auto-configure them as owned entities, but the behaviour is undefined and query
translation on nested properties (`p.Live != null`, `p.Draft.Title`, `p.Draft.SearchIndex`)
is not guaranteed to work.

**Fix:** Add explicit owned-type configuration in `ApplicationDbContext`:

```csharp
builder.Entity<BlogPost>(b =>
{
    b.ToContainer("BlogPosts");
    b.HasPartitionKey(p => p.GroupKey);
    b.HasNoDiscriminator();

    b.OwnsOne(p => p.Draft, draft => {
        draft.Property(d => d.Title);
        draft.Property(d => d.Markdown);
        draft.Property(d => d.Content);
        draft.Property(d => d.SearchIndex);
        draft.Property(d => d.Short);        // BlogPostContent only
        draft.Property(d => d.FeaturedImageUrl);
        draft.Property(d => d.FeaturedImageAlt);
        draft.Property(d => d.MetaDescription);
        draft.Property(d => d.MetaKeywords);
        draft.Property(d => d.ContentHash);
    });

    b.OwnsOne(p => p.Live, live => {
        live.Property(d => d.Title);
        live.Property(d => d.Markdown);
        live.Property(d => d.Content);
        live.Property(d => d.SearchIndex);
        live.Property(d => d.Short);
        live.Property(d => d.FeaturedImageUrl);
        live.Property(d => d.FeaturedImageAlt);
        live.Property(d => d.MetaDescription);
        live.Property(d => d.MetaKeywords);
        live.Property(d => d.ContentHash);
    });

    b.Property(p => p.Tags);
    b.Property(p => p.CategoryIds);
    b.Property(p => p.CategoryNames);
    b.Property(p => p.MediaUrls);
});
```

The same pattern is required for `Page` (`PageContent Draft`, `PageContent? Live`).

> **`Short` field note:** `Short` is defined on `BlogPostContent`, not `BaseContent`.
> EF Core's `OwnsOne` lambda won't see it via the `BaseContent` interface. The owned-type
> configuration must use the concrete type. Use `OwnsOne<BlogPostContent>(p => p.Draft, ...)`
> or let convention discover `Short` automatically — but verify it is included.

> **Implication for sorting/filtering:** Once `OwnsOne` is configured correctly, all of the
> following should translate cleanly to CosmosDB SQL:
> - `p.Live != null` → `IS_NULL(c["live"])` / `IS_DEFINED(c["live"])`
> - `p.Draft.SearchIndex.Contains(term)` → `CONTAINS(c["draft"]["searchIndex"], term)`
> - `p.Draft.Title` as sort key → `ORDER BY c["draft"]["title"]`

---

### Issue C — `SearchByTitleAsync` uses `.ToLower()` inside LINQ

`BlogSearchService.SearchByTitleAsync` constructs:

```csharp
p => p.Title.ToLower().Contains(normalizedTitleTerm)
```

After fixing to `p.Draft.Title.ToLower()`, this will need EF Core CosmosDB to translate
`LOWER(c["draft"]["title"])` into a CosmosDB SQL call. EF Core 8+ does support `ToLower()`
translation for CosmosDB, but it is safer to rely on the pre-built `SearchIndex` (which is
already lowercase) rather than calling `ToLower()` at query time. Aligning `SearchByTitleAsync`
to also use `Draft.SearchIndex.Contains(titleTerm)` would eliminate the risk entirely — since
`ContentProcessingService.UpdateSearchIndex` already incorporates the title into the index.

---

### Recommended order for these fixes

| Step | Action | Risk if skipped |
|------|--------|-----------------|
| 1 | Add `OwnsOne` for `Draft`/`Live` to `ApplicationDbContext` (both `BlogPost` and `Page`) | All nested-property queries silently fail |
| 2 | Replace `p.IsPublished` → `p.Live != null` in `CosmosDbBlogPostRepository` (8 locations) | Every published-post query crashes |
| 3 | Replace `p.IsPublished` → `p.Live != null` in `PostsAdminFacade` predicates and sort key | Admin post listing crashes |
| 4 | Fix `BlogSearchService` to use `p.Draft.SearchIndex` and `p.Draft.Title` | Search crashes |
| 5 | Align `SearchByTitleAsync` to use `SearchIndex` instead of `.ToLower().Contains()` | Low — works in EF Core 10, but fragile |

---

## Summary

The new Draft/Live content model is fully designed and the entity layer is complete. All 15 test files have been updated. The main blocker is that a handful of production files were not updated during the entity migration and still reference old properties or patterns.

There are also two classes of silent runtime crash in CosmosDB (not visible as compile errors):
the computed `IsPublished` property being used in LINQ, and the `Draft`/`Live` owned types
not being registered in the EF Core model.

---

## ✅ What's Done

### Entity Layer — Complete
| File | Status |
|------|--------|
| `BlogPost.cs` | ✅ Draft/Live model, `IsPublished` is read-only computed (`Live != null`) |
| `Page.cs` | ✅ Draft/Live model, `IsPublished` is read-only computed (`Live != null`) |
| `BaseContent.cs` | ✅ Shared content base with `ComputeHash()` |
| `BlogPostContent.cs` | ✅ Extends `BaseContent` with `Short` field |
| `PageContent.cs` | ✅ Extends `BaseContent` with `ShowTitle` field |
| `ContentSchedule.cs` | ✅ Value object with `Status`, `ScheduledPublishDate`, `PublishedAt` |
| `ContentStatus.cs` | ✅ Enum: `Draft`, `Scheduled`, `Published` |
| `ISchedulableContent.cs` | ✅ Interface contract for scheduling |
| `BlogPostVersion.cs` | ✅ Version history entity |
| `PageVersion.cs` | ✅ Version history entity |

### Service Layer — Complete
| File | Status |
|------|--------|
| `ContentSchedulingService.cs` | ✅ `PublishNowAsync`, `ScheduleForPublish`, `Unpublish` |
| `ContentVersionService.cs` | ✅ `PromoteDraftToLiveAsync`, version snapshots |
| `ContentProcessingService.cs` | ✅ `UpdateSearchIndex`, `CalculateReadingTime` |
| `SchedulableContentExtensions.cs` | ✅ `IsPublished()`, `GetLiveContent()` extensions |

### Repository Layer — Mostly Complete
| File | Status |
|------|--------|
| `IBlogPostVersionRepository.cs` | ✅ Defined |
| `IPageVersionRepository.cs` | ✅ Defined |
| `IPageRepository.cs` | ✅ Has `GetScheduledPagesReadyToPublishAsync` |
| `IBlogPostRepository.cs` | ⚠️ Missing `GetWhereAsync` / scheduled-post query method |

### Components — Complete (no errors)
| File | Status |
|------|--------|
| `PostCard.razor` | ✅ Uses `Post.Live` via `Content` computed property |
| `ShortPostList.razor` | ✅ Uses `post.Live?.Title`, `liveContent?.Short` |

### Tests — Complete (blocked by main project errors)
All 15 test files have been updated to the Draft/Live model. The test project
currently reports 1 error (`CS0006: Metadata file Viblog.dll not found`) which is
purely a downstream consequence of the main project not building. Once production
errors are resolved the test project should compile cleanly.

---

## ✅ Build Errors — Resolved

All 28 production errors from the original report have been fixed. Groups are kept for historical reference.

---

### Group A — Old entity method/property calls in `PagesAdminFacade.cs` (4 errors)

These methods were removed from `Page` when the old `IsPublished`/`PublishDate` model was replaced.

| Line | Error | Cause |
|------|-------|-------|
| 181 | `'Page' does not contain PublishDraftNow` | Old entity method call |
| 208 | `'Page' does not contain PublishDate` | Old property |
| 226 | `Page.IsPublished cannot be assigned — read only` | Computed property |
| 227 | `'Page' does not contain PublishDate` | Old property |

**Recommended action:** Replace direct entity manipulation with `ContentSchedulingService` calls:
```csharp
// PublishPageNowAsync
await _schedulingService.PublishNowAsync(page, userId, "Manual publish", cancellationToken);

// SchedulePagePublishingAsync
_schedulingService.ScheduleForPublish(page, publishDate);

// UnpublishPageAsync
_schedulingService.Unpublish(page);
```

---

### Group B — Nullable `PublishedAt` not guarded in `SitemapService.cs` (4 errors)

`BlogPost.PublishedAt` is now `DateTimeOffset?` but three lines dereference it directly.

| Line | Error | Fix |
|------|-------|-----|
| 69 | `'DateTimeOffset?' has no 'Year'` | `post.PublishedAt?.Year` |
| 70 | `'DateTimeOffset?' has no 'UtcDateTime'` | `post.PublishedAt?.UtcDateTime` |
| 103 | `'DateTimeOffset?' has no 'Year'` | `post.PublishedAt?.Year` |
| 103 | `'DateTimeOffset?' has no 'Month'` | `post.PublishedAt?.Month` |

**Recommended action:** Apply null-conditional operator and provide fallback values.  
For the URL (line 69), a post in the sitemap should always be published, so `post.PublishedAt!.Value.Year` with a null-guard assertion is also acceptable. For the LINQ projection (line 103), filter nulls first:
```csharp
.Where(p => p.PublishedAt.HasValue)
.Select(p => new { Year = p.PublishedAt!.Value.Year, Month = p.PublishedAt!.Value.Month })
```

---

### Group C — `Title` / `SearchIndex` accessed on `BlogPost` root (7 errors in PostsAdminFacade + BlogSearchService + Post.razor)

`Title`, `Content`, `Markdown`, `Short`, `SearchIndex`, and `MetaDescription` moved to `BlogPostContent` (under `Draft` and `Live`). These files still reference them on the entity directly.

**`PostsAdminFacade.cs`** (6 errors — lines 52, 95, 96, 114, 115, 140, 141):
- Audit log calls like `post.Title` → `post.Draft.Title`
- Sort expression `p => p.Title` → `p => p.Draft.Title`

**`BlogSearchService.cs`** (3 errors — lines 33, 55, 90):
- `p.SearchIndex` → `p.Draft.SearchIndex`
- `p.Title.ToLower()` → `p.Draft.Title.ToLower()`

> **Resolved:** Add a `publishedOnly` parameter to `BlogSearchService`. Public
> callers pass `true` and search `Live.SearchIndex` only. Admin callers pass `false`
> and search both `Live.SearchIndex` and `Draft.SearchIndex` (so unpublished drafts
> appear in admin results). The existing `SearchByTagAsync` / `SearchByTitleAsync` /
> `SearchByContentAsync` methods each need this flag added.

**`Post.razor`** (1 error — line 191):
- `_post!.Title` in breadcrumb → `_post!.Live?.Title ?? _post!.Draft.Title`

---

### Group D — `PageView.razor` missing `@using` for extension method (1 error)

`_page.IsPublished()` calls the extension method from `SchedulableContentExtensions`
but the namespace is not imported.

**Recommended action:** Add to top of `PageView.razor`:
```razor
@using Viblog.Shared.Extensions
```

---

### Group E — `PostEdit.razor` publish model mismatch (6 errors from Razor-generated code)

Two distinct sub-issues:

1. **`DateTimeInput` type mismatch** (lines 116–118): The `DateTimeInput` component
   expects `DateTimeOffset` (non-nullable) but `BlogPostModel.PublishedAt` is `DateTimeOffset?`.
   **Resolved — skip this fix.** `PublishedAt` is removed from `BlogPostModel` entirely
   in Phase 4 (it is set by the service, not the editor). Fixing the nullable binding
   now would be immediately thrown away. Leave these 6 errors; they will be cleared
   when Phase 4 removes the field and the `DateTimeInput` binding with it.

2. **`post.IsPublished = _model.IsPublished`** (line 349): `IsPublished` is a read-only
   computed property (`Live != null`). Setting it directly is no longer valid.

   **Recommended action:** Replace the publish toggle with a call to
   `ContentSchedulingService`. Publishing should promote `Draft → Live` via
   `PublishNowAsync`; unpublishing should call `Unpublish(post)` (which sets `Live = null`).

---

### Group F — `Pages.razor` uses removed `PublishDate` property (3 errors)

Lines 121 and 124 reference `item.PublishDate` which was replaced by `item.Schedule.ScheduledPublishDate`.

**Recommended action:**
```razor
@* OLD *@
var hasScheduledDate = item.PublishDate.HasValue && item.PublishDate.Value > DateTimeOffset.UtcNow;

@* NEW *@
var hasScheduledDate = item.Schedule.ScheduledPublishDate.HasValue &&
                       item.Schedule.ScheduledPublishDate.Value > DateTimeOffset.UtcNow;
```

---

### Group G — `ContentPublishingBackgroundService.cs` calls missing `GetWhereAsync` (2 errors)

The background worker calls `_blogPostRepository.GetWhereAsync(...)` and
`_pageRepository.GetWhereAsync(...)`. Neither interface defines this method.
`IPageRepository` already has `GetScheduledPagesReadyToPublishAsync()` for exactly
this purpose. `IBlogPostRepository` has no equivalent.

**Recommended action (two-part):**

1. Add a domain-specific method to `IBlogPostRepository`:
   ```csharp
   Task<IEnumerable<BlogPost>> GetScheduledPostsReadyToPublishAsync(
       CancellationToken cancellationToken = default);
   ```
   And implement it in `CosmosDbBlogPostRepository` (and any file system equivalent).

2. Rewrite the background service to call the domain methods instead of a generic filter:
   ```csharp
   var readyToPublish = await _blogPostRepository.GetScheduledPostsReadyToPublishAsync(cancellationToken);
   var readyToPublish = await _pageRepository.GetScheduledPagesReadyToPublishAsync(cancellationToken);
   ```

---

## 📋 Remaining Logic Steps

These are work items beyond compile-error fixes — logic that is incomplete or untested.

### Step 1 — Wire `ContentSchedulingService` into admin facades
`PagesAdminFacade` and the publish actions in `PostEdit.razor` still use direct
entity mutation. The `ContentSchedulingService` exists and is ready; it just needs
to be injected and called. `PostsAdminFacade` likely needs similar treatment for
any publish/unpublish actions once `PostEdit.razor` is resolved.

### Step 2 — Complete `PostEdit.razor` publish UX
The `PostEdit.razor` form currently has a single `IsPublished` checkbox and a
`PublishedAt` date field. This does not match the new state model (Draft → Schedule → Published).
The form needs to:
- Show current state (Draft / Scheduled / Published)
- Offer state-transition actions: **Publish Now**, **Schedule...**, **Unpublish**
- Remove the raw `IsPublished` checkbox (it's derived, not editable)

### Step 3 — `Posts.razor` status column
The `Posts.razor` page currently infers "Scheduled" from `post.PublishedAt > UtcNow`
(the old pattern). It should now use `post.Schedule.Status == ContentStatus.Scheduled`
and `post.HasPendingUpdate` to correctly show states.

### Step 4 — `StructuredDataHelper.cs` and `FeedFacade.cs`
These files were flagged in the earlier TEST-FIXES-REPORT as needing updates to
access `post.Live?.Title`, `post.Live?.Content`, etc. They were not fixed in the
initial pass (and may have been the source of some of the 168 remaining errors in
that report). Need to audit current error count and fix content property access
to go through `Live`.

### Step 5 — `PartitionKeyExtensions.cs` logic
Two lines in `PartitionKeyExtensions.cs` reference `post.IsPublished` (as a bool
assignment) and `post.PublishedAt.Year` (without null guard). These should be updated:
- `if (post.IsPublished)` → `if (post.IsPublished())` (extension method) or `if (post.Live != null)`
- `post.PublishedAt?.Year.ToString() ?? "draft"` for the group key

### Step 6 — `FileSystemPageRepository.cs` and `FileSystemBlogPostRepository.cs`
The filesystem repositories still use `p.IsPublished` (property) in query predicates
and call the removed `PromoteDraftIfScheduled()`. These need updating to the new model.
Low priority if CosmosDB is the primary deployment target.

### Step 7 — Admin search via `Draft.SearchIndex`
`BlogSearchService` currently searches `p.SearchIndex` (root), which no longer exists.
For public search: use `p.Live.SearchIndex`. Admin search (all content): use `p.Draft.SearchIndex`.
The service needs a `publishedOnly` guard that also controls which content layer is searched.

---

## 🧪 Test Coverage Assessment

### What's covered
- `BlogSearchService` — comprehensive (constructor, all 3 search methods, edge cases, normalization)
- `StructuredDataHelper` — covered
- `SitemapService` — covered
- All major facades (BlogPostList, BlogPostDetail, FrontPage, Archive, Category, Tag, Feed, BlogSearch, PagesAdmin, PageDetail)
- Integration tests for audit logging (BlogPost + Page)
- Authentication: `Auth0AuthenticationStateProvider`, `Auth0SyncService`, `UserManagementService`, `UserManagementFacade`

### What's missing — new content services (highest priority)

No test files exist yet for any of the three core content services or their
supporting types. These contain the most critical business rules in the refactoring.

**`ContentSchedulingServiceTests.cs`** (new file)
| Test | Covers |
|------|--------|
| `PublishNowAsync_FirstPublish_SetsPublishedAtOnBlogPost` | First-publish date is stamped |
| `PublishNowAsync_RePublish_DoesNotChangeOriginalPublishedAt` | Re-publish does not overwrite first date |
| `PublishNowAsync_ClearsScheduledPublishDate` | Schedule cleared after publish |
| `PublishNowAsync_SetsScheduleStatusToDraft` | Status reset to Draft after publish |
| `PublishNowAsync_NullDraft_ThrowsInvalidOperationException` | Guard: no Draft content |
| `ScheduleForPublish_FutureDate_SetsStatusToScheduled` | Happy path scheduling |
| `ScheduleForPublish_PastDate_ThrowsArgumentException` | Past date rejected |
| `ScheduleForPublish_NowDate_ThrowsArgumentException` | Now (not future) rejected |
| `Unpublish_ClearsLiveAndResetsStatus` | Unpublish removes Live |
| `Unpublish_WhenNotPublished_DoesNotThrow` | Idempotent unpublish |

**`ContentVersionServiceTests.cs`** (new file)
| Test | Covers |
|------|--------|
| `PromoteDraftToLiveAsync_CopiesDraftFieldsToLive` | All Draft fields appear in Live |
| `PromoteDraftToLiveAsync_LiveIsIndependentCopy` | Mutating Draft after promote does not affect Live |
| `PromoteDraftToLiveAsync_CreatesVersionSnapshot` | Snapshot saved to repository |
| `PromoteDraftToLiveAsync_VersionNumberIncrements` | Each promote increments version |
| `PromoteDraftToLiveAsync_NullDraft_ThrowsInvalidOperationException` | Guard |
| `ClearLive_SetsLiveToNull` | Unpublish clears Live reference |

**`ContentProcessingServiceTests.cs`** (new file)
| Test | Covers |
|------|--------|
| `UpdateSearchIndex_IncludesTitleAndMarkdown` | Index contains title and body text |
| `UpdateSearchIndex_IsNormalisedToLowerCase` | Search is case-insensitive |
| `UpdateSearchIndex_IncludesAdditionalText` | Tags/categories fed in |
| `UpdateSearchIndex_EmptyContent_ProducesEmptyIndex` | Edge case |
| `CalculateReadingTime_TwoHundredWords_ReturnsOne` | Minimum 1 minute |
| `CalculateReadingTime_ZeroWords_ReturnsZero` | Empty content |
| `CalculateReadingTime_FourHundredWords_ReturnsTwo` | Multi-minute post |
| `RenderMarkdown_BasicMarkdown_ReturnsHtml` | Conversion works |
| `RenderMarkdown_NullInput_ReturnsEmpty` | Edge case |

**`SchedulableContentExtensionsTests.cs`** (new file)
| Test | Covers |
|------|--------|
| `IsPublished_BlogPost_WhenLiveIsNull_ReturnsFalse` | Draft state |
| `IsPublished_BlogPost_WhenLiveIsSet_ReturnsTrue` | Published state |
| `IsPublished_Page_WhenLiveIsNull_ReturnsFalse` | Draft state |
| `IsPublished_Page_WhenLiveIsSet_ReturnsTrue` | Published state |
| `GetLiveContent_BlogPost_ReturnsLiveContent` | Live access |
| `GetLiveContent_WhenNotPublished_ReturnsNull` | Null guard |

**Other gaps (add after build is fixed)**
| Area | Gap |
|------|-----|
| `ContentPublishingBackgroundService` | Processes due content, skips future, handles single-item errors gracefully |
| `PagesAdminFacade` publish/schedule/unpublish | Requires Group A fixes first |
| `PostsAdminFacade` publish/schedule/unpublish | Requires Group E fixes and new methods first |

---

## 🔔 Audit Logging Gaps

The existing audit infrastructure is solid — `IAuditLogService`, `AuditLogService`, the
`AuditLog` entity, and the `AuditAction`/`EntityType`/`ActionResult` enums are all in place.
`PostsAdminFacade` already logs Create, Update, and Delete. Several gaps remain.

### `AuditAction` enum — superseded by Design Decision 1

The previously noted missing values (`PageScheduled`, `PageScheduleUpdated`,
`PageScheduleCancelled`) are **not to be added** as standalone entries.
They are made redundant by the confirmed consolidation to `Content*` actions
(see Design Decision 1). The enum rewrite in Phase 3 replaces both the existing
`Post*`/`Page*` values and covers all scheduling variants under `ContentScheduled`,
`ContentScheduleUpdated`, and `ContentScheduleCancelled`.

### Actions that need logging but currently don't have it

| Action | Location | `AuditAction` (after Phase 3) |
|--------|----------|-------------------------------|
| Publish post now | `PostsAdminFacade` (method doesn't exist yet) | `ContentPublished` |
| Schedule post | `PostsAdminFacade` (method doesn't exist yet) | `ContentScheduled` |
| Update post schedule | `PostsAdminFacade` (method doesn't exist yet) | `ContentScheduleUpdated` |
| Cancel post schedule | `PostsAdminFacade` (method doesn't exist yet) | `ContentScheduleCancelled` |
| Unpublish post | `PostsAdminFacade` (method doesn't exist yet) | `ContentUnpublished` |
| Publish page now | `PagesAdminFacade.PublishPageNowAsync` — broken (Group A) | `ContentPublished` |
| Schedule page | `PagesAdminFacade.SchedulePagePublishingAsync` — broken (Group A) | `ContentScheduled` |
| Cancel page schedule | Not yet implemented | `ContentScheduleCancelled` |
| Unpublish page | `PagesAdminFacade.UnpublishPageAsync` — broken (Group A) | `ContentUnpublished` |
| Background auto-publish | `ContentPublishingBackgroundService` — uses `GetWhereAsync` (Group G) | `ContentPublished` — not logged at all |

### Entity history panel on edit views

`IAuditLogService.GetEntityHistoryAsync(EntityType, entityId, pagingParameters)` already
exists and returns a paged result. What's needed is a reusable Blazor component that calls
it and renders the log as a compact timeline.

**Proposed component:** `AuditLogPanel.razor`

```razor
@* Displays entity-scoped audit history — embed on PostEdit and PageEdit views *@
<AuditLogPanel EntityType="EntityType.BlogPost" EntityId="@post.Id" MaxItems="20" />
```

The component should:
- Load lazily (only when the user scrolls to it or clicks an "Activity" tab)
- Show: timestamp, user, action label (human-readable), description
- Use the existing `AuditLog.Timestamp`, `UserName`, `Action`, `Description` fields
- Support a "Load more" pager (backed by `GetEntityHistoryAsync` with `PagingParameters`)

---

## 🎨 Admin View Redesign — Publish State & Actions

> See **Design Decisions → Edit View** at the top of this document for the full
> state matrix, badge design, button visibility rules, and workflow intent.
> This section tracks the implementation scope.

### What still needs to change in code

**New shared component** `ContentStatusBadge.razor`
- Accepts a `BlogPost` or `ISchedulableContent` parameter
- Derives state from `Live != null` + `Schedule.Status` + `IsDeleted`
- Renders the correct badge CSS class, icon, and human-readable label + date
- Used in `PostEdit.razor`, `PageEdit.razor`, `Posts.razor` list column, `Pages.razor` list column

**`BlogPostModel` — remove publish fields from the view-model**
- Remove `bool IsPublished` — this is now derived from `post.Live != null`
- Remove `DateTimeOffset? PublishedAt` from the editable form fields
  (it remains on the entity; the service sets it, the editor does not)
- The model represents Draft content only: title, slug, excerpt, markdown, metadata, tags, categories

**`PostEdit.razor` — wire up action buttons**
- Replace status badge rendered from `_model.IsPublished` with `<ContentStatusBadge>`
- Replace existing button handlers (which directly mutate `post.IsPublished`) with calls
  to the new `PostsAdminFacade` publish/schedule/unpublish methods (Phase 3 prerequisite)
- Replace Schedule dialog with inline date/time picker per the workflow spec
- Handle `IsDeleted` post gracefully: show read-only deleted banner, hide action buttons

**`PostsAdminFacade` — new publish methods** (none exist today)
```csharp
Task PublishPostNowAsync(string id, string publishedBy, CancellationToken ct = default);
Task SchedulePostAsync(string id, DateTimeOffset publishDate, CancellationToken ct = default);
Task CancelPostScheduleAsync(string id, CancellationToken ct = default);
Task UnpublishPostAsync(string id, CancellationToken ct = default);
```
Each calls `ContentSchedulingService`, saves, and logs the matching `AuditAction`.

**`Posts.razor` status column**
- Replace `post.PublishedAt > UtcNow` heuristic with `post.Schedule.Status`
- Add `HasPendingUpdate` display ("Published · Update Jan 15")
- Use `<ContentStatusBadge>` component

**`Pages.razor` status column**
- Fix Group F compile errors (`PublishDate` → `Schedule.ScheduledPublishDate`)
- Add `<ContentStatusBadge>` component (same as posts)

**`PagesAdminFacade` — fix and complete** (Group A compile errors + missing schedule method)
- Wire `PublishPageNowAsync` to `ContentSchedulingService.PublishNowAsync`
- Wire `SchedulePagePublishingAsync` to `ContentSchedulingService.ScheduleForPublish`
- Wire `UnpublishPageAsync` to `ContentSchedulingService.Unpublish`
- Add `CancelPageScheduleAsync` (missing entirely)



---

## 🔍 Interesting Observations

### `HasPendingUpdate` is already a computed property — not an extension method
`HasPendingUpdate` was noted earlier as something to add as an extension method. In fact
it already exists as a read-only computed property on both `BlogPost` and `Page`:
```csharp
public bool HasPendingUpdate => IsPublished && Schedule.Status == ContentStatus.Scheduled;
```
The list view and badge component can reference it directly.

**However:** this property contains logic inside the model, which contradicts the project
guideline of keeping models logic-free. Decide: leave it as-is (it is simple derived state,
not business logic), or remove the property and add `HasPendingUpdate(this ISchedulableContent c)`
as an extension method in `SchedulableContentExtensions`. Either way, the three entity-state
tests in `SchedulableContentExtensionsTests.cs` remain valid test coverage regardless of where
the implementation lives.

### `Posts.razor` has a stray unused field
`private BlogPostContent _content;` is declared in the component's `@code` block
but is never assigned or used. This will cause a warning (possibly an error depending
on nullable settings) and should be removed.

### `PostsAdminFacade` sort by `IsPublished` is a boolean sort on a computed property
`p => p.IsPublished` is used as a sort expression. This may not translate well to
a CosmosDB LINQ query since `IsPublished` is a computed C# property, not a stored
field. For CosmosDB this would need to sort by a real stored field, or the sorting
should be handled client-side after fetching.

### `BlogPostModel.IsPublished` is a flat boolean, not a state enum
The admin model still has `public bool IsPublished { get; set; }` from the old design.
Once the form is updated (Phase 4), this should become a `ContentStatus` or be removed
in favor of explicit publish/unpublish actions. Keeping a boolean here creates an
impedance mismatch between the model and the new entity state.

### C# 14 extension member syntax in `BlogPostExtensions.cs`
The file uses C# 14 `extension(BlogPost post) { ... }` syntax. This is the new
extension member syntax. It compiles correctly on .NET 10 with C# 14 but is a
notable modern pattern that team members should be aware of.

### Version repositories not yet registered in DI
`IBlogPostVersionRepository` and `IPageVersionRepository` are defined and their
CosmosDB implementations exist. Verify they are registered in
`CosmosDbServiceExtensions.cs` and `RegisterSharedExtensions.cs` — if `ContentVersionService`
is injected anywhere before they're registered, the app will throw at startup.

### `PostEdit.razor` buttons already exist but are wired to broken handlers
The Publish Now / Schedule / Unpublish buttons are already rendered in the page header.
The UI structure for Phase 4 is largely already there — the main work is rewiring the
`@onclick` handlers to use the new facade methods instead of direct entity mutation.

### `ContentSchedulingService` has no audit logging by design
The service is pure business logic with no data-access or HTTP dependencies.
This is correct — the facade layer (the caller) is responsible for logging. Ensure
every facade method that calls the scheduling service also calls `LogAuditAsync`
immediately after a successful state change.

### `ContentVersionService` `PublishedByName` resolved
In `CreatePublishedSnapshotAsync`, `PublishedByName` is currently set to the same value
as `PublishedBy` (the user ID). **Resolved:** the facade already has `IHttpContextAccessor`
and can read the user's display name from claims. The facade should pass both `publishedBy`
(ID) and `publishedByName` (display name) as parameters to `PromoteDraftToLiveAsync`.
`ContentVersionService` stays dependency-free; the resolution happens at the call site.

---

## ✅ Action Checklist

Items are listed in the recommended execution order. Check each off as it is completed.
Phase boundaries are noted for context but individual items can be ticked independently.

### Phase 1A — Stub to build green ✅ Complete

Touch as little code as possible. The goal is a compiling project so the 15 existing tests
can run. All stubs will be replaced properly in Phase 1B or Phase 3 — they are not permanent.

- [x] `PagesAdminFacade.cs` — **stub** comment out the offending lines in `PublishPageNowAsync`,
  `SchedulePagePublishingAsync`, and `UnpublishPageAsync`; replace each method body with
  `throw new NotImplementedException("Pending rewrite — Phase 3");` *(clears 4 errors)*
- [x] `PostEdit.razor` — **stub** comment out `post.IsPublished = _model.IsPublished;` *(removes that error)*
- [x] `ContentPublishingBackgroundService.cs` — **stub** comment out the `GetWhereAsync` call sites;
  replace execution body with `await Task.CompletedTask; // TODO: rewire in Phase 3` *(clears 2 errors)*

After Phase 1A the remaining errors should be only Groups B, C, D, and F.

### Phase 1B — Proper quick fixes ✅ Complete

These are small mechanical changes. None of them will be revisited.

- [x] `PageView.razor` — add `@using Viblog.Shared.Extensions` *(1 error)*
- [x] `SitemapService.cs` — guard nullable `PublishedAt` with `?.` and `!.Value` *(4 errors)*
- [x] `Pages.razor` — replace `item.PublishDate` with `item.Schedule.ScheduledPublishDate` *(3 errors)*
- [x] `ApplicationDbContext.cs` — add `OwnsOne` for `Draft`/`Live` on `BlogPost` and `Page`; register `BlogPostVersions` and `PageVersions` containers *(prevents runtime crash)*
- [x] `CosmosDbBlogPostRepository.cs` — replace `p.IsPublished` with `p.Live != null` in all 8 LINQ expressions *(prevents runtime crash)*
- [x] `IBlogPostRepository.cs` — add `GetScheduledPostsReadyToPublishAsync`; implement in `CosmosDbBlogPostRepository` and `FileSystemBlogPostRepository` *(unblocks Phase 3 background service rewrite)*
- [x] `BlogSearchService.cs` — add `publishedOnly` parameter; when `false` (admin) search both `Live.SearchIndex` and `Draft.SearchIndex`; when `true` (public) search `Live.SearchIndex` only; replace root `p.SearchIndex`/`p.Title` references with `p.Draft.*` *(3 errors)*
- [x] `PostsAdminFacade.cs` — set `AuthorId`/`AuthorName` from current user in `CreatePostAsync` *(no errors — prevents empty author)*
- [x] `PagesAdminFacade.cs` — set `AuthorId`/`AuthorName` from current user in `CreatePageAsync` *(no errors — prevents empty author)*
- [x] `PostsAdminFacade.cs` — replace `post.Title` with `post.Draft.Title` in audit calls; replace `p.Title` sort with `p.Draft.Title`; replace `p.IsPublished` predicates with `p.Live != null` *(6 errors + 2 runtime fixes)*
- [x] `Post.razor` — replace `_post!.Title` in breadcrumb with `_post!.Live?.Title ?? _post!.Draft.Title` *(1 error)*
- [x] `PostEdit.razor` — leave `DateTimeInput` binding untouched (field removed in Phase 4); stub comment from 1A should already handle remaining Group E errors

**Phase 1 result: build green. 532 passed, 0 failed, 10 skipped.**

> **Skipped tests (9):** `PagesAdminFacadeTests` — `PublishPageNowAsync_*` (2), `SchedulePagePublishingAsync_*` (2), `UnpublishPageAsync_*` (2);
> `PageAuditIntegrationTests` — `PublishPage_LogsAuditEntry`, `UnpublishPage_LogsAuditEntry`, `PageLifecycle_CreatesCompleteAuditTrail`.
> All marked `[Fact(Skip = "Pending rewrite — Phase 3. See Docs/LIVE-DRAFT-REFACTORING-STATUS.md")]`.
> Remove the `Skip` and restore assertions to `Content*` audit actions when Phase 3 implements the real methods.

### Write tests for already-complete services

These services have no compile errors and are fully implemented. Write their tests now,
immediately after the build is green — not as a deferred batch. From Phase 3 onwards,
each item gets its tests written alongside the implementation.

- [x] Create `ContentSchedulingServiceTests.cs` — first publish, re-publish, schedule validation, unpublish *(11 tests — all passing)*
- [x] Create `ContentVersionServiceTests.cs` — promote copies fields, clone independence, snapshot creation, version increment *(10 tests — all passing)*
- [x] Create `ContentProcessingServiceTests.cs` — search index content and normalisation, reading time, markdown render *(12 tests — all passing)*
- [x] Create `SchedulableContentExtensionsTests.cs` — `IsPublished()` and `GetLiveContent()`; `HasPendingUpdate` tests belong here as entity-property tests (see observation) *(21 tests — all passing)*

### Phase 3 — Audit logging and new facade methods

Write tests alongside each item as it is implemented.

- [ ] `AuditLog.cs` — consolidate `Post*`/`Page*` content actions into `Content*` enum values; add `ContentOwnershipTransferred`; assign explicit integer values to all enum members
- [ ] `AuditLogService.cs` — update `GetDefaultDescription` switch to `Content*` arms
- [ ] `PostsAdminFacade.cs` — update 3 existing audit call sites to `Content*`
- [ ] `PagesAdminFacade.cs` — update 3+ existing audit call sites to `Content*`
- [ ] `BlogPostAuditIntegrationTests.cs` — update 3 assertions to `Content*`
- [ ] `PageAuditIntegrationTests.cs` — update 3 assertions to `Content*`
- [ ] `PostsAdminFacade.cs` — add `PublishPostNowAsync`, `SchedulePostAsync`, `CancelPostScheduleAsync`, `UnpublishPostAsync`, `AdoptPostAsync` each calling the appropriate service and logging audit; **write tests alongside**
- [ ] `IPostsAdminFacade.cs` — add the four new publish/schedule methods and `AdoptPostAsync` to the interface
- [ ] `PagesAdminFacade.cs` — **replace Phase 1A stubs** with proper `ContentSchedulingService` calls; add `CancelPageScheduleAsync` and `AdoptPageAsync`; verify all publish methods log audit; **write tests alongside**
- [ ] `IPagesAdminFacade.cs` — add `CancelPageScheduleAsync` and `AdoptPageAsync` to the interface
- [ ] `ContentPublishingBackgroundService.cs` — **replace Phase 1A stub** with proper domain method calls; log `ContentPublished` for each auto-published item; **write tests alongside**
- [ ] `ContentVersionService.cs` — update `PromoteDraftToLiveAsync` signature to accept `publishedByName` as a parameter; remove the TODO comment
- [ ] Create `AuditLogPanel.razor` — lazy-loading entity history timeline backed by `GetEntityHistoryAsync`
- [ ] `PostEdit.razor` — embed `<AuditLogPanel>` component
- [ ] `PageEdit.razor` — embed `<AuditLogPanel>` component

### Phase 4 — Admin view redesign

- [ ] `Posts.razor` — remove stray unused `_content` field
- [ ] Create `ContentStatusBadge.razor` — derives state from `Live != null` + `Schedule.Status` + `IsDeleted`; used in all four views
- [ ] Create `ScheduleTimePicker.razor` — shared Telerik dialog for both Schedule and Change Time actions; pre-populates existing date when editing
- [ ] `BlogPostModel` — remove `bool IsPublished` and `DateTimeOffset? PublishedAt` form fields
- [ ] `PostEdit.razor` — replace status badge with `<ContentStatusBadge>`; wire action buttons to new facade methods; add Change Time and Adopt buttons; handle deleted-post read-only state
- [ ] `Posts.razor` — status column: replace `PublishedAt > UtcNow` heuristic with `Schedule.Status`; add `HasPendingUpdate` display; use `<ContentStatusBadge>`
- [ ] `Pages.razor` — status column: use `<ContentStatusBadge>` (Phase 1 fixes compile errors; this adds full scheduling-aware display)
- [ ] `PageEdit.razor` — add publish action buttons, Adopt button, `<ContentStatusBadge>`, and `<ScheduleTimePicker>` mirroring `PostEdit.razor`
- [ ] Verify `IBlogPostVersionRepository` and `IPageVersionRepository` are registered in DI

