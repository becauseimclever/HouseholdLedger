// <copyright file="SessionAccessNotice.razor.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Client.Components;

using HouseholdLedger.Client.State;
using Microsoft.AspNetCore.Components;

/// <summary>Offers full-document sign-in after session loss without discarding pending edits.</summary>
public partial class SessionAccessNotice : ComponentBase, IDisposable
{
    private bool disposed;

    [Inject]
    private SessionAccessState Access { get; set; } = null!;

    /// <inheritdoc/>
    public void Dispose()
    {
        this.disposed = true;
        this.Access.Changed -= this.OnAccessChanged;
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc/>
    protected override void OnInitialized() => this.Access.Changed += this.OnAccessChanged;

    private void OnAccessChanged()
    {
        _ = this.InvokeAsync(() =>
        {
            if (!this.disposed)
            {
                this.StateHasChanged();
            }
        });
    }
}
