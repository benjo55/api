using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Dtos.Person;
using api.Models;

namespace api.Mappers
{
    public static class PersonMapper

    {
        public static PersonDto ToPersonDto(this Person personModel)
        {
            return new PersonDto
            {
                Id = personModel.Id,
                FirstName = personModel.FirstName,
                LastName = personModel.LastName,
                BirthCountry = personModel.BirthCountry,
                BirthCity = personModel.BirthCity,
                BirthDate = personModel.BirthDate,
                Sex = personModel.Sex,
                Role = personModel.Role,
                Status = personModel.Status,
                PhoneNumber = personModel.PhoneNumber,
                Email1 = personModel.Email1,
                Email2 = personModel.Email2,
                TaxAddress = personModel.TaxAddress,
                PostalAddress = personModel.PostalAddress,
                Contracts = personModel.Contracts.Select(c => c.ToContractDto()).ToList(),
                BeneficiaryClausePersons = personModel.BeneficiaryClausePersons.Select(bcp => bcp
                .ToBeneficiaryClausePersonDto())
                .ToList(),
                CreatedDate = personModel.CreatedDate,
                UpdatedDate = personModel.UpdatedDate,
            };
        }
        public static Person ToPersonFromCreateDto(this CreatePersonRequestDto personDto)

        {
            return new Person
            {
                FirstName = personDto.FirstName,
                LastName = personDto.LastName,
                BirthCountry = personDto.BirthCountry,
                BirthCity = personDto.BirthCity,
                BirthDate = personDto.BirthDate,
                Sex = personDto.Sex,
                Role = personDto.Role,
                Status = personDto.Status,
                PhoneNumber = personDto.PhoneNumber,
                Email1 = personDto.Email1,
                Email2 = personDto.Email2,
                TaxAddress = personDto.TaxAddress,
                PostalAddress = personDto.PostalAddress,
                CreatedDate = personDto.CreatedDate,
                UpdatedDate = personDto.UpdatedDate,
            };
        }

        public static Person ToPersonFromUpdateDto(this UpdatePersonRequestDto dto)
        {
            return new Person
            {
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Email1 = dto.Email1,
                Email2 = dto.Email2,
                PhoneNumber = dto.PhoneNumber,
                BirthCountry = dto.BirthCountry,
                BirthCity = dto.BirthCity,
                BirthDate = dto.BirthDate,
                Sex = dto.Sex,
                Role = dto.Role,
                Status = dto.Status,
                TaxAddress = dto.TaxAddress,
                PostalAddress = dto.PostalAddress,
                UpdatedDate = DateTime.UtcNow
            };
        }
    }
}