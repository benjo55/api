using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Dtos.Brand;
using api.Models;
using Brand = api.Models.Brand;

namespace api.Mappers
{
    public static class BrandMapper
    {
        public static BrandDto ToBrandDto(this Brand brandModel)
        {
            return new BrandDto
            {
                Id = brandModel.Id,
                BrandCode = brandModel.BrandCode,
                BrandName = brandModel.BrandName,
                Description = brandModel.Description,
                LogoUrl = brandModel.LogoUrl,
                Slogan = brandModel.Slogan,
                Website = brandModel.Website,
                ContactEmail = brandModel.ContactEmail,
                Country = brandModel.Country,
                City = brandModel.City,
                FoundedYear = brandModel.FoundedYear,
                Founder = brandModel.Founder,
                Industry = brandModel.Industry,
                MainColor = brandModel.MainColor,
                FacebookUrl = brandModel.FacebookUrl,
                InstagramUrl = brandModel.InstagramUrl,
                LinkedInUrl = brandModel.LinkedInUrl,
                Notes = brandModel.Notes,
                ParentGroup = brandModel.ParentGroup,
                IsActive = brandModel.IsActive,
                CreatedDate = brandModel.CreatedDate,
                UpdatedDate = brandModel.UpdatedDate
            };
        }
        public static Brand ToBrandFromCreateDto(this CreateBrandRequestDto brandDto)
        {
            return new Brand
            {
                BrandCode = brandDto.BrandCode,
                BrandName = brandDto.BrandName,
                Description = brandDto.Description,
                LogoUrl = brandDto.LogoUrl,
                Slogan = brandDto.Slogan,
                Website = brandDto.Website,
                ContactEmail = brandDto.ContactEmail,
                Country = brandDto.Country,
                City = brandDto.City,
                FoundedYear = brandDto.FoundedYear,
                Founder = brandDto.Founder,
                Industry = brandDto.Industry,
                MainColor = brandDto.MainColor,
                FacebookUrl = brandDto.FacebookUrl,
                InstagramUrl = brandDto.InstagramUrl,
                LinkedInUrl = brandDto.LinkedInUrl,
                Notes = brandDto.Notes,
                ParentGroup = brandDto.ParentGroup,
                IsActive = brandDto.IsActive,
                CreatedDate = brandDto.CreatedDate,
                UpdatedDate = brandDto.UpdatedDate
            };
        }
        public static Brand ToBrandFromUpdateDto(this UpdateBrandRequestDto brandDto)
        {
            return new Brand
            {
                BrandCode = brandDto.BrandCode,
                BrandName = brandDto.BrandName,
                Description = brandDto.Description,
                LogoUrl = brandDto.LogoUrl,
                Slogan = brandDto.Slogan,
                Website = brandDto.Website,
                ContactEmail = brandDto.ContactEmail,
                Country = brandDto.Country,
                City = brandDto.City,
                FoundedYear = brandDto.FoundedYear,
                Founder = brandDto.Founder,
                Industry = brandDto.Industry,
                MainColor = brandDto.MainColor,
                FacebookUrl = brandDto.FacebookUrl,
                InstagramUrl = brandDto.InstagramUrl,
                LinkedInUrl = brandDto.LinkedInUrl,
                Notes = brandDto.Notes,
                ParentGroup = brandDto.ParentGroup,
                IsActive = brandDto.IsActive,
                CreatedDate = brandDto.CreatedDate,
                UpdatedDate = brandDto.UpdatedDate,
            };
        }
    }
}