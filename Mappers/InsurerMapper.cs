using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Dtos.Insurer;
using api.Models;

namespace api.Mappers
{
    public static class InsurerMapper

    {
        public static InsurerDto ToInsurerDto(this Insurer InsurerModel)
        {
            return new InsurerDto
            {
                Id = InsurerModel.Id,
                Name = InsurerModel.Name,
                RegistrationNumber = InsurerModel.RegistrationNumber,
                FoundedYear = InsurerModel.FoundedYear,
                HeadQuarters = InsurerModel.HeadQuarters,
                PhoneNumber = InsurerModel.PhoneNumber,
                Email = InsurerModel.Email,
                WebSite = InsurerModel.WebSite,
                PostalAddress = InsurerModel.PostalAddress,
                IsActive = InsurerModel.IsActive,
                CreatedDate = InsurerModel.CreatedDate,
                UpdatedDate = InsurerModel.UpdatedDate,
            };
        }
        public static Insurer ToInsurerFromCreateDto(this CreateInsurerRequestDto InsurerDto)

        {
            return new Insurer
            {
                Name = InsurerDto.Name,
                RegistrationNumber = InsurerDto.RegistrationNumber,
                FoundedYear = InsurerDto.FoundedYear,
                HeadQuarters = InsurerDto.HeadQuarters,
                PhoneNumber = InsurerDto.PhoneNumber,
                Email = InsurerDto.Email,
                WebSite = InsurerDto.WebSite,
                PostalAddress = InsurerDto.PostalAddress,
                IsActive = InsurerDto.IsActive,
                CreatedDate = InsurerDto.CreatedDate,
                UpdatedDate = InsurerDto.UpdatedDate,
            };
        }
    }
}