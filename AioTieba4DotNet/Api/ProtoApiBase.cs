using AioTieba4DotNet.Abstractions;
using AioTieba4DotNet.Enums;
using AioTieba4DotNet.Exceptions;

namespace AioTieba4DotNet.Api;

/// <summary>
/// 处理 Protobuf 格式响应的 API 基类
/// </summary>
/// <param name="httpCore">Http 核心组件</param>
public abstract class ProtoApiBase(ITiebaHttpCore httpCore) : ApiBase(httpCore);

/// <summary>
/// 支持双模分发的 Protobuf API 基类
/// </summary>
/// <typeparam name="TResult">响应实体类型</typeparam>
/// <param name="httpCore">Http 核心组件</param>
/// <param name="wsCore">Websocket 核心组件</param>
/// <param name="mode">请求模式</param>
public abstract class ProtoApiWsBase<TResult>(
    ITiebaHttpCore httpCore,
    ITiebaWsCore wsCore,
    TiebaRequestMode mode = TiebaRequestMode.Http)
    : ApiWsBase<TResult>(httpCore, wsCore, mode);
