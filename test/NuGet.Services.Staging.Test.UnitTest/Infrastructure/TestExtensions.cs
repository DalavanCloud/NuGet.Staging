﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NuGet.Services.Staging.Authentication;
using NuGet.Services.Staging.Manager;

namespace NuGet.Services.Staging.Test.UnitTest
{
    public static class TestExtensions
    {
        public static Mock<HttpContext> WithMockHttpContext(this Controller controller)
        {
            var mockHttpContext = new Mock<HttpContext>();
            controller.ControllerContext.HttpContext = mockHttpContext.Object;
            return mockHttpContext;
        }

        public static Mock<HttpContext> WithUser(this Mock<HttpContext> httpContextMock, UserInformation userInformation)
        {
            httpContextMock.SetupGet(x => x.User.Identity.Name).Returns(userInformation.UserKey.ToString);
            httpContextMock.Setup(x => x.Items).Returns(new Dictionary<object, object> { { Constants.UserInformationKey, userInformation } });
            return httpContextMock;
        }

        public static Mock<HttpContext> WithRegisteredService(this Mock<HttpContext> httpContextMock, Action<IServiceCollection> registedService)
        {
            var serviceCollection = new ServiceCollection();
            registedService(serviceCollection);

            httpContextMock.SetupGet(x => x.RequestServices).Returns(serviceCollection.BuildServiceProvider());
            return httpContextMock;
        }

        public static Mock<HttpContext> WithBaseAddress(this Mock<HttpContext> httpContextMock)
        {
            var mockRequest = new Mock<HttpRequest>();

            mockRequest.Setup(x => x.Scheme).Returns("http");
            mockRequest.Setup(x => x.Host).Returns(new HostString("stage.nuget.org"));
            httpContextMock.Setup(x => x.Request).Returns(mockRequest.Object);
            return httpContextMock;
        }

        public static Mock<HttpContext> WithFile(this Mock<HttpContext> httpContextMock, Stream stream)
        {
            var mockRequest = new Mock<HttpRequest>();
            var mockForm = new Mock<IFormCollection>();
            var formFileCollection = new Mock<IFormFileCollection>();
            var formFileMock = new Mock<IFormFile>();

            formFileMock.Setup(x => x.OpenReadStream()).Returns(stream);
            formFileCollection.Setup(x => x[It.IsAny<int>()]).Returns(formFileMock.Object);
            mockForm.Setup(x => x.Files).Returns(formFileCollection.Object);
            mockRequest.Setup(x => x.Form).Returns(mockForm.Object);
            httpContextMock.Setup(x => x.Request).Returns(mockRequest.Object);
            return httpContextMock;
        }
    }
}