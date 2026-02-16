using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Dtos.Notary;
using api.Models;

namespace api.Mappers
{
    public static class NotaryMappers

    {
        public static NotaryDto ToNotaryDto(this Notary notaryModel)
        {
            return new NotaryDto
            {
                Id = notaryModel.Id,
                RaisonSociale = notaryModel.RaisonSociale,
                Adresse1 = notaryModel.Adresse1,
                Adresse2 = notaryModel.Adresse2,
                CodePostal = notaryModel.CodePostal,
                Ville = notaryModel.Ville,
                Telephone = notaryModel.Telephone,
                Fax = notaryModel.Fax,
            };
        }
        public static Notary ToNotaryFromCreateDto(this CreateNotaryRequestDto notaryDto)

        {
            return new Notary
            {
                RaisonSociale = notaryDto.RaisonSociale,
                Adresse1 = notaryDto.Adresse1,
                Adresse2 = notaryDto.Adresse2,
                CodePostal = notaryDto.CodePostal,
                Ville = notaryDto.Ville,
            };
        }
    }
}