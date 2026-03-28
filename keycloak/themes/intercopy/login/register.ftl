<#import "template.ftl" as layout>
<@layout.registrationLayout displayMessage=!messagesPerField.existsError('firstName','lastName','email','username','password','password-confirm') displayInfo=true; section>
  <#if section = "header">
    Create your InterCopy account
  <#elseif section = "form">
    <#if message?has_content>
      <div class="InterCopy-alert InterCopy-alert-${message.type}">${kcSanitize(message.summary)?no_esc}</div>
    </#if>
    <form id="kc-register-form" class="InterCopy-form" action="${url.registrationAction}" method="post">
      <div class="InterCopy-form-group">
        <label for="firstName" class="InterCopy-label">First name</label>
        <input type="text" id="firstName" class="pf-c-form-control" name="firstName" value="${(register.formData.firstName!'')}" autocomplete="given-name" required />
      </div>

      <div class="InterCopy-form-group">
        <label for="lastName" class="InterCopy-label">Last name</label>
        <input type="text" id="lastName" class="pf-c-form-control" name="lastName" value="${(register.formData.lastName!'')}" autocomplete="family-name" required />
      </div>

      <div class="InterCopy-form-group">
        <label for="email" class="InterCopy-label">Email</label>
        <input type="email" id="email" class="pf-c-form-control" name="email" value="${(register.formData.email!'')}" autocomplete="email" required />
      </div>

      <input type="hidden" id="username" name="username" value="${(register.formData.username!'')}" />

      <div class="InterCopy-form-group">
        <label for="password" class="InterCopy-label">Password</label>
        <div class="InterCopy-password-row">
          <input type="password" id="password" class="pf-c-form-control" name="password" autocomplete="new-password" required />
          <button type="button" class="InterCopy-password-toggle" data-toggle-password="password" aria-label="Show password">Show</button>
        </div>
      </div>

      <div class="InterCopy-form-group">
        <label for="password-confirm" class="InterCopy-label">Confirm password</label>
        <div class="InterCopy-password-row">
          <input type="password" id="password-confirm" class="pf-c-form-control" name="password-confirm" autocomplete="new-password" required />
          <button type="button" class="InterCopy-password-toggle" data-toggle-password="password-confirm" aria-label="Show confirm password">Show</button>
        </div>
      </div>

      <div class="InterCopy-form-group">
        <input class="pf-c-button pf-m-primary pf-m-block" id="kc-register" type="submit" value="Create account" />
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

      const emailInput = document.getElementById('email')
      const usernameInput = document.getElementById('username')
      const syncUsername = () => {
        if (emailInput && usernameInput) usernameInput.value = emailInput.value || ''
      }
      if (emailInput) {
        emailInput.addEventListener('input', syncUsername)
        syncUsername()
      }

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
    <div class="InterCopy-register-cta">
      <span>Already have an account?</span>
      <a href="${url.loginUrl}">Back to sign in</a>
    </div>
  </#if>
</@layout.registrationLayout>
