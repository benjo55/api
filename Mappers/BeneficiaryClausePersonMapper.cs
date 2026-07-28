using api.Dtos.BeneficiaryClause;
using api.Dtos.Person;
using api.Mappers;
using api.Models;

namespace api.Mappers
{
    public static class BeneficiaryClausePersonMapper
    {
        public static BeneficiaryClausePersonDto ToBeneficiaryClausePersonDto(this BeneficiaryClausePerson bcp)
        {
            var contract = bcp.BeneficiaryClause?.Contract;
            var subscriber = contract?.Person;

            return new BeneficiaryClausePersonDto
            {
                ClauseId = bcp.ClauseId,
                PersonId = bcp.PersonId,
                RelationWithClause = bcp.RelationWithClause,
                Percentage = bcp.Percentage,
                Person = bcp.Person == null ? null : new PersonDto
                {
                    Id = bcp.Person.Id,
                    FirstName = bcp.Person.FirstName,
                    LastName = bcp.Person.LastName,
                    BirthCountry = bcp.Person.BirthCountry,
                    BirthCity = bcp.Person.BirthCity,
                    BirthDate = bcp.Person.BirthDate,
                    Sex = bcp.Person.Sex,
                    Role = bcp.Person.Role,
                    Status = bcp.Person.Status,
                    PhoneNumber = bcp.Person.PhoneNumber,
                    Email1 = bcp.Person.Email1,
                    Email2 = bcp.Person.Email2,
                    TaxAddress = bcp.Person.TaxAddress,
                    PostalAddress = bcp.Person.PostalAddress
                },
                ContractId = contract?.Id,
                ContractNumber = contract?.ContractNumber ?? string.Empty,
                ContractLabel = contract?.ContractLabel ?? string.Empty,
                SubscriberPersonId = subscriber?.Id,
                SubscriberFirstName = subscriber?.FirstName ?? string.Empty,
                SubscriberLastName = subscriber?.LastName ?? string.Empty,
                CreatedDate = bcp.CreatedDate,
                UpdatedDate = bcp.UpdatedDate
            };
        }

        public static BeneficiaryClausePersonExportDto ToExportDto(this BeneficiaryClausePerson bcp)
        {
            return new BeneficiaryClausePersonExportDto
            {
                ClauseId = bcp.ClauseId,
                PersonId = bcp.PersonId,
                Percentage = bcp.Percentage,
            };
        }

    }
}
