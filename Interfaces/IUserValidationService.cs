

public interface IValidationService<T>
{
    List<string> Validate(T entity);
    bool IsValid(T entity);
}
