// #define WAIT_FOR_DEBUGGER

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using JsonRpc.Standard.Client;
using JsonRpc.Standard.Contracts;
using JsonRpc.Standard.Server;
using JsonRpc.Streams;
using LanguageServer.VsCode;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Debug;
using Autofac;
using Rhetos.Dsl;
using RhetosLanguageServer.Services;
using Rhetos.Logging;
using RhetosLSP.Extensibility;
using RhetosLSP.Dsl;

namespace RhetosLanguageServer
{
    static class Program
    {
        static void Main(string[] args)
        {
            var debugMode = args.Any(a => a.Equals("--debug", StringComparison.OrdinalIgnoreCase));
#if WAIT_FOR_DEBUGGER
            while (!Debugger.IsAttached) Thread.Sleep(1000);
            Debugger.Break();
#endif
            var argsList = new List<string>(args);

            var rhetosServerPathIndex = argsList.IndexOf("--rhetos-server-path");
            var rhetosServerPath = "";
            if (rhetosServerPathIndex > -1)
                rhetosServerPath = argsList[rhetosServerPathIndex + 1];

            var rhetosServerConfiguration = new RhetosProjectConfiguration(rhetosServerPath);

            StreamWriter logWriter = null;
            if (debugMode)
            {
                logWriter = File.CreateText("messages-" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".log");
                logWriter.AutoFlush = true;
            }
            using (logWriter)
            using (var cin = Console.OpenStandardInput())
            using (var bcin = new BufferedStream(cin))
            using (var cout = Console.OpenStandardOutput())
            using (var reader = new PartwiseStreamMessageReader(bcin))
            using (var writer = new PartwiseStreamMessageWriter(cout))
            {
                var contractResolver = new JsonRpcContractResolver
                {
                    NamingStrategy = new CamelCaseJsonRpcNamingStrategy(),
                    ParameterValueConverter = new CamelCaseJsonValueConverter(),
                };
                var clientHandler = new StreamRpcClientHandler();
                var client = new JsonRpcClient(clientHandler);
                if (debugMode)
                {
                    // We want to capture log all the LSP server-to-client calls as well
                    clientHandler.MessageSending += (_, e) =>
                    {
                        lock (logWriter) logWriter.WriteLine("{0} <C{1}", Utility.GetTimeStamp(), e.Message);
                    };
                    clientHandler.MessageReceiving += (_, e) =>
                    {
                        lock (logWriter) logWriter.WriteLine("{0} >C{1}", Utility.GetTimeStamp(), e.Message);
                    };
                }
                // Configure & build service host
                var session = new LanguageServerSession(client, contractResolver);
                ContainerBuilder builder = BuildContainer(rhetosServerConfiguration, debugMode);
                builder.RegisterInstance<LanguageServerSession>(session);
                var container = builder.Build();
                var host = BuildServiceHost(logWriter, contractResolver, debugMode, container);
                var serverHandler = new StreamRpcServerHandler(host,
                    StreamRpcServerHandlerOptions.ConsistentResponseSequence |
                    StreamRpcServerHandlerOptions.SupportsRequestCancellation);
                serverHandler.DefaultFeatures.Set(session);
                // If we want server to stop, just stop the "source"
                using (serverHandler.Attach(reader, writer))
                using (clientHandler.Attach(reader, writer))
                {
                    // Wait for the "stop" request.
                    session.CancellationToken.WaitHandle.WaitOne();
                }
                logWriter?.WriteLine("Exited");
            }
        }

        private static ContainerBuilder BuildContainer(RhetosProjectConfiguration rhetosServerConfiguration, bool debugMode)
        {
            ContainerBuilder builder = new ContainerBuilder();
            builder.RegisterType<RhetosLSP.Dsl.DslModel>().SingleInstance();
            builder.RegisterType<TextDocumentService>().SingleInstance();
            builder.RegisterType<InitializaionService>().SingleInstance();
            builder.RegisterType<WorkspaceService>().SingleInstance();
            builder.RegisterType<CompletionItemService>().SingleInstance();
            builder.RegisterType<RhetosLSP.Dsl.DslParser>().SingleInstance();
            builder.RegisterType<ParsedDslScriptProvider>().As<IParsedDslScriptProvider>().SingleInstance();
            builder.RegisterInstance<RhetosProjectConfiguration>(rhetosServerConfiguration);
            builder.RegisterInstance<IPluginFolderProvider>(rhetosServerConfiguration);
            builder.RegisterType<ConceptDescriptionProvider>().SingleInstance();
            MefPluginScanner.FindAndRegisterPlugins<IConceptInfo>(builder, rhetosServerConfiguration.PluginsFolderPath);

            if (debugMode)
            {
                builder.RegisterType<VSCodeClientLogProvider>().As<ILogProvider>().InstancePerLifetimeScope();
            }
            return builder;
        }

        private static IJsonRpcServiceHost BuildServiceHost(TextWriter logWriter,
            IJsonRpcContractResolver contractResolver, bool debugMode, IContainer container)
        {
            var loggerFactory = new LoggerFactory();
            loggerFactory.AddProvider(new DebugLoggerProvider(null));
            var builder = new JsonRpcServiceHostBuilder
            {
                ContractResolver = contractResolver,
                LoggerFactory = loggerFactory
            };
            builder.UseCancellationHandling();
            builder.Register(typeof(Program).GetTypeInfo().Assembly);
            builder.ServiceFactory = new ServiceFactory(container);
            if (debugMode)
            {
                // Log all the client-to-server calls.
                builder.Intercept(async (context, next) =>
                {
                    lock (logWriter) logWriter.WriteLine("{0} > {1}", Utility.GetTimeStamp(), context.Request);
                    await next();
                    lock (logWriter) logWriter.WriteLine("{0} < {1}", Utility.GetTimeStamp(), context.Response);
                });
            }
            return builder.Build();
        }

    }
}