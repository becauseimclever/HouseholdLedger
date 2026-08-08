// <copyright file="OpenSourceNoticesPage.razor.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Client.Pages;

using HouseholdLedger.Client.OpenSourceNotices;

using Microsoft.AspNetCore.Components;

/// <summary>
/// Presents source-controlled third-party notices included with the application.
/// </summary>
public partial class OpenSourceNoticesPage : ComponentBase
{
    private static IReadOnlyList<OpenSourceNoticeExclusion> Exclusions => OpenSourceNoticesManifest.Exclusions;

    private static IReadOnlyList<OpenSourceNotice> Notices => OpenSourceNoticesManifest.Notices;

    private static string ScopeStatement => OpenSourceNoticesManifest.ScopeStatement;

    private static string ExclusionKindLabel(OpenSourceNoticeExclusionKind kind) => kind switch
    {
        OpenSourceNoticeExclusionKind.ProprietaryProduct => "Proprietary HouseholdLedger product",
        OpenSourceNoticeExclusionKind.GeneralFrameworkDependency => "Microsoft platform/framework exclusion",
        _ => throw new InvalidOperationException("The exclusion kind is not supported."),
    };

    private static string PublicationFormLabel(OpenSourceNoticePublicationForm form) => form switch
    {
        OpenSourceNoticePublicationForm.StaticBundledAsset => "Static/bundled asset",
        OpenSourceNoticePublicationForm.RuntimeAssembly => "Runtime assembly",
        OpenSourceNoticePublicationForm.Both => "Both",
        _ => throw new InvalidOperationException("The publication form is not supported."),
    };
}
