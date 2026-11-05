using MiCash.Communication.Requests.Categories;
using MiCash.Communication.Responses.Categories;
using MiCash.Domain.Entities;

namespace MiCash.Application.Mappers
{
    internal static class CategoryMapper
    {
        public static Category ToEntity ( this RequestCreateCategoryJson request ) => new()
        {
            Name = request.Name ,
            Color = request.Color ,
            Icon = request.Icon
        };

        public static ResponseCategoryJson ToResponse ( this Category category ) => new()
        {
            Id = category.Id ,
            Name = category.Name ,
            Color = category.Color ,
            Icon = category.Icon ,
            IsActive = category.IsActive
        };

        public static ResponseShortCategoryJson ToShortResponse ( this Category category ) => new()
        {
            Id = category.Id ,
            Name = category.Name ,
            Color = category.Color ,
            Icon = category.Icon ,
            IsActive = category.IsActive
        };
    }
}
