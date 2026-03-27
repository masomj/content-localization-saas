using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContentLocalizationSaaS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Story81ForeignKeyConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddForeignKey(
                name: "FK_content_item_language_tasks_content_items_ContentItemId",
                table: "content_item_language_tasks",
                column: "ContentItemId",
                principalTable: "content_items",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_content_item_revisions_content_items_ContentItemId",
                table: "content_item_revisions",
                column: "ContentItemId",
                principalTable: "content_items",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_content_items_copy_components_CopyComponentId",
                table: "content_items",
                column: "CopyComponentId",
                principalTable: "copy_components",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_content_items_projects_ProjectId",
                table: "content_items",
                column: "ProjectId",
                principalTable: "projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_copy_components_projects_ProjectId",
                table: "copy_components",
                column: "ProjectId",
                principalTable: "projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_design_layer_links_content_items_ContentItemId",
                table: "design_layer_links",
                column: "ContentItemId",
                principalTable: "content_items",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_design_layer_links_projects_ProjectId",
                table: "design_layer_links",
                column: "ProjectId",
                principalTable: "projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_discussion_comments_discussion_threads_ThreadId",
                table: "discussion_comments",
                column: "ThreadId",
                principalTable: "discussion_threads",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_discussion_threads_content_items_ContentItemId",
                table: "discussion_threads",
                column: "ContentItemId",
                principalTable: "content_items",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_external_review_links_content_items_ContentItemId",
                table: "external_review_links",
                column: "ContentItemId",
                principalTable: "content_items",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_plugin_sync_conflicts_design_layer_links_DesignLayerLinkId",
                table: "plugin_sync_conflicts",
                column: "DesignLayerLinkId",
                principalTable: "design_layer_links",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_project_audit_logs_projects_ProjectId",
                table: "project_audit_logs",
                column: "ProjectId",
                principalTable: "projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_project_key_conventions_projects_ProjectId",
                table: "project_key_conventions",
                column: "ProjectId",
                principalTable: "projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_project_languages_projects_ProjectId",
                table: "project_languages",
                column: "ProjectId",
                principalTable: "projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_projects_workspaces_WorkspaceId",
                table: "projects",
                column: "WorkspaceId",
                principalTable: "workspaces",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_usage_references_content_items_ContentItemId",
                table: "usage_references",
                column: "ContentItemId",
                principalTable: "content_items",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_webhook_delivery_logs_webhook_subscriptions_SubscriptionId",
                table: "webhook_delivery_logs",
                column: "SubscriptionId",
                principalTable: "webhook_subscriptions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_webhook_subscriptions_projects_ProjectId",
                table: "webhook_subscriptions",
                column: "ProjectId",
                principalTable: "projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_workspace_invites_workspaces_WorkspaceId",
                table: "workspace_invites",
                column: "WorkspaceId",
                principalTable: "workspaces",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_workspace_memberships_workspaces_WorkspaceId",
                table: "workspace_memberships",
                column: "WorkspaceId",
                principalTable: "workspaces",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_content_item_language_tasks_content_items_ContentItemId",
                table: "content_item_language_tasks");

            migrationBuilder.DropForeignKey(
                name: "FK_content_item_revisions_content_items_ContentItemId",
                table: "content_item_revisions");

            migrationBuilder.DropForeignKey(
                name: "FK_content_items_copy_components_CopyComponentId",
                table: "content_items");

            migrationBuilder.DropForeignKey(
                name: "FK_content_items_projects_ProjectId",
                table: "content_items");

            migrationBuilder.DropForeignKey(
                name: "FK_copy_components_projects_ProjectId",
                table: "copy_components");

            migrationBuilder.DropForeignKey(
                name: "FK_design_layer_links_content_items_ContentItemId",
                table: "design_layer_links");

            migrationBuilder.DropForeignKey(
                name: "FK_design_layer_links_projects_ProjectId",
                table: "design_layer_links");

            migrationBuilder.DropForeignKey(
                name: "FK_discussion_comments_discussion_threads_ThreadId",
                table: "discussion_comments");

            migrationBuilder.DropForeignKey(
                name: "FK_discussion_threads_content_items_ContentItemId",
                table: "discussion_threads");

            migrationBuilder.DropForeignKey(
                name: "FK_external_review_links_content_items_ContentItemId",
                table: "external_review_links");

            migrationBuilder.DropForeignKey(
                name: "FK_plugin_sync_conflicts_design_layer_links_DesignLayerLinkId",
                table: "plugin_sync_conflicts");

            migrationBuilder.DropForeignKey(
                name: "FK_project_audit_logs_projects_ProjectId",
                table: "project_audit_logs");

            migrationBuilder.DropForeignKey(
                name: "FK_project_key_conventions_projects_ProjectId",
                table: "project_key_conventions");

            migrationBuilder.DropForeignKey(
                name: "FK_project_languages_projects_ProjectId",
                table: "project_languages");

            migrationBuilder.DropForeignKey(
                name: "FK_projects_workspaces_WorkspaceId",
                table: "projects");

            migrationBuilder.DropForeignKey(
                name: "FK_usage_references_content_items_ContentItemId",
                table: "usage_references");

            migrationBuilder.DropForeignKey(
                name: "FK_webhook_delivery_logs_webhook_subscriptions_SubscriptionId",
                table: "webhook_delivery_logs");

            migrationBuilder.DropForeignKey(
                name: "FK_webhook_subscriptions_projects_ProjectId",
                table: "webhook_subscriptions");

            migrationBuilder.DropForeignKey(
                name: "FK_workspace_invites_workspaces_WorkspaceId",
                table: "workspace_invites");

            migrationBuilder.DropForeignKey(
                name: "FK_workspace_memberships_workspaces_WorkspaceId",
                table: "workspace_memberships");
        }
    }
}

