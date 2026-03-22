<#import "template.ftl" as layout>
<@layout.registrationLayout displayMessage=!messagesPerField.existsError('firstName','lastName','email','username','password','password-confirm') displayInfo=true; section>
  <#if section = "header">
    Create your LocFlow account
  <#elseif section = "form">
    <form id="kc-register-form" class="locflow-form" action="${url.registrationAction}" method="post">
      <div class="locflow-form-group">
        <label for="email" class="locflow-label">Email</label>
        <input type="email" id="email" class="pf-c-form-control" name="email" value="${(register.formData.email!'')}" autocomplete="email" required />
      </div>

      <#if !realm.registrationEmailAsUsername>
        <div class="locflow-form-group">
          <label for="username" class="locflow-label">Username</label>
          <input type="text" id="username" class="pf-c-form-control" name="username" value="${(register.formData.username!'')}" autocomplete="username" required />
        </div>
      </#if>

      <div class="locflow-form-group">
        <label for="password" class="locflow-label">Password</label>
        <input type="password" id="password" class="pf-c-form-control" name="password" autocomplete="new-password" required />
      </div>

      <div class="locflow-form-group">
        <label for="password-confirm" class="locflow-label">Confirm password</label>
        <input type="password" id="password-confirm" class="pf-c-form-control" name="password-confirm" autocomplete="new-password" required />
      </div>

      <div class="locflow-form-group">
        <input class="pf-c-button pf-m-primary pf-m-block" id="kc-register" type="submit" value="Create account" />
      </div>
    </form>
  <#elseif section = "info">
    <div class="locflow-register-cta">
      <span>Already have an account?</span>
      <a href="${url.loginUrl}">Back to sign in</a>
    </div>
  </#if>
</@layout.registrationLayout>
