using DHT.Server.Data.Filters;

namespace DHT.Server.Service.Viewer;

public readonly record struct ViewerSession(MessageFilter? MessageFilter);
