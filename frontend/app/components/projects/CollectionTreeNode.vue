<script setup lang="ts">
type Node = { id: string; name: string; isRoot: boolean; children: Node[] }

const props = defineProps<{ node: Node; draggingId: string }>()
const emit = defineEmits<{
  rename: [nodeId: string]
  drop: [payload: { parentId: string; index: number }]
  dragstart: [nodeId: string]
}>()
</script>

<template>
  <li>
    <div class="tree-node" :class="{ 'root-node': props.node.isRoot }">
      <span>{{ props.node.name }}</span>
      <button v-if="!props.node.isRoot" class="inline-action" @click="emit('rename', props.node.id)">Rename</button>
    </div>

    <ul>
      <li
        v-for="(child, index) in props.node.children"
        :key="child.id"
        class="draggable-item"
        draggable="true"
        @dragstart="emit('dragstart', child.id)"
        @dragover.prevent
        @drop.prevent="emit('drop', { parentId: props.node.id, index })"
      >
        <CollectionTreeNode :node="child" :dragging-id="props.draggingId" @rename="emit('rename', $event)" @drop="emit('drop', $event)" @dragstart="emit('dragstart', $event)" />
      </li>
      <li class="drop-zone" @dragover.prevent @drop.prevent="emit('drop', { parentId: props.node.id, index: props.node.children.length })">Drop here to append</li>
    </ul>
  </li>
</template>

<style scoped>
.tree-node { display: flex; align-items: center; justify-content: space-between; border: 1px solid var(--color-border); border-radius: var(--radius-md); padding: var(--spacing-2) var(--spacing-3); margin-bottom: var(--spacing-2); }
.root-node { font-weight: 600; background: var(--color-primary-50); }
.drop-zone { border: 1px dashed var(--color-border); border-radius: var(--radius-md); padding: var(--spacing-2); color: var(--color-text-muted); margin-bottom: var(--spacing-2); }
.inline-action { border: 0; background: transparent; color: var(--color-primary-700); cursor: pointer; }
ul { list-style: none; margin: 0; padding-left: var(--spacing-4); }
</style>
