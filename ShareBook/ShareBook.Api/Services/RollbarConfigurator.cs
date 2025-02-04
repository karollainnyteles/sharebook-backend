﻿using Rollbar;
using ShareBook.Domain.Exceptions;

namespace ShareBook.Api.Services
{
    public static class RollbarConfigurator
    {
        public static bool IsActive { get; private set; }

        public static void Configure(string environment, string isActive, string token, string logLevel)
        {
            if (string.IsNullOrEmpty(environment) ||
                string.IsNullOrEmpty(isActive) ||
                string.IsNullOrEmpty(token))
                return;

            var result = ErrorLevel.TryParse(logLevel, out ErrorLevel logLevelEnum);

            if (!result)
                throw new RollbarInvalidException("Rollbar invalid logLevel: " + logLevel);

            RollbarLocator.RollbarInstance.Configure(new RollbarConfig(accessToken: token) { Environment = environment, LogLevel = logLevelEnum, AccessToken = token });
            RollbarLocator.RollbarInstance.Info($"Rollbar is configured properly in {environment} environment.");

            IsActive = true;
        }
    }
}