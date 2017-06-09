using Sitecore.Services.Core;
using Sitecore.Services.Infrastructure.Sitecore.Controllers;
using Sitecore.Services.Infrastructure.Sitecore.Services;
using System;

namespace Sitecore.Support.Services.Infrastructure.Sitecore
{
    public class RestrictedControllerProvider : ITypeProvider
    {
        public Type[] Types
        {
            get
            {
                return new Type[]
                {
                    typeof(ItemServiceController),
                    typeof(EntityService<>)
                };
            }
        }
    }
}
