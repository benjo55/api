using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Dtos.BeneficiaryClause;
using api.Dtos.Contract;
using api.Models;

namespace api.Dtos.Person
{
    public class PersonListDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email1 { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime UpdatedDate { get; set; }
        public int ContractCount { get; set; }
    }
}