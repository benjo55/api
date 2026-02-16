
using api.Data;
using api.Dtos.Person;
using api.Helpers;
using api.Interfaces;
using api.Mappers;
using api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;

namespace api.Controllers
{

    [ApiController]
    [Route("api/fieldDescriptions")]
    public class FieldDescriptionController : ControllerBase
    {
        private readonly IFieldDescriptionRepository _repository;
        private readonly IConfiguration _configuration;


        public FieldDescriptionController(IFieldDescriptionRepository repository, IConfiguration configuration)
        {
            _repository = repository;
            _configuration = configuration;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] QueryObject query) => Ok(await _repository.GetAllAsync(query));

        [HttpGet("entity/{entityName}")]
        public async Task<IActionResult> GetByEntityName(string entityName) =>
            Ok(await _repository.GetByEntityNameAsync(entityName));

        [HttpPost]
        public async Task<IActionResult> Create(FieldDescription fieldDescription)
        {
            var created = await _repository.CreateAsync(fieldDescription);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var fieldDesc = await _repository.GetByIdAsync(id);
            if (fieldDesc == null) return NotFound();
            return Ok(fieldDesc);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, FieldDescription fieldDescription)
        {
            var updated = await _repository.UpdateAsync(id, fieldDescription);
            if (updated == null) return NotFound();
            return Ok(updated);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _repository.DeleteAsync(id);
            if (deleted == null) return NotFound();
            return Ok(deleted);
        }



        // Endpoint pour lister les tables de la base
        [HttpGet("entityTables")]
        public IActionResult GetEntityTables()
        {
            var tables = new List<string>();
            var connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                var cmd = new SqlCommand(
                    @"SELECT TABLE_NAME
                      FROM INFORMATION_SCHEMA.TABLES
                      WHERE TABLE_TYPE = 'BASE TABLE'
                      ORDER BY TABLE_NAME", connection);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        tables.Add(reader.GetString(0));
                    }
                }
            }

            return Ok(tables);
        }

        // Endpoint pour lister les colonnes d'une table donnée
        [HttpGet("entityColumns/{tableName}")]
        public IActionResult GetEntityColumns(string tableName)
        {
            var columns = new List<string>();
            var connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                var cmd = new SqlCommand(
                    @"SELECT COLUMN_NAME
                      FROM INFORMATION_SCHEMA.COLUMNS
                      WHERE TABLE_NAME = @TableName
                      ORDER BY ORDINAL_POSITION", connection);

                cmd.Parameters.AddWithValue("@TableName", tableName);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        columns.Add(reader.GetString(0));
                    }
                }
            }

            return Ok(columns);
        }


    }

}