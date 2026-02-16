namespace api.Exceptions
{
    /// <summary>
    /// Exception métier claire et courte, utilisée quand une règle de gestion est violée.
    /// </summary>
    public class BusinessException : Exception
    {
        public BusinessException(string message) : base(message) { }
    }
}
