<template>
  <div>
    <a href="#main-content" class="skip-link">Skip to main content</a>
    <header class="site-header">
      <h1>Content Localization SaaS</h1>
    </header>
    <main id="main-content" tabindex="-1">
      <NuxtPage />
    </main>
  </div>
</template>

<style>
body { font-family: Inter, system-ui, Arial, sans-serif; margin: 0; }
.site-header { padding: 1rem; border-bottom: 1px solid #e5e7eb; }
main { padding: 1rem; max-width: 980px; margin: 0 auto; }
.skip-link {
  position: absolute; left: 0.5rem; top: -100px;
  background: #111827; color: white; padding: .5rem .75rem; border-radius: .375rem;
}
.skip-link:focus { top: 0.5rem; z-index: 1000; }
.card { border: 1px solid #e5e7eb; border-radius: .5rem; padding: 1rem; margin-bottom: 1rem; }
label { display:block; font-weight:600; margin-bottom:.25rem; }
input, select { width:100%; padding:.5rem; border:1px solid #d1d5db; border-radius:.375rem; }
button { padding: .55rem .85rem; border-radius: .375rem; border:1px solid #111827; background:#111827; color:white; }
.error { color: #b91c1c; font-size: .875rem; }
.grid { display:grid; gap:1rem; grid-template-columns:1fr; }
@media (min-width: 900px) { .grid { grid-template-columns: 1fr 1fr; } }
</style>
