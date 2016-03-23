using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using Umbraco.Core;
using Umbraco.Core.Events;
using Umbraco.Core.Models;
using Umbraco.Core.Services;

namespace Ikels_DateFolders_v7_X.EventHandlers
{
    public class Ikels_DateFolders : ApplicationEventHandler
    {
        #region Private Variables
        private string _localProvidersJson = string.Empty;
        private string _localConfigsJson = string.Empty;
        private List<DateFolderConfig> _dateFolderConfigs = null;
        private List<DateFolderProvider> _dateFolderProviders = null;
        #endregion

        protected override void ApplicationStarted(UmbracoApplicationBase umbracoApplication, ApplicationContext applicationContext)
        {
            //Listen for when content is being saved
            ContentService.Saving += DateFolders_Saving;
            ContentService.Saved += DateFolders_Saved;
        }

        /// <summary>
        /// 
        /// </summary>
        private void DateFolders_Saving(IContentService sender, SaveEventArgs<IContent> e)
        {
            // Reviso si los archivos de configuracion existen
            string configsFilePath = HttpContext.Current.Server.MapPath("~") + "/Config/IkelsDateFoldersConfigs.json";
            bool configsExists = System.IO.File.Exists(configsFilePath);

            string providersFilePath = HttpContext.Current.Server.MapPath("~") + "/Config/IkelsDateFoldersProviders.json";
            bool providersExists = System.IO.File.Exists(providersFilePath);

            if (configsExists && providersExists)
            {
                // Si los archivos de configuracion existen, debo cargar en memoria la lista de DocTypes configurados
                // y los providers.
                _localConfigsJson = System.IO.File.ReadAllText(configsFilePath, Encoding.UTF8);
                _localProvidersJson = System.IO.File.ReadAllText(providersFilePath, Encoding.UTF8);

                _dateFolderConfigs = JsonConvert.DeserializeObject<List<DateFolderConfig>>(_localConfigsJson);
                _dateFolderProviders = JsonConvert.DeserializeObject<List<DateFolderProvider>>(_localProvidersJson);

                if (_dateFolderConfigs.Count > 0 && _dateFolderProviders.Count > 0)
                {
                    foreach (var node in e.SavedEntities)
                    {
                        if (!node.HasIdentity)
                        {
                            // Obtengo el Config y Provider
                            if (_dateFolderConfigs.Exists(x => x.docTypeAlias == node.ContentType.Alias))
                            {
                                DateFolderConfig currentConfig = _dateFolderConfigs.Find(x => x.docTypeAlias == node.ContentType.Alias);

                                if (_dateFolderProviders.Exists(x => x.providerName == currentConfig.providerName))
                                {
                                    DateFolderProvider currentProvider = _dateFolderProviders.Find(x => x.providerName == currentConfig.providerName);

                                    string dateProperty = node.HasProperty(currentProvider.dateProperty) ? node.GetValue(currentProvider.dateProperty).ToString() : string.Empty;

                                    // Verifico si tiene fecha
                                    if (!string.IsNullOrEmpty(dateProperty))
                                    {
                                        DateTime contentDate = Convert.ToDateTime(dateProperty);

                                        if (node.Parent().ContentType.Alias.Equals(currentProvider.rootAlias))
                                        {
                                            IContent contentFolderNode = node.Parent();

                                            CheckNodeStructure(contentDate, node, contentFolderNode, currentProvider);
                                            continue;
                                        }                                        
                                    }
                                }
                            }
                        }
                        else
                        {
                            // Obtengo el Config y Provider
                            if (_dateFolderConfigs.Exists(x => x.docTypeAlias == node.ContentType.Alias))
                            {
                                DateFolderConfig currentConfig = _dateFolderConfigs.Find(x => x.docTypeAlias == node.ContentType.Alias);

                                if (_dateFolderProviders.Exists(x => x.providerName == currentConfig.providerName))
                                {
                                    DateFolderProvider currentProvider = _dateFolderProviders.Find(x => x.providerName == currentConfig.providerName);

                                    // Si creo un contenido dentro de un mes...
                                    if (node.Parent().ContentType.Alias.Equals(currentProvider.monthAlias))
                                    {
                                        IContent parentMonth = node.Parent();
                                        IContent grandParentYear = node.Parent().Parent();

                                        DateTime contentDate = Convert.ToDateTime(node.GetValue(currentProvider.dateProperty));

                                        // Reviso si el nodo corresponde al mes y año correcto
                                        string monthName = contentDate.ToString(currentProvider.monthFormat);
                                        string yearName = contentDate.ToString(currentProvider.yearFormat);
                                        if (parentMonth.Name.Equals(monthName) && grandParentYear.Name.Equals(yearName))
                                        {
                                            continue;
                                        }
                                        else
                                        {
                                            CheckNodeStructure(contentDate, node, grandParentYear.Parent(), currentProvider);
                                            SortDateFolders(node, currentProvider, currentConfig);
                                            continue;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            

            return;
        }

        private void DateFolders_Saved(IContentService sender, SaveEventArgs<IContent> e)
        {
            // Reviso si los archivos de configuracion existen
            string configsFilePath = HttpContext.Current.Server.MapPath("~") + "/Config/IkelsDateFoldersConfigs.json";
            bool configsExists = System.IO.File.Exists(configsFilePath);

            string providersFilePath = HttpContext.Current.Server.MapPath("~") + "/Config/IkelsDateFoldersProviders.json";
            bool providersExists = System.IO.File.Exists(providersFilePath);

            if (configsExists && providersExists)
            {
                // Si los archivos de configuracion existen, debo cargar en memoria la lista de DocTypes configurados
                // y los providers.
                _localConfigsJson = System.IO.File.ReadAllText(configsFilePath, Encoding.UTF8);
                _localProvidersJson = System.IO.File.ReadAllText(providersFilePath, Encoding.UTF8);

                _dateFolderConfigs = JsonConvert.DeserializeObject<List<DateFolderConfig>>(_localConfigsJson);
                _dateFolderProviders = JsonConvert.DeserializeObject<List<DateFolderProvider>>(_localProvidersJson);

                if (_dateFolderConfigs.Count > 0 && _dateFolderProviders.Count > 0)
                {
                    foreach (var node in e.SavedEntities)
                    {

                        // Obtengo el Config y Provider
                        if (_dateFolderConfigs.Exists(x => x.docTypeAlias == node.ContentType.Alias))
                        {
                            DateFolderConfig currentConfig = _dateFolderConfigs.Find(x => x.docTypeAlias == node.ContentType.Alias);

                            if (_dateFolderProviders.Exists(x => x.providerName == currentConfig.providerName))
                            {
                                DateFolderProvider currentProvider = _dateFolderProviders.Find(x => x.providerName == currentConfig.providerName);

                                if (node.Parent().ContentType.Alias.Equals(currentProvider.monthAlias))
                                {
                                    SortDateFolders(node, currentProvider, currentConfig);
                                }
                            }
                        }
                        
                    }
                }
            }

            return;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="currentNode"></param>
        /// <param name="provider"></param>
        private void SortDateFolders(IContent currentNode, DateFolderProvider provider, DateFolderConfig config)
        {
            string parentContentAlias = currentNode.Parent().ContentType.Alias;
            bool goRecursive = false;

            bool currentIsContent = currentNode.ContentType.Alias.Equals(config.docTypeAlias);

            if (!parentContentAlias.Equals(provider.rootAlias))
            {
                goRecursive = true;
            }

            IContent parentNode = currentNode.Parent();

            List<IContent> unsortedChildren = parentNode.Children().ToList<IContent>();
            List<IContent> sortedChildren = parentNode.Children().ToList<IContent>();

            if(currentIsContent)
            {
                IComparer<IContent> dateComparer = new NodeDateTimeComparer(provider.dateProperty);
                sortedChildren.Sort(dateComparer);
            }
            else
            {
                IComparer<IContent> nameComparer = new NodeNameComparer();
                sortedChildren.Sort(nameComparer);
            }          

            bool changesMade = !unsortedChildren.Equals(sortedChildren);

            if (changesMade)
            {
                foreach (IContent child in parentNode.Children())
                {
                    child.SortOrder = sortedChildren.IndexOf(child);
                    ApplicationContext.Current.Services.ContentService.SaveAndPublishWithStatus(child,0,false);
                }

                ApplicationContext.Current.Services.ContentService.SaveAndPublishWithStatus(parentNode,0,false);
            }

            if (goRecursive)
            {
                SortDateFolders(parentNode.Parent(), provider, config);
            }

            return;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="contentDate"></param>
        /// <param name="node"></param>
        /// <param name="contentFolder"></param>
        private void CheckNodeStructure(DateTime contentDate, IContent node, IContent contentFolder, DateFolderProvider provider)
        {
            IContent yearNode = GetYearNode(contentFolder, contentDate.ToString(provider.yearFormat), provider);

            if (yearNode != null)
            {
                IContent monthNode = GetMonthNode(yearNode, contentDate.ToString(provider.monthFormat), provider);

                if (monthNode != null)
                {
                    node.ParentId = monthNode.Id;
                    return;
                }
                else
                {
                    // Si no existe, creo el nodo del mes
                    string monthName = contentDate.ToString(provider.monthFormat);
                    IContent newMonth = ApplicationContext.Current.Services.ContentService.CreateContentWithIdentity(monthName, yearNode, provider.monthAlias);
                    ApplicationContext.Current.Services.ContentService.SaveAndPublishWithStatus(newMonth,0, false);

                    node.ParentId = newMonth.Id;
                    return;
                }
            }
            else
            {
                // Si no existe, creo el nodo de año...
                string yearName = contentDate.ToString(provider.yearFormat);
                IContent newYear = ApplicationContext.Current.Services.ContentService.CreateContentWithIdentity(yearName, contentFolder, provider.yearAlias);
                ApplicationContext.Current.Services.ContentService.SaveAndPublishWithStatus(newYear,0,false);

                // ... luego creo el nodo del mes
                string monthName = contentDate.ToString(provider.monthFormat);
                IContent newMonth = ApplicationContext.Current.Services.ContentService.CreateContentWithIdentity(monthName, newYear, provider.monthAlias);
                ApplicationContext.Current.Services.ContentService.SaveAndPublishWithStatus(newMonth,0,false);

                node.ParentId = newMonth.Id;
                return;
            }
        }

        /// <summary>
        /// Devuelve el nodo correspondiente al año que se le pasa como parametro de entrada a la
        /// función.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="year"></param>
        /// <returns></returns>
        private IContent GetYearNode(IContent node, string year, DateFolderProvider provider)
        {
            foreach (var child in node.Children())
            {
                if (child.ContentType.Alias.Equals(provider.yearAlias) && child.Name.Equals(year))
                    return child;
            }

            return null;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="yearNode"></param>
        /// <param name="p"></param>
        /// <returns></returns>
        private IContent GetMonthNode(IContent node, string month, DateFolderProvider provider)
        {
            foreach (var child in node.Children())
            {
                if (child.ContentType.Alias.Equals(provider.monthAlias) && child.Name.Equals(month))
                    return child;
            }

            return null;
        }
    }


    /// <summary>
    /// 
    /// </summary>
    public class NodeNameComparer : IComparer<IContent>
    {
        public int Compare(IContent x, IContent y)
        {
            int nameX = -1;
            int nameY = -1;
            bool parseXOk = int.TryParse(x.Name, out nameX);
            bool parseYOk = int.TryParse(y.Name, out nameY);

            if (parseXOk && parseYOk)
            {
                // Los comparo de manera invertida para que el orden sea descendiente
                return nameY.CompareTo(nameX);
            }

            return 0;
        }
    }


    /// <summary>
    /// 
    /// </summary>
    public class NodeDateTimeComparer : IComparer<IContent>
    {
        private string _propertyAlias;

        public NodeDateTimeComparer(string propertyAlias)
        {
            _propertyAlias = propertyAlias;
        }

        public int Compare(IContent x, IContent y)
        {
            if(!string.IsNullOrEmpty(_propertyAlias))
            {
                DateTime xDate = x.GetValue<DateTime>(_propertyAlias);
                DateTime yDate = y.GetValue<DateTime>(_propertyAlias);

                return yDate.CompareTo(xDate);
            }

            return 0;
        }
    }


    /// <summary>
    /// 
    /// </summary>
    public class DateFolderConfig
    {
        [JsonProperty("docTypeAlias")]
        public string docTypeAlias { get; set; }

        [JsonProperty("providerName")]
        public string providerName { get; set; }
    }


    /// <summary>
    /// 
    /// </summary>
    public class DateFolderProvider
    {
        [JsonProperty("providerName")]
        public string providerName { get; set; }

        [JsonProperty("monthAlias")]
        public string monthAlias { get; set; }

        [JsonProperty("monthFormat")]
        public string monthFormat { get; set; }

        [JsonProperty("yearAlias")]
        public string yearAlias { get; set; }

        [JsonProperty("yearFormat")]
        public string yearFormat { get; set; }

        [JsonProperty("dateProperty")]
        public string dateProperty { get; set; }

        [JsonProperty("rootAlias")]
        public string rootAlias { get; set; }
    }
}