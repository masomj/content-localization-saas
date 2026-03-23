<#import "template.ftl" as layout>
<@layout.registrationLayout displayMessage=!messagesPerField.existsError('username','password') displayInfo=realm.password && realm.registrationAllowed && !registrationDisabled??; section>
  <#if section = "header">
    LocFlow
  <#elseif section = "form">
    <form id="kc-form-login" class="locflow-form" onsubmit="login.disabled = true; return true;" action="${url.loginAction}" method="post">
      <div class="locflow-form-group">
        <label for="username" class="locflow-label">Email</label>
        <input tabindex="1" id="username" class="pf-c-form-control" name="username" value="${(login.username!'')}" type="text" autofocus autocomplete="email"/>
      </div>

      <div class="locflow-form-group">
        <label for="password" class="locflow-label">Password</label>
        <div class="locflow-password-row">
          <input tabindex="2" id="password" class="pf-c-form-control" name="password" type="password" autocomplete="current-password"/>
          <button type="button" class="locflow-password-toggle" data-toggle-password="password" aria-label="Show password">Show</button>
        </div>
      </div>

      <#if realm.resetPasswordAllowed>
        <div class="locflow-form-options">
          <a href="${url.loginResetCredentialsUrl}">Forgot password?</a>
        </div>
      </#if>

      <div class="locflow-form-group">
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
      <div id="kc-registration" class="locflow-register-cta">
        <span>New to LocFlow?</span>
        <a tabindex="6" href="${url.registrationUrl}">Create an account</a>
      </div>
    </#if>
  </#if>
</@layout.registrationLayout>
