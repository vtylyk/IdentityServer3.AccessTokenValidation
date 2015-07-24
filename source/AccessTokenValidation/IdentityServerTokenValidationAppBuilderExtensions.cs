﻿/*
 * Copyright 2015 Dominick Baier, Brock Allen
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using IdentityServer3.AccessTokenValidation;
using Microsoft.Owin.Logging;
using Microsoft.Owin.Security.Jwt;
using Microsoft.Owin.Security.OAuth;
using System.IdentityModel.Tokens;
using System.Linq;

namespace Owin
{
    public static class IdentityServerTokenValidationAppBuilderExtensions
    {
        public static IAppBuilder UseIdentityServerTokenAuthentication(this IAppBuilder app, IdentityServerTokenAuthenticationOptions options)
        {
            var loggerFactory = app.GetLoggerFactory();
            var middlewareOptions = new IdentityServerOAuthBearerAuthenticationOptions();

            if (options.ValidationMode == ValidationMode.Both ||
                options.ValidationMode == ValidationMode.LocalOnly)
            {
                middlewareOptions.LocalValidationOptions = ConfigureLocalValidation(options, loggerFactory);
            }
            
            if (options.ValidationMode == ValidationMode.Both ||
                options.ValidationMode == ValidationMode.ValidationEndpointOnly)
            {
                middlewareOptions.EndpointValidationOptions = ConfigureEndpointValidation(options, loggerFactory);
            }

            if (options.TokenProvider != null)
            {
                middlewareOptions.TokenProvider = options.TokenProvider;
            }

            app.Use<IdentityServerTokenValidationMiddleware>(middlewareOptions);

            if (options.RequiredScopes.Any())
            {
                app.Use<ScopeRequirementMiddleware>(options.RequiredScopes);
            }

            return app;
        }

        private static OAuthBearerAuthenticationOptions ConfigureEndpointValidation(IdentityServerTokenAuthenticationOptions options, ILoggerFactory loggerFactory)
        {
            if (options.EnableValidationResultCache)
            {
                if (options.ValidationResultCache == null)
                {
                    options.ValidationResultCache = new InMemoryValidationResultCache(options);
                }
            }

            var bearerOptions = new OAuthBearerAuthenticationOptions
            {
                AuthenticationMode = options.AuthenticationMode,
                AuthenticationType = options.AuthenticationType,
                AccessTokenProvider = new ValidationEndpointTokenProvider(options, loggerFactory),
                Provider = new ContextTokenProvider(),
            };

            return bearerOptions;
        }

        internal static OAuthBearerAuthenticationOptions ConfigureLocalValidation(IdentityServerTokenAuthenticationOptions options, ILoggerFactory loggerFactory)
        {
            var discoveryEndpoint = options.Authority.EnsureTrailingSlash();
            discoveryEndpoint += ".well-known/openid-configuration";

            var issuerProvider = new DiscoveryDocumentIssuerSecurityTokenProvider(
                discoveryEndpoint,
                options);

            var valParams = new TokenValidationParameters
            {
                ValidAudience = issuerProvider.Audience,
                NameClaimType = options.NameClaimType,
                RoleClaimType = options.RoleClaimType
            };

            var tokenFormat = new JwtFormat(valParams, issuerProvider);

            var bearerOptions = new OAuthBearerAuthenticationOptions
            {
                AccessTokenFormat = tokenFormat,
                AuthenticationMode = options.AuthenticationMode,
                AuthenticationType = options.AuthenticationType,
                Provider = new ContextTokenProvider()
            };

            return bearerOptions;
        }
    }
}