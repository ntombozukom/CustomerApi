using CustomerApi.Application.Interfaces;
using CustomerApi.Application.Services;
using CustomerApi.Application.Validators;
using CustomerApi.Application.DTOs;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace CustomerApi.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<IValidator<CreateCustomerRequest>, CreateCustomerRequestValidator>();
        services.AddScoped<IValidator<UpdateCustomerRequest>, UpdateCustomerRequestValidator>();
       
        return services;
    }
}
