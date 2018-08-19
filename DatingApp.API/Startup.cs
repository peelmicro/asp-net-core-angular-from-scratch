using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using DatingApp.API.Data;
using DatingApp.API.Helpers;
using DatingApp.API.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;

namespace DatingApp.API
{
    public class Startup
    {
        private readonly IHostingEnvironment _env;
        public readonly IConfiguration _config;

        public Startup(IHostingEnvironment env, IConfiguration configuration)
        {
            _env = env;
            _config = configuration;
        }


        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            if (!_env.IsDevelopment())
            {
                services.AddDbContext<DataContext>(x => x
                    .UseSqlServer(_config.GetConnectionString("DefaultConnection"))
                        .ConfigureWarnings(warnings => warnings.Ignore(CoreEventId.IncludeIgnoredWarning))
                );
            } else {
                services.AddDbContext<DataContext>(x => x.
                    UseSqlite(_config.GetConnectionString("DefaultConnection"))
                );                
            }

            // AddIdentityCore is needed with only API is used
            IdentityBuilder builder = services.AddIdentityCore<User>(opt => {
                opt.Password = new PasswordOptions
                {
                    RequireDigit = false,
                    RequiredLength = 4,
                    RequireNonAlphanumeric = false,
                    RequireUppercase = false
                };
            });
            // All these are needed because we use AddIdentityCore
            builder = new IdentityBuilder(builder.UserType, typeof(Role), builder.Services);
            builder.AddEntityFrameworkStores<DataContext>();
            builder.AddRoleValidator<RoleValidator<Role>>();
            builder.AddRoleManager<RoleManager<Role>>();
            builder.AddSignInManager<SignInManager<User>>();
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options => {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII
                            .GetBytes(_config.GetSection("AppSettings:Token").Value)),
                        ValidateIssuer = false,
                        ValidateAudience = false
                    };
                });

            services.AddAuthorization(Options => {
                Options.AddPolicy("RequireAdminRole", policy => policy.RequireRole("Admin"));
                Options.AddPolicy("ModeratePhotoRole", policy => policy.RequireRole("Admin", "Moderator"));
                Options.AddPolicy("VipOnly", policy => policy.RequireRole("VIP"));
            });

            services.AddMvc(options => {
                // With this the Authorize attributes in Controllers are not needed
                var policy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
                options.Filters.Add(new AuthorizeFilter(policy));
            })
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_1)
                .AddJsonOptions(opt => {
                    opt.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
                });
            services.BuildServiceProvider().GetService<DataContext>().Database.Migrate();
            services.AddCors();
            services.Configure<CloudinarySettings>(_config.GetSection("CloudinarySettings"));
            if (_env.IsDevelopment()){
                Mapper.Reset();
            }
            services.AddAutoMapper();
            services.AddTransient<Seed>();
            services.AddScoped<LogUserActivity>();
            services.AddScoped<IDatingRepository, DatingRepository>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, Seed seeder)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler(builder => {
                    builder.Run(async context => {
                        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                        var error = context.Features.Get<IExceptionHandlerFeature>();
                        if (error != null ) {
                            context.Response.AddApplicationError(error.Error.Message);
                            await context.Response.WriteAsync(error.Error.Message);
                        }
                    });
                });
                // app.UseHsts();
            }

            seeder.SeedUsers();
            app.UseCors( x => x.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
            app.UseAuthentication();
            if (!env.IsDevelopment())
            {
                // The following three are only necesary when the content from Angular has been generated on wwwroot
                app.UseDefaultFiles();
                app.UseStaticFiles(); 
                app.UseMvc(routes => {
                    routes.MapSpaFallbackRoute(
                        name: "spa-fallback",
                        defaults: new { controller = "Fallback", action = "Index" }
                    );
                });
            } else {
                app.UseMvc();
            }
        }
    }
}
