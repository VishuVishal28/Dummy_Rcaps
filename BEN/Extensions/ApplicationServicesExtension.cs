using BEN.Controllers;
using BEN.Errors;
using BEN.Helpers;
using Core.Interfaces;
using Core.Models;
using Infrastructure.Data;
using Infrastructure.Data.Repository;
using Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;

namespace BEN.Extensions
{
    public static class ApplicationServicesExtension
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {


            services.AddAutoMapper(typeof(MappingProfilescs));
            services.AddScoped<ITokenServices, TokenServices>();
            services.AddTransient<IMailService, MailService>();
         
            // Add services to the container.
            services.Configure<ApiBehaviorOptions>(option =>
            {
                option.InvalidModelStateResponseFactory = actionContext =>
                {
                    var error = actionContext.ModelState
                    .Where(e => e.Value.Errors.Count > 0)
                    .SelectMany(x => x.Value.Errors)
                    .Select(x => x.ErrorMessage).ToArray();
                    var errorresponse = new ApiValidationErrorResponse
                    {
                        Errors = error
                    };
                    return new BadRequestObjectResult(errorresponse);
                };
            });
            return services;
        }
    }
}
