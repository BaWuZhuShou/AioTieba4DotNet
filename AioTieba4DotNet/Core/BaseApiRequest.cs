namespace AioTieba4DotNet.Core;

public interface IRequest<TResponse>
{
    Task<TResponse> RequestAsync();
}

public interface IRequest<TRequest, TResponse>
{
    Task<TResponse> RequestAsync(TRequest requestParams);
}

public interface IParse<TResponse>
{
    public abstract TResponse ParseBody(string body);
}

public interface IParse<TResponse, TBody>
{
    public abstract TResponse ParseBody(TBody body);
}

public abstract class BaseApiRequest<TResponse> : IRequest<TResponse>, IParse<TResponse>
{
    public abstract TResponse ParseBody(string body);
    public abstract Task<TResponse> RequestAsync();
}

public abstract class BaseApiRequest<TRequest, TResponse> : IRequest<TRequest, TResponse>, IParse<TResponse>
{
    public abstract TResponse ParseBody(string body);

    public abstract Task<TResponse> RequestAsync(TRequest requestParams);
}

public abstract class BaseApiRequest<TRequest, TResponse, TBody> : IRequest<TRequest, TResponse>,
    IParse<TResponse, TBody>
{
    public abstract TResponse ParseBody(TBody body);
    public abstract Task<TResponse> RequestAsync(TRequest requestParams);
}