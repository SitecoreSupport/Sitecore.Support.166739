using Sitecore.Pipelines;
using System;
using System.Web.Http;
using System.Web.Routing;

namespace Sitecore.Support.Services.Infrastructure.Sitecore.Pipelines
{
    public class ServicesWebApiInitializer
    {
        public void Process(PipelineArgs args)
        {
            new ApplicationContainer().ResolveServicesWebApiConfiguration().Configure(GlobalConfiguration.Configuration, RouteTable.Routes);
        }
    }
}
