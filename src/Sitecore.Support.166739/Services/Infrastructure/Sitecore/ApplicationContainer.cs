using Sitecore.Services.Core;
using Sitecore.Services.Core.ComponentModel.DataAnnotations;
using Sitecore.Services.Core.Configuration;
using Sitecore.Services.Core.Diagnostics;
using Sitecore.Services.Core.MetaData;
using Sitecore.Services.Infrastructure.Configuration;
using Sitecore.Services.Infrastructure.Net.Http;
using Sitecore.Services.Infrastructure.Reflection;
using Sitecore.Services.Infrastructure.Services;
using Sitecore.Services.Infrastructure.Sitecore;
using Sitecore.Services.Infrastructure.Sitecore.Configuration;
using Sitecore.Services.Infrastructure.Sitecore.Diagnostics;
using Sitecore.Services.Infrastructure.Sitecore.Handlers;
using Sitecore.Services.Infrastructure.Sitecore.Security;
using Sitecore.Services.Infrastructure.Web.Http;
using Sitecore.Services.Infrastructure.Web.Http.Dispatcher;
using Sitecore.Services.Infrastructure.Web.Http.Filters;
using Sitecore.Services.Infrastructure.Web.Http.Formatting;
using Sitecore.Support.Services.Infrastructure.Web.Http.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Formatting;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Web.Http;
using System.Web.Http.Dispatcher;
using System.Web.Http.Filters;
using AnonymousUserFilter = Sitecore.Support.Services.Infrastructure.Web.Http.Filters.AnonymousUserFilter;
using ConfigurationFilterProvider = Sitecore.Services.Core.Configuration.ConfigurationFilterProvider;

namespace Sitecore.Support.Services.Infrastructure.Sitecore
{
    public class ApplicationContainer
    {
        private Dictionary<Type, Func<object>> _builderMethods;

        private readonly SitecoreLogger _logger;

        private readonly ServicesSettingsConfigurationProvider _servicesSettings;

        private readonly ITypeProvider _restrictedControllerProvider;

        private readonly HttpRequestOrigin _httpRequestOrigin;

        private static Assembly[] _siteAssemblies;

        public ApplicationContainer()
        {
            this._servicesSettings = new ServicesSettingsConfigurationProvider();
            this._logger = new SitecoreLogger();
            this._restrictedControllerProvider = new RestrictedControllerProvider();
            this._httpRequestOrigin = new HttpRequestOrigin();
        }

        public static IEntityValidator ResolveEntityValidator()
        {
            return new EntityValidator();
        }

        public static IMetaDataBuilder ResolveMetaDataBuilder()
        {
            List<string> list = new List<string>
            {
                "List`1",
                "IEnumerable`1"
            };
            EntityParser entityParser = new EntityParser(new JavascriptTypeMapper(), list, ApplicationContainer.ResolveValidationMetaDataProvider());
            return new MetaDataBuilder(entityParser);
        }

        private static IValidationMetaDataProvider ResolveValidationMetaDataProvider()
        {
            ILogger logger = ApplicationContainer.ResolveLogger();
            return new AssemblyScannerValidationMetaDataProvider(new ValidationMetaDataTypeProvider(ApplicationContainer.GetSiteAssemblies()), logger);
        }

        public virtual IHttpConfiguration ResolveServicesWebApiConfiguration()
        {
            this._builderMethods = this.CreateBuilderMapping();
            NamespaceQualifiedUniqueNameGenerator namespaceQualifiedUniqueNameGenerator = new NamespaceQualifiedUniqueNameGenerator(DefaultHttpControllerSelector.ControllerSuffix);
            NamespaceHttpControllerSelector namespaceHttpControllerSelector = new NamespaceHttpControllerSelector(System.Web.Http.GlobalConfiguration.Configuration, namespaceQualifiedUniqueNameGenerator);
            IMapRoutes instance = new ConfigurationRouteConfigurationFactory(this._servicesSettings, this._logger).Instance;
            ConfigurationFilterProvider configurationFilterProvider = new ConfigurationFilterProvider(new FilterProvider(ApplicationContainer.GetSiteAssemblies()), new FilterTypeNames(this._logger).Types);
            IEnumerable<IFilter> filters = this.GetFilters(configurationFilterProvider.Types);
           MediaTypeFormatter[] array = new MediaTypeFormatter[]
            {
                new BrowserJsonFormatter()
            };
            return new ServicesHttpConfiguration(namespaceHttpControllerSelector, instance, filters.ToArray<IFilter>(), array, this._logger);
        }

        private static Assembly[] GetSiteAssemblies()
        {
            Assembly[] result;
            bool flag = (result = ApplicationContainer._siteAssemblies) == null;
            if (flag)
            {
                result = (ApplicationContainer._siteAssemblies = AppDomain.CurrentDomain.GetAssemblies());
            }
            return result;
        }

        private IEnumerable<IFilter> GetFilters(IEnumerable<Type> filterTypes)
        {
            foreach (Type current in filterTypes)
            {
                if (this._builderMethods.ContainsKey(current))
                {
                    yield return (IFilter)this._builderMethods[current]();
                }
                else
                {
                    IFilter filter = (IFilter)this.CreateInstance(current);
                    if (filter != null)
                    {
                        yield return filter;
                    }
                    else
                    {
                        this._logger.Error("Filter ({0}) instance cannot be created, missing builder mapping", new object[]
                        {
                            current
                        });
                    }
                }
            }
            yield break;
        }

        private object CreateInstance(Type type)
        {
            object result;
            try
            {
                result = Activator.CreateInstance(type);
                return result;
            }
            catch (Exception ex)
            {
                this._logger.Warn("Failed to create instanec of {0}, exception details {1}", new object[]
                {
                    ex.Message
                });
            }
            result = null;
            return result;
        }

        protected virtual Dictionary<Type, Func<object>> CreateBuilderMapping()
        {
            return new Dictionary<Type, Func<object>>
            {
                {
                    typeof(AnonymousUserFilter),
                    new Func<object>(this.BuildAnonymousUserFilter)
                },
                {
                    typeof(SecurityPolicyAuthorisationFilter),
                    new Func<object>(this.BuildSecurityPolicyAuthorisationFilter)
                },
                {
                    typeof(LoggingExceptionFilter),
                    new Func<object>(this.BuildLoggingExceptionFilter)
                }
            };
        }

        private object BuildAnonymousUserFilter()
        {
            return new AnonymousUserFilter(new UserService(), this._servicesSettings, this._restrictedControllerProvider, this._logger, this._httpRequestOrigin);
        }

        private object BuildSecurityPolicyAuthorisationFilter()
        {
            return new SecurityPolicyAuthorisationFilter(new ConfigurationSecurityPolicyFactory(this._servicesSettings, this._logger), this._logger, this._httpRequestOrigin, new AllowedControllerTypeNames(this._logger).Types);
        }

        protected object BuildLoggingExceptionFilter()
        {
            return new LoggingExceptionFilter(this._logger);
        }

        public static IHandlerProvider ResolveHandlerProvider()
        {
            return new HandlerProvider();
        }

        public static ILogger ResolveLogger()
        {
            return new SitecoreLogger();
        }
    }
}