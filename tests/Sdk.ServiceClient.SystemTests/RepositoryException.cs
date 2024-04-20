namespace FireGuardians.Repository;

public class RepositoryException : Exception
{
    private RepositoryException()
    {
    }

    private RepositoryException(string message) : base(message)
    {
    }

    private RepositoryException(string message, Exception inner) : base(message, inner)
    {
    }

    public static Exception QueryReturnedNullFailed()
    {
        return new RepositoryException("Query returned null");
    }
}

