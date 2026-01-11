using AioTieba4DotNet.Abstractions;

namespace AioTieba4DotNet.Api;

/// <summary>
///     处理 JSON 格式响应的 API 基类
/// </summary>
/// <param name="httpCore">Http 核心组件</param>
public abstract class JsonApiBase(ITiebaHttpCore httpCore) : ApiBase(httpCore);
