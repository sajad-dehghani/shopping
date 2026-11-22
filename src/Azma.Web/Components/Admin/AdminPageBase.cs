using Azma.Application.Abstractions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace Azma.Web.Components.Admin;

/// <summary>Base class for admin pages: current admin identity and a simple toast notification.</summary>
public abstract class AdminPageBase : ComponentBase
{
    [CascadingParameter] protected Task<AuthenticationState> AuthenticationState { get; set; } = default!;

    protected string? ToastMessage { get; private set; }
    protected bool ToastIsError { get; private set; }
    protected bool Busy { get; set; }

    protected async Task<AdminActor> GetActorAsync()
    {
        var user = (await AuthenticationState).User;
        return new AdminActor(user.FindFirstValue(ClaimTypes.NameIdentifier), user.Identity?.Name);
    }

    protected void ShowOk(string message) { ToastMessage = message; ToastIsError = false; }
    protected void ShowError(string? message) { ToastMessage = message ?? "خطای نامشخص"; ToastIsError = true; }
    protected void ClearToast() => ToastMessage = null;
}
