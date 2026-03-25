# ORCHESTRA Story: GitHub PR-Style Content Review

## Summary

Transform the review section into a GitHub PR-style review experience where reviewers can:
- See a queue of content items awaiting their review
- Open a review detail view for each item (like opening a PR)
- Leave comments in discussion threads (like PR comments)
- Submit a formal review with a verdict: **Approve**, **Request Changes**, or **Comment Only**
- See a timeline of all review activity (submissions, comments, approvals, rejections)
- Track review history and resolution status

## Current State

### Backend (already exists)
- `ReviewWorkflowController`: submit for review, approve, reject (with role-based access)
- `DiscussionThreadsController`: create threads, reply, resolve threads
- `ContentItemHistoryController`: revisions, compare, rollback
- `ExternalReviewLinksController`: shareable review links
- Entities: `ContentItem` (status, reviewAssigneeEmail, rejectionReason, approvedUtc, approvedByEmail), `DiscussionThread`, `DiscussionComment`, `ContentItemRevision`
- Roles: Viewer=0, Editor=1, Reviewer=2, Admin=3
- Permission matrix: Reviewer can read + review; Editor can read + write

### Backend (needs new)
- **ContentReview entity**: formal review record with verdict (approved/changes_requested/comment)
- **Review-scoped comments**: link comments to a specific review submission
- **Review summary endpoint**: aggregate reviews per content item

### Frontend (needs building)
- Review page (`pages/app/review/index.vue`) is a stub — shows empty state
- No review queue, detail view, comment UI, or approve/reject controls

## Implementation Stories

### Story 1: Domain — ContentReview Entity

**Changes:**
1. New entity in `Entities.cs`:
   ```csharp
   public sealed class ContentReview
   {
       public Guid Id { get; set; } = Guid.NewGuid();
       public Guid ContentItemId { get; set; }
       public string ReviewerEmail { get; set; } = string.Empty;
       public string Verdict { get; set; } = string.Empty; // approved | changes_requested | comment
       public string Body { get; set; } = string.Empty; // overall review comment
       public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
   }
   ```

2. `AppDbContext`:
   - Add `DbSet<ContentReview> ContentReviews`
   - Table config: `content_reviews`, max lengths, indexes on `(ContentItemId, CreatedUtc)` and `ReviewerEmail`
   - FK: `ContentReview.ContentItemId → ContentItem.Id` with `Cascade`

3. EF migration: `AddContentReview`

4. Link comments to reviews — add `Guid? ReviewId` to `DiscussionComment`:
   - Nullable (existing comments have no review link, new review comments do)
   - FK: `DiscussionComment.ReviewId → ContentReview.Id` with `SetNull`

5. EF migration: `AddReviewIdToDiscussionComment`

**Acceptance:**
- Migrations apply cleanly
- `dotnet build` passes

### Story 2: Backend — Review API Endpoints

**Changes:**
1. New `ContentReviewsController`:
   - `GET /api/content-reviews?contentItemId=X` — list all reviews for a content item (ordered by createdUtc desc)
   - `GET /api/content-reviews/queue?reviewerEmail=X` — reviewer's queue: content items with status `in_review` assigned to them, plus unassigned `in_review` items
   - `POST /api/content-reviews` — submit a formal review:
     ```json
     {
       "contentItemId": "...",
       "verdict": "approved | changes_requested | comment",
       "body": "Overall review comment"
     }
     ```
     - `RequireAppRole(AppRole.Reviewer)`
     - If verdict is `approved`: also call the existing approve logic (update ContentItem status, fire webhooks)
     - If verdict is `changes_requested`: also call existing reject logic (set status back to draft, set rejectionReason)
     - If verdict is `comment`: no status change, just record the review
     - Always create a `ContentItemRevision` entry for audit trail
   - `GET /api/content-reviews/{id}` — get single review with its comments
   - `GET /api/content-reviews/{contentItemId}/timeline` — unified timeline of: reviews, comments, status changes, revisions — ordered chronologically

2. Update `DiscussionThreadsController`:
   - `POST /api/discussions/replies` should accept optional `reviewId` in the request to link comments to a review

3. Contracts:
   ```csharp
   record SubmitReviewRequest(Guid ContentItemId, string Verdict, string Body);
   record ReviewQueueItem { ContentItem, int CommentCount, int ReviewCount, string LatestReviewVerdict }
   record TimelineEntry { string Type, DateTime Timestamp, string ActorEmail, string Summary, object Details }
   ```

**Acceptance:**
- All endpoints respond correctly
- `dotnet build` + `dotnet test` pass
- Submitting "approved" review also approves the content item
- Submitting "changes_requested" review also rejects with the body as reason

### Story 3: Frontend — Types & API Client

**Changes:**
1. Types in `types.ts`:
   ```ts
   interface ContentReview {
     id: string
     contentItemId: string
     reviewerEmail: string
     verdict: 'approved' | 'changes_requested' | 'comment'
     body: string
     createdUtc: string
   }

   interface ReviewQueueItem {
     id: string
     key: string
     source: string
     status: string
     reviewAssigneeEmail: string
     projectId: string
     commentCount: number
     reviewCount: number
     latestReviewVerdict: string | null
   }

   interface TimelineEntry {
     type: 'review' | 'comment' | 'status_change' | 'revision'
     timestamp: string
     actorEmail: string
     summary: string
     details: any
   }

   interface DiscussionThread {
     id: string
     contentItemId: string
     title: string
     createdByEmail: string
     isResolved: boolean
     createdUtc: string
   }

   interface DiscussionComment {
     id: string
     threadId: string
     parentCommentId: string | null
     reviewId: string | null
     body: string
     authorEmail: string
     createdUtc: string
   }
   ```

2. New `reviewClient.ts`:
   - `getQueue(reviewerEmail)` → review queue
   - `getReviews(contentItemId)` → list reviews
   - `submitReview(contentItemId, verdict, body)` → submit
   - `getTimeline(contentItemId)` → unified timeline
   - `getThreads(contentItemId, includeResolved?)` → discussion threads
   - `getComments(threadId)` → thread comments
   - `createThread(contentItemId, title, body)` → new thread
   - `reply(threadId, body, parentCommentId?, reviewId?)` → reply
   - `resolveThread(threadId)` → resolve

**Acceptance:**
- Types compile, `npm run build` passes

### Story 4: Frontend — Review Queue Page

**Changes:**
1. Rewrite `pages/app/review/index.vue`:
   - Header: "Review" / "Content items waiting for your review"
   - Review queue list: cards showing each item needing review
   - Each card shows:
     - Content key (monospace)
     - Source text (truncated)
     - Status badge (in_review)
     - Assigned reviewer
     - Comment count, review count
     - Latest review verdict badge (if any previous reviews)
     - "Open Review" button → navigates to review detail
   - Filters: All / Assigned to me / Unassigned
   - Empty state when no items need review

2. New page `pages/app/review/[id].vue` — review detail (Story 5)

**Acceptance:**
- Queue loads and shows items in_review
- Filter works
- Cards display correct info
- `npm run build` passes

### Story 5: Frontend — Review Detail Page (PR-Style)

**Changes:**
1. New page `pages/app/review/[id].vue`:
   - **Header section**: Content key, source text, current status, assigned reviewer
   - **Timeline section** (left column, like GitHub PR timeline):
     - Chronological list of all activity: reviews, comments, status changes
     - Each review entry shows: reviewer avatar/email, verdict badge (green check/red X/grey comment), review body, timestamp
     - Each comment shows: author, body, timestamp, reply button
     - Status changes shown as events ("Mason submitted for review", "Jane approved")
   - **Discussion section**:
     - Existing threads with comments (collapsible)
     - "New comment" textarea at bottom of timeline
     - Reply to specific comments (nested like GitHub)
     - Resolve thread button on each thread
   - **Review submission panel** (right sidebar or bottom bar):
     - Textarea: "Leave a review comment"
     - Three submit buttons (like GitHub PR):
       - ✅ **Approve** (green) — "Submit review: Approve"
       - 🔄 **Request Changes** (red/orange) — "Submit review: Request Changes"
       - 💬 **Comment** (grey) — "Submit review: Comment only"
     - Only visible to users with Reviewer role
   - **Content diff view** (if source was edited since review started):
     - Show previous vs current source text
     - Highlight changes

2. UX details:
   - Review verdict badges: green (approved), red (changes_requested), grey (comment)
   - Timeline entries alternate styling for visual clarity
   - Auto-scroll to latest activity
   - Keyboard shortcuts: Ctrl+Enter to submit comment

**Acceptance:**
- Timeline shows all activity chronologically
- Can submit reviews with all three verdicts
- Approved review changes content status to approved
- Changes requested review changes status back to draft
- Comments display correctly with threading
- `npm run build` passes

### Story 6: A11y Review

- Timeline: ARIA landmarks, live region for new entries
- Review buttons: clear labels, keyboard accessible
- Comment threading: proper nesting roles
- Verdict badges: text + icon (not color alone)
- Focus management when submitting review or adding comment
- Skip-link maintained

### Story 7: QA Smoke Tests

- Submit content for review → appears in reviewer's queue
- Open review → timeline shows submission event
- Leave comment → appears in timeline
- Submit "Approve" review → content status changes to approved, appears in timeline
- Submit "Request Changes" → content status back to draft with reason
- Submit "Comment only" → no status change
- Filter queue: assigned to me vs all
- Threaded replies work
- Resolve thread
- `npm run build` + `dotnet build` + `dotnet test` pass

## Delivery Rules

- Strict phase order: Domain → Backend API → Frontend Types → Review Queue → Review Detail → A11y → QA
- Small commits to `main`
- Report after each story: changed files, validations, commit hash
- Raise blockers immediately

## UX Defaults (Mason)

- Helper text under labels, not placeholders
- Keep skip-link pattern
- GitHub PR-inspired but adapted for content review (not code review)
- Verdict buttons clearly labeled with icon + text
- Mobile-responsive: timeline stacks vertically on narrow screens
