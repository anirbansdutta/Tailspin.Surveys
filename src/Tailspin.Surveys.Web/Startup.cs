// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System;
using Tailspin.Surveys.Common;
using Tailspin.Surveys.Data.DataModels;
using Tailspin.Surveys.Security.Policy;
using Tailspin.Surveys.TokenStorage;
using Tailspin.Surveys.Web.Security;
using Tailspin.Surveys.Web.Services;
using SurveyAppConfiguration = Tailspin.Surveys.Web.Configuration;


namespace Tailspin.Surveys.Web
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddDebug();
                builder.SetMinimumLevel(LogLevel.Information);
            });

            services.Configure<SurveyAppConfiguration.ConfigurationOptions>(options => Configuration.Bind(options));
            var configOptions = new SurveyAppConfiguration.ConfigurationOptions();
            Configuration.Bind(configOptions);

            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(o =>
                {
                    o.AccessDeniedPath = "/Home/Forbidden";
                    o.ExpireTimeSpan = TimeSpan.FromHours(1);
                    o.SlidingExpiration = true;
                    o.Cookie = (o.Cookie ?? new CookieBuilder());
                    o.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                })
                .AddOpenIdConnect(o =>
                {
                    o.ClientId = configOptions.AzureAd.ClientId;
                    o.ClientSecret = configOptions.AzureAd.ClientSecret;
                    o.Authority = Constants.AuthEndpointPrefix;
                    o.ResponseType =  OpenIdConnectResponseType.CodeIdToken;
                    o.SignedOutRedirectUri = configOptions.AzureAd.PostLogoutRedirectUri;
                    o.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    o.TokenValidationParameters = new TokenValidationParameters { ValidateIssuer = false };
                    o.Events = new SurveyAuthenticationEvents(configOptions.AzureAd, loggerFactory);
                });

            // This will add the Redis implementation of IDistributedCache
            services.AddDistributedRedisCache(setup =>
            {
                setup.Configuration = configOptions.Redis.Configuration;
            });

            services.AddAuthorization(options =>
            {
                options.AddPolicy(PolicyNames.RequireSurveyCreator,
                    policy =>
                    {
                        policy.AddRequirements(new SurveyCreatorRequirement());
                        policy.RequireAuthenticatedUser(); // Adds DenyAnonymousAuthorizationRequirement 
                        // By adding the CookieAuthenticationDefaults.AuthenticationScheme,
                        // if an authenticated user is not in the appropriate role, they will be redirected to the "forbidden" experience.
                        policy.AddAuthenticationSchemes(CookieAuthenticationDefaults.AuthenticationScheme);
                    });

                options.AddPolicy(PolicyNames.RequireSurveyAdmin,
                    policy =>
                    {
                        policy.AddRequirements(new SurveyAdminRequirement());
                        policy.RequireAuthenticatedUser(); // Adds DenyAnonymousAuthorizationRequirement 
                        // By adding the CookieAuthenticationDefaults.AuthenticationScheme,
                        // if an authenticated user is not in the appropriate role, they will be redirected to the "forbidden" experience.
                        policy.AddAuthenticationSchemes(CookieAuthenticationDefaults.AuthenticationScheme);
                    });
            });

            // Add Entity Framework services to the services container.
            services.AddEntityFrameworkSqlServer()
                .AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(Configuration.GetSection("Data")["SurveysConnectionString"]));

            // Add MVC services to the services container.
            services.AddControllersWithViews();

            // Register application services.

            // This will register IDistributedCache based token cache which ADAL will use for caching access tokens.
            services.AddScoped<ITokenCacheService, DistributedTokenCacheService>();
            services.AddScoped<ISurveysTokenService, SurveysTokenService>();
            services.AddSingleton<HttpClientService>();

            // Uncomment the following line to use client certificate credentials.
            services.AddSingleton<ICredentialService, CertificateCredentialService>();

            // Comment out the following line if you are using client certificates.
            //services.AddSingleton<ICredentialService, ClientCredentialService>();

            services.AddScoped<ISurveyService, SurveyService>();
            services.AddScoped<IQuestionService, QuestionService>();
            services.AddScoped<SignInManager, SignInManager>();
            services.AddScoped<TenantManager, TenantManager>();
            services.AddScoped<UserManager, UserManager>();
            services.AddHttpContextAccessor();
        }

        // Configure is called after ConfigureServices is called.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory)
        {
            var configOptions = new SurveyAppConfiguration.ConfigurationOptions();
            Configuration.Bind(configOptions);

            // Configure the HTTP request pipeline.
            // Add the following to the request pipeline only in development environment.
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
            }
            else
            {
                // Add Error handling middleware which catches all application specific errors and
                // sends the request to the following path or controller action.
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
