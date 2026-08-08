// <copyright file="OpenSourceNoticesManifest.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Client.OpenSourceNotices;

using System;
using System.Collections.Generic;

#pragma warning disable SA1201
#pragma warning disable SA1402
#pragma warning disable SA1518

/// <summary>
/// Provides the source-controlled notices for third-party components confirmed
/// in the Client and hosted release publish outputs on 2026-08-07.
/// </summary>
public static class OpenSourceNoticesManifest
{
    /// <summary>
    /// Gets the statement rendered before the included-component notices.
    /// </summary>
    public const string ScopeStatement = "These notices cover third-party components confirmed in the published HouseholdLedger Client and hosted application. They do not list every package restored during development.";

    /// <summary>
    /// Gets the third-party package notices that the page must render.
    /// </summary>
    public static IReadOnlyList<OpenSourceNotice> Notices { get; } =
    [
        new(
            "Blazicons.Lucide",
            "3.0.8",
            OpenSourceNoticePublicationForm.RuntimeAssembly,
            "The Client and hosted publish outputs include the Blazicons.Lucide WebAssembly assembly; the hosted output also includes Blazicons.Lucide.dll.",
            new Uri("https://github.com/kyleherzog/Blazicons.Lucide"),
            [
                new(
                    "Blazicons.Lucide",
                    "MIT",
                    "Copyright (c) 2024 Kyle Herzog",
                    MitKyleHerzog2024,
                    new Uri("https://github.com/kyleherzog/Blazicons.Lucide/blob/0528196e60d2ba731470fa38992661afec432b06/LICENSE")),
                new(
                    "Lucide Icons",
                    "ISC",
                    "Copyright (c) 2026 Lucide Icons and Contributors",
                    LucideIsc,
                    new Uri("https://lucide.dev/license")),
                new(
                    "Feather-derived Lucide icons (conservative notice)",
                    "MIT",
                    "Copyright (c) 2013-present Cole Bemis",
                    FeatherMit,
                    new Uri("https://lucide.dev/license")),
            ],
            "The application uses PanelLeft and PanelRight through this package. The package does not declare an independently pinned Lucide source version or an icon-to-Feather derivation mapping, so the Feather MIT notice is included conservatively."),
        new(
            "Blazicons",
            "4.0.21",
            OpenSourceNoticePublicationForm.Both,
            "The Client and hosted publish outputs include the Blazicons WebAssembly assembly and Blazicons scoped and minified CSS assets; the hosted output also includes Blazicons.dll.",
            new Uri("https://github.com/kyleherzog/Blazicons"),
            [
                new(
                    "Blazicons",
                    "MIT",
                    "Copyright (c) 2022 Kyle Herzog",
                    MitKyleHerzog2022,
                    new Uri("https://github.com/kyleherzog/Blazicons/blob/5e55c733ca9302329007d49c33856152cb88a923/LICENSE")),
            ],
            "The resolved package does not bundle a license file; this notice is retained from its locked upstream source license."),
        new(
            "BlazorComponentUtilities",
            "1.8.0",
            OpenSourceNoticePublicationForm.RuntimeAssembly,
            "The Client and hosted publish outputs include the BlazorComponentUtilities WebAssembly assembly; the hosted output also includes BlazorComponentUtilities.dll.",
            new Uri("https://github.com/EdCharbeneau/BlazorComponentUtilities"),
            [
                new(
                    "BlazorComponentUtilities",
                    "MIT",
                    "Copyright (c) 2011-2019 Ed Charbeneau",
                    BlazorComponentUtilitiesMit,
                    new Uri("https://github.com/EdCharbeneau/BlazorComponentUtilities")),
            ],
            "This notice is copied from the LICENSE.txt file bundled in the resolved package."),
        new(
            "Npgsql.EntityFrameworkCore.PostgreSQL",
            "10.0.3",
            OpenSourceNoticePublicationForm.RuntimeAssembly,
            "The hosted publish output includes Npgsql.EntityFrameworkCore.PostgreSQL.dll.",
            new Uri("https://github.com/npgsql/efcore.pg"),
            [
                new(
                    "Npgsql Entity Framework Core Provider",
                    "PostgreSQL",
                    "Copyright 2025 (c) The Npgsql Development Team",
                    PostgreSqlLicense,
                    new Uri("https://www.postgresql.org/about/licence/")),
            ],
            "The locked NuGet.org package declares the PostgreSQL license, names The Npgsql Development Team in its copyright metadata, and identifies repository commit 5e912bf071068a44e5abbdab497b313ba99d3ee1. The resolved package does not bundle a license file; the canonical PostgreSQL license text is carried here."),
        new(
            "Npgsql",
            "10.0.3",
            OpenSourceNoticePublicationForm.RuntimeAssembly,
            "The hosted publish output includes Npgsql.dll.",
            new Uri("https://github.com/npgsql/npgsql"),
            [
                new(
                    "Npgsql .NET Data Provider",
                    "PostgreSQL",
                    "Copyright 2025 (c) The Npgsql Development Team",
                    PostgreSqlLicense,
                    new Uri("https://www.postgresql.org/about/licence/")),
            ],
            "The locked NuGet.org package declares the PostgreSQL license, names The Npgsql Development Team in its copyright metadata, and identifies repository commit d3768398c17877b3a916c3c4d87e8e11698991fc. The resolved package does not bundle a license file; the canonical PostgreSQL license text is carried here."),
    ];

    /// <summary>
    /// Gets shipped dependencies outside this third-party package notice set.
    /// </summary>
    public static IReadOnlyList<OpenSourceNoticeExclusion> Exclusions { get; } =
    [
        new(
            "HouseholdLedger assemblies",
            OpenSourceNoticeExclusionKind.ProprietaryProduct,
            "Application assemblies are proprietary HouseholdLedger product code, not third-party package notices."),
        new(
            "Microsoft platform and framework package assets",
            OpenSourceNoticeExclusionKind.GeneralFrameworkDependency,
            "The hosted publish output includes Microsoft.AspNetCore.OpenApi.dll, Microsoft.OpenApi.dll, Microsoft.EntityFrameworkCore.dll, Microsoft.EntityFrameworkCore.Abstractions.dll, and Microsoft.EntityFrameworkCore.Relational.dll. Dependency Governance classifies the Microsoft ASP.NET Core, EF Core, and OpenAPI.NET families as Microsoft platform/framework dependencies with direct-family provenance, MIT-license, and commercial-model review. They are not independently maintained third-party package notices in this manifest."),
    ];

    private const string MitKyleHerzog2024 = """
        MIT License

        Copyright (c) 2024 Kyle Herzog

        Permission is hereby granted, free of charge, to any person obtaining a copy
        of this software and associated documentation files (the "Software"), to deal
        in the Software without restriction, including without limitation the rights
        to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
        copies of the Software, and to permit persons to whom the Software is
        furnished to do so, subject to the following conditions:

        The above copyright notice and this permission notice shall be included in all
        copies or substantial portions of the Software.

        THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
        IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
        FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
        AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
        LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
        OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
        SOFTWARE.
        """;

    private const string PostgreSqlLicense = """
        PostgreSQL Database Management System
        (formerly known as Postgres, then as Postgres95)

        Portions Copyright (c) 1996-2026, The PostgreSQL Global Development Group

        Portions Copyright (c) 1994, The Regents of the University of California

        Permission to use, copy, modify, and distribute this software and its
        documentation for any purpose, without fee, and without a written agreement
        is hereby granted, provided that the above copyright notice and this paragraph
        and the following two paragraphs appear in all copies.

        IN NO EVENT SHALL THE UNIVERSITY OF CALIFORNIA BE LIABLE TO ANY PARTY FOR
        DIRECT, INDIRECT, SPECIAL, INCIDENTAL, OR CONSEQUENTIAL DAMAGES, INCLUDING LOST
        PROFITS, ARISING OUT OF THE USE OF THIS SOFTWARE AND ITS DOCUMENTATION, EVEN IF
        THE UNIVERSITY OF CALIFORNIA HAS BEEN ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

        THE UNIVERSITY OF CALIFORNIA SPECIFICALLY DISCLAIMS ANY WARRANTIES, INCLUDING,
        BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A
        PARTICULAR PURPOSE. THE SOFTWARE PROVIDED HEREUNDER IS ON AN "AS IS" BASIS, AND
        THE UNIVERSITY OF CALIFORNIA HAS NO OBLIGATIONS TO PROVIDE MAINTENANCE, SUPPORT,
        UPDATES, ENHANCEMENTS, OR MODIFICATIONS.
        """;

    private const string MitKyleHerzog2022 = """
        MIT License

        Copyright (c) 2022 Kyle Herzog

        Permission is hereby granted, free of charge, to any person obtaining a copy
        of this software and associated documentation files (the "Software"), to deal
        in the Software without restriction, including without limitation the rights
        to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
        copies of the Software, and to permit persons to whom the Software is
        furnished to do so, subject to the following conditions:

        The above copyright notice and this permission notice shall be included in all
        copies or substantial portions of the Software.

        THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
        IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
        FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
        AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
        LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
        OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
        SOFTWARE.
        """;

    private const string BlazorComponentUtilitiesMit = """
        The MIT License (MIT)

        Copyright (c) 2011-2019 Ed Charbeneau
        Copyright (c) 2011-2019 Ed Charbeneau

        Permission is hereby granted, free of charge, to any person obtaining a copy
        of this software and associated documentation files (the "Software"), to deal
        in the Software without restriction, including without limitation the rights
        to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
        copies of the Software, and to permit persons to whom the Software is
        furnished to do so, subject to the following conditions:

        The above copyright notice and this permission notice shall be included in
        all copies or substantial portions of the Software.

        THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
        IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
        FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
        AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
        LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
        OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
        THE SOFTWARE.
        """;

    private const string LucideIsc = """
        ISC License

        Copyright (c) 2026 Lucide Icons and Contributors

        Permission to use, copy, modify, and/or distribute this software for any purpose
        with or without fee is hereby granted, provided that the above copyright notice
        and this permission notice appear in all copies.

        THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES WITH
        REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF MERCHANTABILITY AND
        FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY SPECIAL, DIRECT, INDIRECT,
        OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES WHATSOEVER RESULTING FROM LOSS OF USE,
        DATA OR PROFITS, WHETHER IN AN ACTION OF CONTRACT, NEGLIGENCE OR OTHER TORTIOUS
        ACTION, ARISING OUT OF OR IN CONNECTION WITH THE USE OR PERFORMANCE OF THIS
        SOFTWARE.
        """;

    private const string FeatherMit = """
        The MIT License (MIT)

        Copyright (c) 2013-present Cole Bemis

        Permission is hereby granted, free of charge, to any person obtaining a copy
        of this software and associated documentation files (the "Software"), to deal
        in the Software without restriction, including without limitation the rights
        to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
        copies of the Software, and to permit persons to whom the Software is
        furnished to do so, subject to the following conditions:

        The above copyright notice and this permission notice shall be included in all
        copies or substantial portions of the Software.

        THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
        IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
        FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
        COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN
        ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
        SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
        """;
}

/// <summary>
/// Identifies how a component appears in the final published application.
/// </summary>
public enum OpenSourceNoticePublicationForm
{
    /// <summary>
    /// The component is delivered as a runtime assembly.
    /// </summary>
    RuntimeAssembly,

    /// <summary>
    /// The component is delivered as a static or bundled asset.
    /// </summary>
    StaticBundledAsset,

    /// <summary>
    /// The component is delivered in both forms.
    /// </summary>
    Both,
}

/// <summary>
/// Describes one confirmed third-party package and its required notices.
/// </summary>
/// <param name="PackageName">The resolved package identity.</param>
/// <param name="Version">The resolved package version.</param>
/// <param name="PublicationForm">How the package appears in the published application.</param>
/// <param name="PublishEvidence">The public output evidence supporting the publication form.</param>
/// <param name="ProjectUrl">The package project URL.</param>
/// <param name="RequiredNotices">The notices that must be displayed for the package.</param>
/// <param name="AttributionBasis">The source and rationale for the notice content.</param>
public sealed record OpenSourceNotice(
    string PackageName,
    string Version,
    OpenSourceNoticePublicationForm PublicationForm,
    string PublishEvidence,
    Uri ProjectUrl,
    IReadOnlyList<OpenSourceLicenseNotice> RequiredNotices,
    string AttributionBasis);

/// <summary>
/// Describes one copyright and license notice delivered for a package.
/// </summary>
/// <param name="ComponentName">The component to which the notice applies.</param>
/// <param name="LicenseName">The license identifier or name.</param>
/// <param name="CopyrightNotice">The required copyright notice.</param>
/// <param name="LicenseText">The complete required license text.</param>
/// <param name="SourceUrl">The supplementary source or license URL.</param>
public sealed record OpenSourceLicenseNotice(
    string ComponentName,
    string LicenseName,
    string CopyrightNotice,
    string LicenseText,
    Uri SourceUrl);

/// <summary>
/// Categorizes an included output outside the third-party package notice set.
/// </summary>
public enum OpenSourceNoticeExclusionKind
{
    /// <summary>
    /// The output is HouseholdLedger product code.
    /// </summary>
    ProprietaryProduct,

    /// <summary>
    /// The output is a general framework dependency.
    /// </summary>
    GeneralFrameworkDependency,
}

/// <summary>
/// Explains why a shipped output is not presented as a third-party package notice.
/// </summary>
/// <param name="Name">The output category name.</param>
/// <param name="Kind">The category distinguishing it from third-party packages.</param>
/// <param name="Reason">The factual exclusion reason.</param>
public sealed record OpenSourceNoticeExclusion(
    string Name,
    OpenSourceNoticeExclusionKind Kind,
    string Reason);