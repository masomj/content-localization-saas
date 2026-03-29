<script setup lang="ts">
definePageMeta({ layout: 'docs' })

useSeoMeta({
  title: 'Admin - InterCopy Docs',
  description: 'Admin features: project management, RBAC, and team member management.',
})
</script>

<template>
  <div>
    <h1>Admin</h1>
    <p>
      Organisation administrators have access to additional features for managing projects, team members, and
      permissions.
    </p>

    <h2>Project management</h2>
    <p>
      Admins can create, rename, and archive projects from the <strong>Projects</strong> page. Each project has
      its own set of languages, content keys, and version history.
    </p>
    <ul>
      <li><strong>Create project</strong> &mdash; Set a name and source language.</li>
      <li><strong>Archive project</strong> &mdash; Hide from the sidebar without deleting data.</li>
      <li><strong>Delete project</strong> &mdash; Permanently remove all content. This cannot be undone.</li>
    </ul>

    <h2>Role-based access control (RBAC)</h2>
    <p>
      InterCopy uses a role-based permission model. Each team member is assigned one of the following roles:
    </p>
    <table>
      <thead>
        <tr>
          <th>Role</th>
          <th>Permissions</th>
        </tr>
      </thead>
      <tbody>
        <tr>
          <td><strong>Owner</strong></td>
          <td>Full access. Can manage billing, delete the organisation, and assign any role.</td>
        </tr>
        <tr>
          <td><strong>Admin</strong></td>
          <td>Can manage projects, members, and settings. Cannot delete the organisation.</td>
        </tr>
        <tr>
          <td><strong>Translator</strong></td>
          <td>Can view content and edit translations in assigned languages. Cannot manage projects or members.</td>
        </tr>
        <tr>
          <td><strong>Reviewer</strong></td>
          <td>Can review translation batches and approve or reject. Cannot edit translations directly.</td>
        </tr>
        <tr>
          <td><strong>Viewer</strong></td>
          <td>Read-only access. Useful for stakeholders who need visibility without edit privileges.</td>
        </tr>
      </tbody>
    </table>

    <h2>Managing team members</h2>
    <p>
      Navigate to <strong>Settings &gt; Team Members</strong> to view and manage your organisation's members.
    </p>
    <ul>
      <li><strong>Invite</strong> &mdash; Send an email invitation with a role assignment.</li>
      <li><strong>Change role</strong> &mdash; Update a member's role from the members table.</li>
      <li><strong>Remove</strong> &mdash; Revoke access immediately.</li>
    </ul>
    <p>
      An audit log tracks all membership changes (invites, role changes, removals) for compliance purposes.
    </p>

    <div class="docs-next-page">
      <NuxtLink to="/docs/figma-plugin">Figma Plugin &rarr;</NuxtLink>
    </div>
  </div>
</template>
