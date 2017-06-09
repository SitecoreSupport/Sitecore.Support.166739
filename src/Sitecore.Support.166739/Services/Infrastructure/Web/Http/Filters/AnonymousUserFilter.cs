using Sitecore.Services.Core;
using Sitecore.Services.Core.Configuration;
using Sitecore.Services.Core.Diagnostics;
using Sitecore.Services.Core.Security;
using Sitecore.Services.Infrastructure.Net.Http;
using Sitecore.Services.Infrastructure.Web.Http.Filters;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http.Controllers;


namespace Sitecore.Support.Services.Infrastructure.Web.Http.Filters
{
    public class AnonymousUserFilter : AuthorizationFilterBase
    {
        private readonly IUserService _userService;

        private readonly ITypeProvider _typeProvider;

        private readonly ServicesSettingsConfiguration.ServiceConfiguration _serviceConfiguration;

        public AnonymousUserFilter(IUserService userService, IServicesConfiguration servicesConfiguration, ITypeProvider typeProvider, ILogger logger, IRequestOrigin requestOrigin) : base(logger, requestOrigin)
        {
            this._userService = userService;
            this._typeProvider = typeProvider;
            this._serviceConfiguration = servicesConfiguration.Configuration.Services;
        }

        public bool IsAnonymousUser()
        {
            string domain = Context.Domain.Name;
            if (string.IsNullOrEmpty(domain))
                domain = "extranet";
            return Context.User == null || Context.User.Name.Equals(string.Format("{0}\\Anonymous",domain),StringComparison.InvariantCultureIgnoreCase);
        }

        protected override void DoAuthorization(HttpActionContext actionContext)
        {
            bool flag = this.IsSecurityRestrictedController(actionContext.ControllerContext.Controller);
            if (flag)
            {
                bool flag2 = !this.IsAnonymousUser();///
                if (!flag2)
                {
                    bool flag3 = !this._serviceConfiguration.AllowAnonymousUser;
                    if (flag3)
                    {
                        base.LogUnauthorisedRequest(actionContext.Request, "Anonymous user access is disabled");
                        actionContext.Response = actionContext.Request.CreateResponse(HttpStatusCode.Forbidden);
                    }
                    else
                    {
                        bool flag4 = !this._userService.UserExists(this._serviceConfiguration.AnonymousUser);
                        if (flag4)
                        {
                            base.LogUnauthorisedRequest(actionContext.Request, this._serviceConfiguration.AnonymousUser + " user does not exist");
                            actionContext.Response = actionContext.Request.CreateResponse(HttpStatusCode.Forbidden);
                        }
                        else
                        {
                            this._userService.SwitchToUser(this._serviceConfiguration.AnonymousUser);
                        }
                    }
                }
            }
        }

        private bool IsSecurityRestrictedController(IHttpController controller)
        {
            return this._typeProvider.Types.Any((Type x) => x.IsInstanceOfType(controller) || this.IsSubclassOfRawGeneric(x, controller.GetType()));
        }

        private bool IsSubclassOfRawGeneric(Type generic, Type toCheck)
        {
            bool flag = !generic.IsGenericType;
            bool result;
            if (flag)
            {
                result = false;
            }
            else
            {
                while (toCheck != null && toCheck != typeof(object))
                {
                    Type right = toCheck.IsGenericType ? toCheck.GetGenericTypeDefinition() : toCheck;
                    bool flag2 = generic == right;
                    if (flag2)
                    {
                        result = true;
                        return result;
                    }
                    toCheck = toCheck.BaseType;
                }
                result = false;
            }
            return result;
        }
    }
}