<#import "template.ftl" as layout>
<@layout.registrationLayout displayMessage=!messagesPerField.existsError('username','password') displayInfo=realm.password && realm.registrationAllowed && !registrationDisabled??; section>
  <#if section = "header">
    InterCopy
  <#elseif section = "form">
    <#if message?has_content>
      <div class="InterCopy-alert InterCopy-alert-${message.type}">${kcSanitize(message.summary)?no_esc}</div>
    </#if>
    <form id="kc-form-login" class="InterCopy-form" onsubmit="login.disabled = true; return true;" action="${url.loginAction}" method="post">
      <div class="InterCopy-form-group">
        <label for="username" class="InterCopy-label">Email</label>
        <input tabindex="1" id="username" class="pf-c-form-control" name="username" value="${(login.username!'')}" type="text" autofocus autocomplete="email"/>
      </div>

      <div class="InterCopy-form-group">
        <label for="password" class="InterCopy-label">Password</label>
        <div class="InterCopy-password-row">
          <input tabindex="2" id="password" class="pf-c-form-control" name="password" type="password" autocomplete="current-password"/>
          <button type="button" class="InterCopy-password-toggle" data-toggle-password="password" aria-label="Show password">Show</button>
        </div>
      </div>

      <#if realm.resetPasswordAllowed>
        <div class="InterCopy-form-options">
          <a href="${url.loginResetCredentialsUrl}">Forgot password?</a>
        </div>
      </#if>

      <div class="InterCopy-form-group">
        <input tabindex="4" class="pf-c-button pf-m-primary pf-m-block" name="login" id="kc-login" type="submit" value="Sign in"/>
      </div>
    </form>
    <script>
      const params = new URLSearchParams(window.location.search)
      const uiTheme = params.get('ui_theme')
      if (uiTheme === 'light' || uiTheme === 'dark') {
        document.documentElement.setAttribute('data-theme', uiTheme)
      }

      document.querySelectorAll('a[href]').forEach((link) => {
        if (!uiTheme) return
        try {
          const u = new URL(link.getAttribute('href'), window.location.origin)
          u.searchParams.set('ui_theme', uiTheme)
          link.setAttribute('href', u.pathname + u.search + u.hash)
        } catch {}
      })

      document.querySelectorAll('[data-toggle-password]').forEach((btn) => {
        btn.addEventListener('click', () => {
          const input = document.getElementById(btn.getAttribute('data-toggle-password'))
          if (!input) return
          const showing = input.type === 'text'
          input.type = showing ? 'password' : 'text'
          btn.textContent = showing ? 'Show' : 'Hide'
          btn.setAttribute('aria-label', showing ? 'Show password' : 'Hide password')
        })
      })
    </script>
  <#elseif section = "info">
    <#if realm.password && realm.registrationAllowed && !registrationDisabled??>
      <div id="kc-registration" class="InterCopy-register-cta">
        <span>New to InterCopy?</span>
        <a tabindex="6" href="${url.registrationUrl}">Create an account</a>
      </div>
    </#if>
  </#if>
</@layout.registrationLayout>
