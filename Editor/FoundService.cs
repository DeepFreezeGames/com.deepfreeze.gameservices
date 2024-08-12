using System;

namespace GameServices.Editor
{
    internal sealed class FoundService
    {
        public Type Type { get; }
        public int SortOrder { get; }
        public bool IsAsync { get; }
        public bool StaticService { get; }

        public FoundService(IGameService service)
        {
            Type = service.GetType();
            SortOrder = service.SortOrder;
            StaticService = false;
        }

        public FoundService(Type type, int sortOrder, bool isAsync)
        {
            Type = type;
            SortOrder = sortOrder;
            IsAsync = isAsync;
            StaticService = true;
        }
    }
}