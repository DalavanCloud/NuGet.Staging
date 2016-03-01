﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;
using Microsoft.Data.Entity;
using Microsoft.Extensions.Logging;
using NuGet.Packaging;
using NuGet.Versioning;
using Stage.Database.Models;
using Stage.Packages;
using static Stage.Manager.Controllers.Messages;
using static Stage.Manager.StageHelper;

namespace Stage.Manager.Controllers
{
    [Route("api/[controller]")]
    public class PackageController : Controller
    {
        internal static readonly NuGetVersion MaxSupportedMinClientVersion = new NuGetVersion("3.4.0.0");

        private readonly ILogger<PackageController> _logger;
        private readonly StageContext _context;
        private readonly IPackageService _packageService;

        public PackageController(ILogger<PackageController> logger, StageContext context, IPackageService packageService)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (packageService == null)
            {
                throw new ArgumentNullException(nameof(packageService));
            }

            _logger = logger;
            _context = context;
            _packageService = packageService;
        }

        [HttpPut("{id}")]
        [HttpPost("{id}")]
        public async Task<IActionResult> PushPackageToStage(string id)
        {
            var userKey = GetUserKey();

            if (!VerifyStageId(id))
            {
                return new BadRequestObjectResult(InvalidStageIdMessage);
            }

            var stage = GetStage(id);
            if (stage == null || !stage.IsUserMemberOfStage(userKey))
            {
                return new HttpNotFoundResult();
            }

            using (var packageStream = this.Request.Form.Files[0].OpenReadStream())
            using (var packageToPush = new PackageArchiveReader(packageStream, leaveStreamOpen: false))
            {
                NuspecReader nuspec = null;
                try
                {
                    nuspec = new NuspecReader(packageToPush.GetNuspec());
                }
                catch (Exception ex) when (!ex.IsFatal())
                {
                    return new BadRequestObjectResult(string.Format(NuspecErrorMessage, ex.Message));
                }

                // Check client version
                if (nuspec.GetMinClientVersion() > MaxSupportedMinClientVersion)
                {
                    return
                        new BadRequestObjectResult(string.Format(MinClientVersionOutOfRangeMessage,
                            nuspec.GetMinClientVersion()));
                }

                string registrationId = nuspec.GetId();
                var version = nuspec.GetVersion();
                string normalizedVersion = version.ToNormalizedString();

                // Check if package exists in the stage
                if (IsPackageExistsOnStage(stage, registrationId, normalizedVersion))
                {
                    return
                        new BadRequestObjectResult(string.Format(PackageExistsOnStageMessage, registrationId,
                            normalizedVersion, stage.DisplayName));
                }

                // Check if user can write to this registration id
                if (!await _packageService.IsUserOwnerOfPackageAsync(userKey, registrationId))
                {
                    return new ObjectResult(ApiKeyUnauthorizedMessage) { StatusCode = (int) HttpStatusCode.Forbidden };
                }

                stage.Packages.Add(new StagedPackage()
                {
                    Id = registrationId,
                    NormalizedVersion = normalizedVersion,
                    Version = version.ToString(),
                    UserKey = userKey,
                    PushDate = DateTime.UtcNow,
                });

                await _context.SaveChangesAsync();

                // Check if package exists in the Gallery (warning message if so)
                bool packageAlreadyExists =
                    await _packageService.IsPackageExistsByIdAndVersionAsync(registrationId, normalizedVersion);

                return packageAlreadyExists
                    ? new ObjectResult(string.Format(PackageAlreadyExists, registrationId, normalizedVersion))
                    {
                        StatusCode = (int) HttpStatusCode.Created
                    }
                    : (IActionResult) new HttpStatusCodeResult((int) HttpStatusCode.Created);
            }
        }

        private bool IsPackageExistsOnStage(Database.Models.Stage stage, string registrationId, string version)
        {
            return stage.Packages.Any(p => string.Equals(p.Id, registrationId, StringComparison.OrdinalIgnoreCase) &&
                                           string.Equals(p.NormalizedVersion, version, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// This method is virtual for test purposes. Include is an extension method, and hence, unmockable.
        /// </summary>
        public virtual Database.Models.Stage GetStage(string stageId) =>
            _context.Stages.Include(s => s.Members).Include(s => s.Packages).FirstOrDefault(s => s.Id == stageId);

        private int GetUserKey() => 1;
    }
}
