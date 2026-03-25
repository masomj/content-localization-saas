<script setup lang="ts">
import type { ProjectTreeNode } from '~/api/types'

const props = defineProps<{
  node: ProjectTreeNode
  draggingId: string
  depth?: number
}>()

const emit = defineEmits<{
  rename: [nodeId: string]
  delete: [nodeId: string]
  newFolder: [parentId: string]
  newContentKey: [parentId: string]
  drop: [payload: { targetId: string | null; index: number; nodeType: 'folder' | 'contentKey' }]
  dragstart: [payload: { nodeId: string; nodeType: 'folder' | 'contentKey' }]
  select: [nodeId: string]
}>()

const expanded = ref(true)
const showContextMenu = ref(false)
const contextMenuEl = ref<HTMLElement | null>(null)
const nodeEl = ref<HTMLElement | null>(null)
const currentDepth = computed(() => props.depth ?? props.node.depth)
const isFolder = computed(() => props.node.nodeType === 'folder')
const isContentKey = computed(() => props.node.nodeType === 'contentKey')
const isDragOver = ref(false)

function toggleExpand() {
  if (isFolder.value) expanded.value = !expanded.value
}

function onContextMenu(e: MouseEvent) {
  e.preventDefault()
  showContextMenu.value = !showContextMenu.value
}

function closeContextMenu() {
  showContextMenu.value = false
}

function handleRename() {
  closeContextMenu()
  emit('rename', props.node.id)
}

function handleDelete() {
  closeContextMenu()
  emit('delete', props.node.id)
}

function handleNewFolder() {
  closeContextMenu()
  emit('newFolder', props.node.id)
}

function handleNewContentKey() {
  closeContextMenu()
  emit('newContentKey', props.node.id)
}

function onDragStart(e: DragEvent) {
  if (!e.dataTransfer) return
  e.dataTransfer.effectAllowed = 'move'
  e.dataTransfer.setData('text/plain', props.node.id)
  emit('dragstart', { nodeId: props.node.id, nodeType: props.node.nodeType })
}

function onDragOver(e: DragEvent) {
  if (!isFolder.value) return
  if (props.draggingId === props.node.id) return
  e.preventDefault()
  isDragOver.value = true
}

function onDragLeave() {
  isDragOver.value = false
}

function onDrop(e: DragEvent) {
  e.preventDefault()
  isDragOver.value = false
  if (!isFolder.value) return
  if (props.draggingId === props.node.id) return
  emit('drop', { targetId: props.node.id, index: props.node.children.length, nodeType: 'folder' })
}

function onKeydown(e: KeyboardEvent) {
  if (e.key === 'Enter' || e.key === ' ') {
    e.preventDefault()
    if (isFolder.value) {
      toggleExpand()
    } else {
      emit('select', props.node.id)
    }
  }
  if (e.key === 'ArrowRight' && isFolder.value && !expanded.value) {
    e.preventDefault()
    expanded.value = true
  }
  if (e.key === 'ArrowLeft' && isFolder.value && expanded.value) {
    e.preventDefault()
    expanded.value = false
  }
  if (e.key === 'ContextMenu' || (e.shiftKey && e.key === 'F10')) {
    e.preventDefault()
    showContextMenu.value = !showContextMenu.value
  }
}

function onClickOutside(e: MouseEvent) {
  if (contextMenuEl.value && !contextMenuEl.value.contains(e.target as Node)) {
    closeContextMenu()
  }
}

watch(showContextMenu, (open) => {
  if (open) {
    document.addEventListener('click', onClickOutside, { capture: true })
  } else {
    document.removeEventListener('click', onClickOutside, { capture: true })
  }
})

onUnmounted(() => {
  document.removeEventListener('click', onClickOutside, { capture: true })
})
</script>

<template>
  <li
    :role="isFolder ? 'treeitem' : 'none'"
    :aria-expanded="isFolder ? expanded : undefined"
  >
    <div
      ref="nodeEl"
      class="tree-node"
      :class="{
        'tree-node--folder': isFolder,
        'tree-node--content-key': isContentKey,
        'tree-node--drag-over': isDragOver,
      }"
      :style="{ paddingLeft: `calc(var(--spacing-3) + ${currentDepth * 16}px)` }"
      :draggable="!props.node.children?.length || isContentKey ? 'true' : 'false'"
      :tabindex="0"
      :role="isContentKey ? 'treeitem' : undefined"
      :aria-label="`${isFolder ? 'Folder' : 'Content key'}: ${props.node.name}`"
      @dragstart="onDragStart"
      @dragover="onDragOver"
      @dragleave="onDragLeave"
      @drop="onDrop"
      @keydown="onKeydown"
      @contextmenu="onContextMenu"
    >
      <div class="tree-node__leading">
        <button
          v-if="isFolder"
          class="tree-node__expand"
          :aria-label="expanded ? 'Collapse folder' : 'Expand folder'"
          tabindex="-1"
          @click.stop="toggleExpand"
        >
          <svg
            class="tree-node__chevron"
            :class="{ 'tree-node__chevron--expanded': expanded }"
            width="16" height="16" viewBox="0 0 16 16" fill="none"
            aria-hidden="true"
          >
            <path d="M6 4l4 4-4 4" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round" />
          </svg>
        </button>

        <svg
          v-if="isFolder"
          class="tree-node__icon tree-node__icon--folder"
          width="16" height="16" viewBox="0 0 16 16" fill="none"
          aria-hidden="true"
        >
          <path d="M2 4a1 1 0 011-1h3.586a1 1 0 01.707.293L8 4h5a1 1 0 011 1v7a1 1 0 01-1 1H3a1 1 0 01-1-1V4z" stroke="currentColor" stroke-width="1.2" />
        </svg>

        <svg
          v-if="isContentKey"
          class="tree-node__icon tree-node__icon--key"
          width="16" height="16" viewBox="0 0 16 16" fill="none"
          aria-hidden="true"
        >
          <path d="M10 2v4h4M10 2H4a1 1 0 00-1 1v10a1 1 0 001 1h8a1 1 0 001-1V6l-3-4z" stroke="currentColor" stroke-width="1.2" stroke-linecap="round" stroke-linejoin="round" />
          <path d="M5 9h6M5 11.5h4" stroke="currentColor" stroke-width="1.2" stroke-linecap="round" />
        </svg>

        <span class="tree-node__name">{{ props.node.name }}</span>

        <span v-if="isContentKey && props.node.status" class="tree-node__badge">
          {{ props.node.status }}
        </span>
      </div>

      <button
        class="tree-node__menu-trigger"
        :aria-label="`Actions for ${props.node.name}`"
        tabindex="-1"
        @click.stop="showContextMenu = !showContextMenu"
      >
        <svg width="16" height="16" viewBox="0 0 16 16" fill="none" aria-hidden="true">
          <circle cx="8" cy="3.5" r="1" fill="currentColor" />
          <circle cx="8" cy="8" r="1" fill="currentColor" />
          <circle cx="8" cy="12.5" r="1" fill="currentColor" />
        </svg>
      </button>

      <div
        v-if="showContextMenu"
        ref="contextMenuEl"
        class="context-menu"
        role="menu"
        :aria-label="`Context menu for ${props.node.name}`"
      >
        <template v-if="isFolder">
          <button class="context-menu__item" role="menuitem" @click="handleNewFolder">
            New Folder
          </button>
          <button class="context-menu__item" role="menuitem" @click="handleNewContentKey">
            New Content Key
          </button>
          <hr class="context-menu__separator" />
          <button class="context-menu__item" role="menuitem" @click="handleRename">
            Rename
          </button>
          <button class="context-menu__item context-menu__item--danger" role="menuitem" @click="handleDelete">
            Delete
          </button>
        </template>
        <template v-if="isContentKey">
          <button class="context-menu__item" role="menuitem" @click="handleRename">
            Rename
          </button>
          <button class="context-menu__item context-menu__item--danger" role="menuitem" @click="handleDelete">
            Delete
          </button>
        </template>
      </div>
    </div>

    <ul
      v-if="isFolder && expanded && props.node.children.length > 0"
      role="group"
      class="tree-node__children"
    >
      <CollectionTreeNode
        v-for="child in props.node.children"
        :key="child.id"
        :node="child"
        :dragging-id="props.draggingId"
        :depth="currentDepth + 1"
        @rename="emit('rename', $event)"
        @delete="emit('delete', $event)"
        @new-folder="emit('newFolder', $event)"
        @new-content-key="emit('newContentKey', $event)"
        @drop="emit('drop', $event)"
        @dragstart="emit('dragstart', $event)"
        @select="emit('select', $event)"
      />
    </ul>

    <div
      v-if="isFolder && expanded"
      class="tree-node__drop-zone"
      :style="{ marginLeft: `calc(var(--spacing-4) + ${(currentDepth + 1) * 16}px)` }"
      @dragover.prevent
      @drop.prevent="emit('drop', { targetId: props.node.id, index: props.node.children.length, nodeType: 'folder' })"
    >
      Drop here
    </div>
  </li>
</template>

<style scoped>
li {
  list-style: none;
}

.tree-node {
  position: relative;
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: var(--spacing-1) var(--spacing-3);
  border-radius: var(--radius-md);
  margin-bottom: 1px;
  cursor: default;
  transition: background var(--transition-fast);
}

.tree-node:hover {
  background: var(--color-gray-100);
}

.tree-node:focus-visible {
  outline: 2px solid var(--color-primary-600);
  outline-offset: -2px;
}

.tree-node--drag-over {
  background: var(--color-primary-50);
  outline: 2px dashed var(--color-primary-400);
  outline-offset: -2px;
}

.tree-node__leading {
  display: flex;
  align-items: center;
  gap: var(--spacing-1);
  min-width: 0;
}

.tree-node__expand {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 20px;
  height: 20px;
  padding: 0;
  border: none;
  background: transparent;
  color: var(--color-text-muted);
  cursor: pointer;
  border-radius: var(--radius-sm);
  flex-shrink: 0;
}

.tree-node__expand:hover {
  background: var(--color-gray-200);
  color: var(--color-text-primary);
}

.tree-node__chevron {
  transition: transform var(--transition-fast);
}

.tree-node__chevron--expanded {
  transform: rotate(90deg);
}

.tree-node__icon {
  flex-shrink: 0;
  color: var(--color-text-muted);
}

.tree-node__icon--folder {
  color: var(--color-primary-600);
}

.tree-node__icon--key {
  color: var(--color-text-secondary);
}

.tree-node__name {
  font-size: var(--font-size-sm);
  color: var(--color-text-primary);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.tree-node--content-key .tree-node__name {
  font-family: var(--font-family-mono, monospace);
}

.tree-node__badge {
  font-size: var(--font-size-xs);
  color: var(--color-text-muted);
  background: var(--color-gray-100);
  padding: 0 var(--spacing-1);
  border-radius: var(--radius-sm);
  flex-shrink: 0;
}

.tree-node__menu-trigger {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 24px;
  height: 24px;
  padding: 0;
  border: none;
  background: transparent;
  color: var(--color-text-muted);
  cursor: pointer;
  border-radius: var(--radius-sm);
  opacity: 0;
  flex-shrink: 0;
  transition: opacity var(--transition-fast);
}

.tree-node:hover .tree-node__menu-trigger,
.tree-node:focus-within .tree-node__menu-trigger {
  opacity: 1;
}

.tree-node__menu-trigger:hover {
  background: var(--color-gray-200);
  color: var(--color-text-primary);
}

.context-menu {
  position: absolute;
  top: 100%;
  right: 0;
  z-index: var(--z-dropdown);
  min-width: 160px;
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  padding: var(--spacing-1) 0;
  box-shadow: var(--shadow-lg);
}

.context-menu__item {
  display: block;
  width: 100%;
  padding: var(--spacing-2) var(--spacing-3);
  border: none;
  background: transparent;
  color: var(--color-text-primary);
  font-size: var(--font-size-sm);
  text-align: left;
  cursor: pointer;
}

.context-menu__item:hover,
.context-menu__item:focus-visible {
  background: var(--color-gray-100);
}

.context-menu__item:focus-visible {
  outline: 2px solid var(--color-primary-600);
  outline-offset: -2px;
}

.context-menu__item--danger {
  color: var(--color-error);
}

.context-menu__separator {
  border: none;
  border-top: 1px solid var(--color-border);
  margin: var(--spacing-1) 0;
}

.tree-node__children {
  margin: 0;
  padding: 0;
}

.tree-node__drop-zone {
  border: 1px dashed var(--color-border);
  border-radius: var(--radius-sm);
  padding: var(--spacing-1) var(--spacing-2);
  margin-bottom: var(--spacing-1);
  color: var(--color-text-muted);
  font-size: var(--font-size-xs);
  text-align: center;
  opacity: 0;
  transition: opacity var(--transition-fast);
}

.tree-node__drop-zone:hover,
li:has(.tree-node--drag-over) > .tree-node__drop-zone {
  opacity: 1;
}
</style>
