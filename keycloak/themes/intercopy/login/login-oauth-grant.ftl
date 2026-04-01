<#import "template.ftl" as layout>
<@layout.registrationLayout bodyClass="oauth"; section>
  <#if section = "header">
    InterCopy
  <#elseif section = "form">
    <div class="InterCopy-form">
      <h2 class="InterCopy-heading">
        <#if client.name?has_content>
          Grant Access to ${advancedMsg(client.name)}
        <#else>
          Grant Access to ${client.clientId}
        </#if>
      </h2>

      <#if message?has_content>
        <div class="InterCopy-alert InterCopy-alert-${message.type}">${kcSanitize(message.summary)?no_esc}</div>
      </#if>

      <p class="InterCopy-hint">${msg("oauthGrantRequest")}</p>

      <ul class="InterCopy-scope-list">
        <#if oauth.clientScopesRequested??>
          <#list oauth.clientScopesRequested as clientScope>
            <li class="InterCopy-scope-item">
              <svg width="16" height="16" viewBox="0 0 16 16" fill="none" style="flex-shrink:0; margin-top:2px;">
                <circle cx="8" cy="8" r="7" stroke="currentColor" stroke-width="1.5" opacity="0.4"/>
                <path d="M5 8l2 2 4-4" stroke="var(--color-primary-500)" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"/>
              </svg>
              <span>
                <#if !clientScope.dynamicScopeParameter??>
                  ${advancedMsg(clientScope.consentScreenText)}
                <#else>
                  ${advancedMsg(clientScope.consentScreenText)}: <strong>${clientScope.dynamicScopeParameter}</strong>
                </#if>
              </span>
            </li>
          </#list>
        </#if>
      </ul>

      <#if client.attributes.policyUri?? || client.attributes.tosUri??>
        <div class="InterCopy-legal-links">
          <#if client.attributes.tosUri??>
            <a href="${client.attributes.tosUri}" target="_blank">Terms of Service</a>
          </#if>
          <#if client.attributes.policyUri??>
            <a href="${client.attributes.policyUri}" target="_blank">Privacy Policy</a>
          </#if>
        </div>
      </#if>

      <form action="${url.oauthAction}" method="POST">
        <input type="hidden" name="code" value="${oauth.code}">
        <div class="InterCopy-grant-buttons">
          <input class="pf-c-button pf-m-primary InterCopy-grant-btn" name="accept" id="kc-login" type="submit" value="Yes"/>
          <input class="pf-c-button InterCopy-grant-btn InterCopy-grant-btn-secondary" name="cancel" id="kc-cancel" type="submit" value="No"/>
        </div>
      </form>
    </div>

    <script>
      (function() {
        if (window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches) {
          document.documentElement.setAttribute('data-theme', 'dark');
        }
        var params = new URLSearchParams(window.location.search);
        var uiTheme = params.get('ui_theme');
        if (uiTheme === 'light' || uiTheme === 'dark') {
          document.documentElement.setAttribute('data-theme', uiTheme);
        }
      })();
    </script>
  </#if>
</@layout.registrationLayout>
