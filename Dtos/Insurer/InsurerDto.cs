namespace api.Dtos.Insurer
{
    public class InsurerDto : InsurerInputDto
    {
        public int Id { get; set; }
        public int ProductCount { get; set; }
        public int ContractCount { get; set; }
        public int BrandCount { get; set; }
        public int DocumentCount { get; set; }
        public int PersonCount { get; set; }
        public int AuthorizationCount { get; set; }
        public int ExerciseCountryCount { get; set; }
    }
}
