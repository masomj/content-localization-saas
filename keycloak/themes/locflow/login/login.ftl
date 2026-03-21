<#import "template.ftl" as layout>
<@layout.registrationLayout displayMessage=!messagesPerField.existsError('username','password') displayInfo=realm.password && realm.registrationAllowed && !registrationDisabled??; section>
  <#if section = "header">
    LocFlow
  <#elseif section = "form">
    <form id="kc-form-login" onsubmit="login.disabled = true; return true;" action="${url.loginAction}" method="post">
      <div class="form-group">
        <label for="username" class="control-label">Email</label>
        <input tabindex="1" id="username" class="form-control" name="username" value="${(login.username!'')}" type="text" autofocus autocomplete="email"/>
      </div>

      <div class="form-group">
        <label for="password" class="control-label">Password</label>
        <input tabindex="2" id="password" class="form-control" name="password" type="password" autocomplete="current-password"/>
      </div>

      <div class="form-group">
        <input tabindex="4" class="btn btn-primary btn-block btn-lg" name="login" id="kc-login" type="submit" value="Sign in"/>
      </div>
    </form>
  </#if>
</@layout.registrationLayout>
