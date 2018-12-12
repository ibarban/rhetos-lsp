using Autofac;
using Rhetos.Extensibility;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.ReflectionModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace RhetosLSP.Extensibility
{
    public static class MefPluginScanner
    {
        /// <summary>
        /// The key is FullName of the plugin's export type (it is usually the interface it implements).
        /// </summary>
        private static MultiDictionary<string, PluginInfo> _pluginsByExport = null;
        private static object _pluginsLock = new object();

        public static void FindAndRegisterPlugins<TPluginInterface>(ContainerBuilder builder, string pluginFolderPath)
        {
            var matchingPlugins = MefPluginScanner.FindPlugins(typeof(TPluginInterface), pluginFolderPath);
            RegisterPlugins(builder, matchingPlugins, typeof(TPluginInterface));
        }

        private static void RegisterPlugins(ContainerBuilder builder, IEnumerable<PluginInfo> matchingPlugins, Type exportType)
        {
            if (matchingPlugins.Count() == 0)
                return;

            foreach (var plugin in matchingPlugins)
            {
                var registration = builder.RegisterType(plugin.Type).As(new[] { exportType });

                foreach (var metadataElement in plugin.Metadata)
                {
                    registration = registration.WithMetadata(metadataElement.Key, metadataElement.Value);
                    if (metadataElement.Key == MefProvider.Implements)
                        registration = registration.Keyed(metadataElement.Value, exportType);
                }
            }
        }

        /// <summary>
        /// Returns plugins that are registered for the given interface, sorted by dependencies (MefPovider.DependsOn).
        /// </summary>
        internal static IEnumerable<PluginInfo> FindPlugins(Type pluginInterface, string pluginFolderPath)
        {
            try
            {
                lock (_pluginsLock)
                {
                    if (_pluginsByExport == null)
                    {
                        var assemblies = ListAssemblies(pluginFolderPath);
                        _pluginsByExport = LoadPlugins(assemblies);
                    }

                    return _pluginsByExport.Get(pluginInterface.FullName);
                }
            }
            catch (ReflectionTypeLoadException ex)
            {
                throw new Exception(CsUtility.ReportTypeLoadException(ex, "Cannot load plugins."), ex);
            }
        }

        private static List<string> ListAssemblies(string plugonFolderPath)
        {
            var stopwatch = Stopwatch.StartNew();

            string[] pluginsPath = new[] { plugonFolderPath };

            List<string> assemblies = new List<string>();
            foreach (var path in pluginsPath)
                if (File.Exists(path))
                    assemblies.Add(Path.GetFullPath(path));
                else if (Directory.Exists(path))
                    assemblies.AddRange(Directory.GetFiles(path, "*.dll", SearchOption.AllDirectories));
            // If the path does not exist, it may be generated later (see DetectAndRegisterNewModulesAndPlugins).

            assemblies.Sort();

            return assemblies;
        }

        private static MultiDictionary<string, PluginInfo> LoadPlugins(List<string> assemblies)
        {
            var stopwatch = Stopwatch.StartNew();

            var assemblyCatalogs = assemblies.Select(a => new AssemblyCatalog(a));
            var container = new CompositionContainer(new AggregateCatalog(assemblyCatalogs));
            var mefPlugins = container.Catalog.Parts
                .Select(part => new
                {
                    PluginType = ReflectionModelServices.GetPartType(part).Value,
                    part.ExportDefinitions
                })
                .SelectMany(part =>
                    part.ExportDefinitions.Select(exportDefinition => new
                    {
                        exportDefinition.ContractName,
                        exportDefinition.Metadata,
                        part.PluginType
                    }));

            var pluginsByExport = new MultiDictionary<string, PluginInfo>();
            int pluginsCount = 0;
            foreach (var mefPlugin in mefPlugins)
            {
                pluginsCount++;
                pluginsByExport.Add(
                    mefPlugin.ContractName,
                    new PluginInfo
                    {
                        Type = mefPlugin.PluginType,
                        Metadata = mefPlugin.Metadata.ToDictionary(m => m.Key, m => m.Value)
                    });
            }

            foreach (var pluginsGroup in pluginsByExport)
                SortByDependency(pluginsGroup.Value);

            return pluginsByExport;
        }

        private static void SortByDependency(List<PluginInfo> plugins)
        {
            var dependencies = plugins
                .Where(p => p.Metadata.ContainsKey(MefProvider.DependsOn))
                .Select(p => Tuple.Create((Type)p.Metadata[MefProvider.DependsOn], p.Type))
                .ToList();

            var pluginTypes = plugins.Select(p => p.Type).ToList();
            Graph.TopologicalSort(pluginTypes, dependencies);
            Graph.SortByGivenOrder(plugins, pluginTypes, p => p.Type);
        }

        internal static void ClearCache()
        {
            lock (_pluginsLock)
                _pluginsByExport = null;
        }
    }
}
