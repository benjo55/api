using System.ComponentModel.DataAnnotations.Schema;

namespace api.Models
{
    public class ContractInsuredPerson
    {
        public int ContractId { get; set; }

        [ForeignKey(nameof(ContractId))]
        public Contract Contract { get; set; } = null!;

        public int PersonId { get; set; }

        [ForeignKey(nameof(PersonId))]
        public Person Person { get; set; } = null!;
    }

}
