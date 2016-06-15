using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using BlockchainAutoPay.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Authentication;

namespace BlockchainAutoPay
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true);

            if (env.IsEnvironment("Development"))
            {
                // This will push telemetry data through Application Insights pipeline faster, allowing you to view results immediately.
                builder.AddApplicationInsightsSettings(developerMode: true);
            }

            builder.AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container
        public void ConfigureServices(IServiceCollection services)
        {
            // Add Authentication services
            services.AddAuthentication(
                options => options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme); 

            var connection = @"Server=(localdb)\mssqllocaldb;Database=BCAPDB;Trusted_Connection=True;";
            services.AddDbContext<BCAPContext>(options => options.UseSqlServer(connection));

            // Add framework services.
            services.AddApplicationInsightsTelemetry(Configuration);

            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            // Add cookie middleware
            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AutomaticAuthenticate = true,
                AutomaticChallenge = true,
                LoginPath = new PathString("/login"),
                LogoutPath = new PathString("/logout")
            });

            // Add the OAuth2 middleware
            app.UseOAuthAuthentication(new OAuthOptions
            {
                AuthenticationScheme = "Coinbase",

                ClientId = Configuration["coinbase:clientId"],
                ClientSecret = Configuration["coinbase:clientSecret"],

                CallbackPath = new PathString("/signin-coinbase"),

                AuthorizationEndpoint = "https://sandbox.coinbase.com/oauth/authorize",
                TokenEndpoint = "http://sandbox.coinbase.com/oauth/token",
                // PROBABLY the URL used after user authenticated
                UserInformationEndpoint = "https://sandbox.coinbase.com/v2/user",
                // in the example, this was written like: "https://sandbox.coinbase.com/v2/user/(id,name,username)"

                // Scope set to just user info for now
                Scope = { "user" }

            });

            // Listen for requests on the /login path, and issue a challenge to log in with Coinbase middleware
            app.Map("/login", builder =>
            {
                builder.Run(async context =>
                {
                    // Return a challenge to invoke the Coinbase authentication scheme
                    await context.Authentication.ChallengeAsync("Coinbase", properties: new AuthenticationProperties() { RedirectUri = "/" });
                });
            });

            // Listen for requests on the /logout path, and sign the user out
            app.Map("/logout", builder =>
            {
                builder.Run(async context =>
                {
                    // Sign the user out of the authentication middleware (i.e. it will clear the Auth cookie)
                    await context.Authentication.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

                    // Redirect the user to the home page after signing out
                    context.Response.Redirect("/");
                });
            });

            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            app.UseApplicationInsightsRequestTelemetry();

            app.UseApplicationInsightsExceptionTelemetry();

            app.UseMvc();
        }
    }
}
