﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using NuGet.Services.V3Repository;

namespace Stage.Manager
{
    public interface IV3ServiceFactory
    {
        IV3Service Create(string stageName);
    }
}
