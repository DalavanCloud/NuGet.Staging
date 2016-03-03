﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using NuGet.Packaging;

namespace Stage.Manager.UnitTests
{
    public class TestPackage
    {
        public string Id { get; }
        public string Version { get; }

        public Stream Stream { get; private set; }

        public TestPackage(string id, string version)
        {
            Id = id;
            Version = version;
        }

        public TestPackage WithDefaultData()
        {
            Stream = CreateTestPackageStream(packageArchive =>
            {
                var nuspecEntry = packageArchive.CreateEntry(Id + ".nuspec", CompressionLevel.Fastest);
                using (var stream = nuspecEntry.Open())
                {
                    WriteNuspec(stream, true, Id, Version);
                }
            });

            return this;
        }

        public TestPackage WithInvalidNuspec()
        {
            Stream = CreateTestPackageStream(packageArchive =>
            {
            });

            return this;
        }

        public TestPackage WithMinClientVersion(string minClientVersion)
        {
            Stream = CreateTestPackageStream(packageArchive =>
            {
                var nuspecEntry = packageArchive.CreateEntry(Id + ".nuspec", CompressionLevel.Fastest);
                using (var stream = nuspecEntry.Open())
                {
                    WriteNuspec(stream, true, Id, Version, minClientVersion);
                }
            });

            return this;
        }

        private static Stream CreateTestPackageStream(Action<ZipArchive> populatePackage)
        {
            var packageStream = new MemoryStream();
            using (var packageArchive = new ZipArchive(packageStream, ZipArchiveMode.Create, leaveOpen: true))
            {
                if (populatePackage != null)
                {
                    populatePackage(packageArchive);
                }
            }

            packageStream.Position = 0;

            return packageStream;
        }

        private static void WriteNuspec(
            Stream stream,
            bool leaveStreamOpen,
            string id,
            string version,
            string minClientVersion = null,
            string title = "Package Id",
            string summary = "Package Summary",
            string authors = "Package author",
            string owners = "Package owners",
            string description = "Package Description",
            string tags = "Package tags",
            string language = null,
            string copyright = null,
            string releaseNotes = null,
            Uri licenseUrl = null,
            Uri projectUrl = null,
            Uri iconUrl = null,
            bool requireLicenseAcceptance = false,
            IEnumerable<PackageDependencyGroup> packageDependencyGroups = null)
        {
            using (var streamWriter = new StreamWriter(stream, new UTF8Encoding(false, true), 1024, leaveStreamOpen))
            {
                streamWriter.WriteLine(@"<?xml version=""1.0""?>
                    <package xmlns=""http://schemas.microsoft.com/packaging/2011/08/nuspec.xsd"">
                        <metadata" + (!string.IsNullOrEmpty(minClientVersion) ? @" minClientVersion=""" + minClientVersion + @"""" : string.Empty) + @">
                            <id>" + id + @"</id>
                            <version>" + version + @"</version>
                            <title>" + title + @"</title>
                            <summary>" + summary + @"</summary>
                            <description>" + description + @"</description>
                            <tags>" + tags + @"</tags>
                            <requireLicenseAcceptance>" + requireLicenseAcceptance + @"</requireLicenseAcceptance>
                            <authors>" + authors + @"</authors>
                            <owners>" + owners + @"</owners>
                            <language>" + (language ?? string.Empty) + @"</language>
                            <copyright>" + (copyright ?? string.Empty) + @"</copyright>
                            <releaseNotes>" + (releaseNotes ?? string.Empty) + @"</releaseNotes>
                            <licenseUrl>" + (licenseUrl?.ToString() ?? string.Empty) + @"</licenseUrl>
                            <projectUrl>" + (projectUrl?.ToString() ?? string.Empty) + @"</projectUrl>
                            <iconUrl>" + (iconUrl?.ToString() ?? string.Empty) + @"</iconUrl>
                            <dependencies>" + WriteDependencies(packageDependencyGroups) + @"</dependencies>
                        </metadata>
                    </package>");
            }
        }

        private static string WriteDependencies(IEnumerable<PackageDependencyGroup> packageDependencyGroups)
        {
            if (packageDependencyGroups == null || !packageDependencyGroups.Any())
            {
                return string.Empty;
            }

            var builder = new StringBuilder();
            foreach (var packageDependencyGroup in packageDependencyGroups)
            {
                builder.Append("<group");
                if (packageDependencyGroup.TargetFramework != null)
                {
                    builder.AppendFormat(" targetFramework=\"{0}\"", packageDependencyGroup.TargetFramework.GetShortFolderName());
                }
                builder.Append(">");

                foreach (var packageDependency in packageDependencyGroup.Packages)
                {
                    builder.AppendFormat("<dependency id=\"{0}\"", packageDependency.Id);
                    if (packageDependency.VersionRange != null)
                    {
                        builder.AppendFormat(" version=\"{0}\"", packageDependency.VersionRange);
                    }
                    builder.Append(">");
                    builder.Append("</dependency>");
                }

                builder.Append("</group>");
            }

            return builder.ToString();
        }
    }
}
