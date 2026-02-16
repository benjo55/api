using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace api.Helpers
{
    public class QueryObject
    {
        public string? FirstName { get; set; } = null;
        public string? LastName { get; set; } = null;
        public string? Role { get; set; } = null;
        public string? Status { get; set; }
        public string? Search { get; set; } = null;
        public string? BirthCountry { get; set; } = null;
        public int? ProductId { get; set; } = null;
        [FromQuery(Name = "personId")]
        public int? PersonId { get; set; } = null;
        public string? SortBy { get; set; } = null;
        public bool IsDescending { get; set; } = false;
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public Dictionary<string, string> Filters { get; set; } = new Dictionary<string, string>();

    }
}