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
using Microsoft.AspNetCore.Authentication.OAuth;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json.Linq;
using System.Security.Claims;

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

            // CORS Policy: Allow any head and any method on a request from local host development machine
            services.AddCors(options =>
            {
                options.AddPolicy("AllowDevelopmentEnvironment",
                    // builder => builder.WithOrigins("http://localhost:8080")
                    builder => builder
                    .AllowAnyOrigin() // any access allowed (commented out specific port permission above)
                    .AllowAnyMethod()
                    .AllowAnyHeader());
            });

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
                TokenEndpoint = "https://sandbox.coinbase.com/oauth/token",
                // the URL accessed used after user authenticated, used to store user object
                UserInformationEndpoint = "https://api.sandbox.coinbase.com/v2/user",
                // in the example, this was written like: "https://sandbox.coinbase.com/v2/user/(id,name,username)" to store fields as "claims"

                // Scope set to just user info for now
                // scope=user%20addresses%20balance%20buttons%20contacts%20recurring_payments%20transactions
                Scope = { "user", "addresses", "balance", "contacts", "recurring_payments", "transactions", "wallet:accounts:read", "wallet:transactions:read", "wallet:transactions:send:bypass-2fa", "wallet:transactions:send" },

                Events = new OAuthEvents
                {
                    // The OnCreatingTicket event is called after the user has been authenticated and the OAuth middleware has
                    // created an auth ticket. We need to manually call the UserInformationEndpoint to retrieve the user's information,
                    // parse the resulting JSON to extract the relevant information, and add the correct claims.
                    OnCreatingTicket = async context =>
                    {
                        // Retrieve user info
                        var request = new HttpRequestMessage(HttpMethod.Get, context.Options.UserInformationEndpoint);
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", context.AccessToken);
                        request.Headers.Add("x-li-format", "json"); // Tell Coinbase we want the result in JSON, otherwise it will return XML

                        var response = await context.Backchannel.SendAsync(request, context.HttpContext.RequestAborted);
                        response.EnsureSuccessStatusCode();

                        // Extract the user info object
                        var user = JObject.Parse(await response.Content.ReadAsStringAsync());

                        // testing JObject/JToken variations
                        string fullName = (string)user["data"]["name"];
                        string gravatar = (string)user["data"]["avatar_url"];
                        JToken tData = user["data"];


                        var fullData = user["data"];
                        string userId = (string)user["data"]["id"];
                        // var userId = user["data"]["id"].ToObject();

                        // import database context
                        var optionsBuilder = new DbContextOptionsBuilder<BCAPContext>();
                        optionsBuilder.UseSqlServer(@"Server=(localdb)\mssqllocaldb;Database=BCAPDB;Trusted_Connection=True;");
                        BCAPContext BCAP = new BCAPContext(optionsBuilder.Options);

                        // Query the singular entry in current customer table
                        var currentUser = BCAP.CurrentCustomer.FirstOrDefault();

                        // add user object into currentUser table if not present, else if present, updates to currently pulled user info
                        if (currentUser == null)
                        {
                            BCAP.CurrentCustomer.Add(new CurrentCustomer
                            {
                                CustomerId = userId,
                                FullName = fullName,
                                ProfilePicUrl = gravatar,
                                Data = fullData.ToString(),
                                AccessToken = context.AccessToken
                            });
                        } else
                        {
                            // update currentUser from database
                            BCAP.CurrentCustomer.Remove(currentUser);
                            BCAP.SaveChanges();
                            BCAP.CurrentCustomer.Add(new CurrentCustomer
                            {
                                CustomerId = userId,
                                FullName = fullName,
                                ProfilePicUrl = gravatar,
                                Data = fullData.ToString(),
                                AccessToken = context.AccessToken
                            });

                            // previous update currentUser code, throwing CustomerId key exception on user change
                            //currentUser.CustomerId = userId;
                            //currentUser.FullName = fullName;
                            //currentUser.ProfilePicUrl = gravatar;
                            //currentUser.Data = fullData.ToString();
                            //currentUser.AccessToken = context.AccessToken;
                        }
                        BCAP.SaveChanges();
                        // angular starts with GET request to currentUser table for user info


                        // Placeholder "Claim" section
                        // Add the Name Identifier claim
                        if (!string.IsNullOrEmpty(userId))
                        {
                            context.Identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, userId, ClaimValueTypes.String, context.Options.ClaimsIssuer));
                        }

                        // Add the Name claim
                        var formattedName = user["data"]["name"].ToString();
                        // var formattedName = user.Value<string>("formattedName");
                        if (!string.IsNullOrEmpty(formattedName))
                        {
                            context.Identity.AddClaim(new Claim(ClaimTypes.Name, formattedName, ClaimValueTypes.String, context.Options.ClaimsIssuer));
                        }

                        context.Identity.AddClaim(new Claim("urn:token:coinbase", context.AccessToken));

                    }
                }

            });

            // Listen for requests on the /login path, and issue a challenge to log in with Coinbase middleware
            app.Map("/login", builder =>
            {
                builder.Run(async context =>
                {
                    // Return a challenge to invoke the Coinbase authentication scheme
                    // await context.Authentication.ChallengeAsync("Coinbase", properties: new AuthenticationProperties() { RedirectUri = "/" });
                    await context.Authentication.ChallengeAsync("Coinbase", properties: new AuthenticationProperties() { RedirectUri = "http://localhost:8080/#/register" });
                });
            });

            // Listen for requests on the /logout path, and sign the user out
            app.Map("/logout", builder =>
            {
                builder.Run(async context =>
                {
                    // logout clear of current customer in BCAP database
                    // import database context
                    var optionsBuilder = new DbContextOptionsBuilder<BCAPContext>();
                    optionsBuilder.UseSqlServer(@"Server=(localdb)\mssqllocaldb;Database=BCAPDB;Trusted_Connection=True;");
                    BCAPContext BCAP = new BCAPContext(optionsBuilder.Options);

                    // define and clear current customer
                    var currentUser = BCAP.CurrentCustomer.FirstOrDefault();
                    BCAP.CurrentCustomer.Remove(currentUser);
                    BCAP.SaveChanges();


                    // Sign the user out of the authentication middleware (i.e. it will clear the Auth cookie)
                    await context.Authentication.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

                    // Redirect the user to the home page after signing out
                    // context.Response.Redirect("/");
                    context.Response.Redirect("http://localhost:8080/#/register");
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
