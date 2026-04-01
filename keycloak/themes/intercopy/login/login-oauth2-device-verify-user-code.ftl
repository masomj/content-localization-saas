<#import "template.ftl" as layout>
<@layout.registrationLayout; section>
  <#if section = "header">
    InterCopy
  <#elseif section = "form">
    <div class="InterCopy-form">
      <h2 class="InterCopy-heading">Enter Device Code</h2>
      <p class="InterCopy-hint">Enter the code shown in your Figma plugin to connect your account.</p>

      <#if message?has_content>
        <div class="InterCopy-alert InterCopy-alert-${message.type}">${kcSanitize(message.summary)?no_esc}</div>
      </#if>

      <form id="kc-user-verify-device-user-code-form" action="${url.oauth2DeviceVerificationAction}" method="post">
        <div class="InterCopy-form-group">
          <label for="device-user-code" class="InterCopy-label">Device Code</label>
          <input id="device-user-code" name="device_user_code" autocomplete="off" type="text"
                 class="pf-c-form-control" autofocus
                 placeholder="e.g. ABCD-EFGH"
                 style="text-align:center; font-size:1.25rem; letter-spacing:0.15em; font-weight:600;" />
        </div>

        <div class="InterCopy-form-group">
          <input class="pf-c-button pf-m-primary pf-m-block" type="submit" value="Submit"/>
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
