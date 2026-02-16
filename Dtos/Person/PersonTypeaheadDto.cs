namespace api.Dtos.Person
{
    public class PersonTypeaheadDto
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public DateTime BirthDate { get; set; }
        public string BirthCity { get; set; } = string.Empty;

    }
}
