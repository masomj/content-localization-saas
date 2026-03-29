<script setup lang="ts">
definePageMeta({ layout: 'docs' })

useSeoMeta({
  title: 'Figma Plugin Authentication - InterCopy Docs',
  description: 'Device auth flow for the InterCopy Figma plugin.',
})
</script>

<template>
  <div>
    <h1>Authentication</h1>
    <p>
      The Figma plugin uses a <strong>device authorisation flow</strong> to connect your Figma session to your
      InterCopy account. This avoids entering passwords inside the plugin and works with SSO providers.
    </p>

    <h2>How it works</h2>
    <ol>
      <li>Open the InterCopy plugin in Figma and click <strong>Sign In</strong>.</li>
      <li>The plugin displays a short <strong>device code</strong> and a URL.</li>
      <li>Open the URL in your browser (or scan the QR code).</li>
      <li>Log in to InterCopy if you are not already signed in.</li>
      <li>Enter the device code to confirm the link.</li>
      <li>The plugin detects the successful authorisation and logs you in.</li>
    </ol>

    <h2>Token management</h2>
    <p>
      After authorisation, the plugin stores an access token locally in Figma's <code>clientStorage</code>. The token
      is refreshed automatically. You stay signed in until you explicitly log out or the token expires (30 days of
      inactivity).
    </p>

    <h2>Signing out</h2>
    <p>
      Open the plugin, click the user menu in the top-right corner, and select <strong>Sign Out</strong>. This
      removes the stored token.
    </p>

    <h2>Troubleshooting</h2>
    <ul>
      <li><strong>"Code expired"</strong> &mdash; Device codes are valid for 10 minutes. If it expires, click <strong>Sign In</strong> again to generate a new code.</li>
      <li><strong>"Unauthorised"</strong> &mdash; Your token may have expired. Sign out and sign in again.</li>
      <li><strong>SSO redirect loop</strong> &mdash; Make sure third-party cookies are enabled in your browser, or use the Figma desktop app.</li>
    </ul>

    <div class="docs-next-page">
      <NuxtLink to="/docs/figma-plugin/sync">Push / Pull Sync &rarr;</NuxtLink>
    </div>
  </div>
</template>
