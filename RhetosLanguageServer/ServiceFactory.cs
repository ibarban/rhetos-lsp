using JsonRpc.Standard.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autofac;

namespace RhetosLanguageServer
{
    public class ServiceFactory : IServiceFactory
    {
        IContainer _container;

        public ServiceFactory(IContainer container)
        {
            _container = container;
        }

        public IJsonRpcService CreateService(Type serviceType, RequestContext context)
        {
            return _container.Resolve(serviceType) as IJsonRpcService;
        }

        public void ReleaseService(IJsonRpcService service)
        {
        }
    }
}
