using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Models;

namespace api.Rules
{
    /// <summary>
    /// Interface for business rules.
    /// </summary>
    /// <typeparam name="T">Type of the entity.</typeparam>
    /// <remarks>

    public interface IBusinessRule<T>
    {
        string Name { get; }
        string ErrorMessage { get; }
        bool IsSatisfiedBy(T entity);
    }

}

