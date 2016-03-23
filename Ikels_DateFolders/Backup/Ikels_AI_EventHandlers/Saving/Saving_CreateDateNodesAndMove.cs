using System;
using Umbraco.Core;
using Umbraco.Core.Events;
using Umbraco.Core.Models;
using Umbraco.Core.Services;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Ikels_AI_EventHandlers.Saving
{
    public class Saving_CreateDateNodesAndMove : ApplicationEventHandler
    {
        protected override void ApplicationStarted(UmbracoApplicationBase umbracoApplication, ApplicationContext applicationContext)
        {
            //Listen for when content is being saved
            ContentService.Saving += DateFolders_Saving;
        }

        /// <summary>
        /// Listen for when content is being saved, check if it is a new item and fill in some
        /// default data.
        /// </summary>
        private void DateFolders_Saving(IContentService sender, SaveEventArgs<IContent> e)
        {
            foreach (var node in e.SavedEntities)
            {
                // Verifico si el nodo en cuestion es una noticia o actividad
                if(node.ContentType.Alias.Equals("noticia") | node.ContentType.Alias.Equals("actividad"))
                {
                    string initialDate = node.HasProperty("initialDate") ? node.GetValue("initialDate").ToString() : string.Empty;
                    
                    // Verifico si tiene fecha
                    if(!string.IsNullOrEmpty(initialDate))
                    {
                        DateTime contentDate = Convert.ToDateTime(initialDate);

                        // Verifico quien es su padre. Si es un contentFolder, reviso
                        // si hay la estructura donde lo voy a almacenar.
                        if (node.Parent().ContentType.Alias.Equals("contentFolder"))
                        {
                            IContent contentFolderNode = node.Parent();

                            CheckNodeStructure(contentDate, node, node.Parent());
                            SortDateFolders(contentFolderNode);
                            continue;
                        }

                        // Si creo un contenido dentro de un mes...
                        if (node.Parent().ContentType.Alias.Equals("month"))
                        {
                            IContent parentMonth = node.Parent();
                            IContent grandParentYear = node.Parent().Parent();

                            // Reviso si el nodo corresponde al mes y año correcto
                            if (parentMonth.Name.Equals(contentDate.Month.ToString()) && grandParentYear.Name.Equals(contentDate.Year.ToString()))
                            {
                                continue;
                            }
                            else
                            {
                                IContent contentFolderNode = grandParentYear.Parent();

                                CheckNodeStructure(contentDate, node, grandParentYear.Parent());
                                SortDateFolders(contentFolderNode);
                                continue;
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
        /// <param name="contentFolderNode"></param>
        private void SortDateFolders(IContent contentFolderNode)
        {          
            List<IContent> unsortedChildren = contentFolderNode.Children().ToList<IContent>();
            List<IContent> sortedChildren = contentFolderNode.Children().ToList<IContent>();

            IComparer<IContent> myComparer = new NodeNameComparer();
            sortedChildren.Sort(myComparer);

            bool changesMade = !unsortedChildren.Equals(sortedChildren);

            if (changesMade)
            {
                foreach (IContent child in contentFolderNode.Children())
                {
                    child.SortOrder = sortedChildren.IndexOf(child);
                    ApplicationContext.Current.Services.ContentService.SaveAndPublishWithStatus(child);
                }

                ApplicationContext.Current.Services.ContentService.SaveAndPublishWithStatus(contentFolderNode);
            }

            foreach(IContent year in contentFolderNode.Children())
            {
                SortDateFolders(year);
            }

            return;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="contentDate"></param>
        /// <param name="node"></param>
        /// <param name="contentFolder"></param>
        private void CheckNodeStructure(DateTime contentDate, IContent node, IContent contentFolder)
        {
            IContent yearNode = GetYearNode(contentFolder, contentDate.Year);

            if (yearNode != null)
            {
                IContent monthNode = GetMonthNode(yearNode, contentDate.Month);

                if (monthNode != null)
                {
                    node.ParentId = monthNode.Id;
                    return;
                }
                else
                {
                    // Si no existe, creo el nodo del mes
                    IContent newMonth = ApplicationContext.Current.Services.ContentService.CreateContentWithIdentity(contentDate.Month.ToString(), yearNode, "month");
                    ApplicationContext.Current.Services.ContentService.PublishWithStatus(newMonth);

                    node.ParentId = newMonth.Id;
                    return;
                }
            }
            else
            {
                // Si no existe, creo el nodo de año...
                IContent newYear = ApplicationContext.Current.Services.ContentService.CreateContentWithIdentity(contentDate.Year.ToString(), contentFolder, "year");
                ApplicationContext.Current.Services.ContentService.PublishWithStatus(newYear);

                // ... luego creo el nodo del mes
                IContent newMonth = ApplicationContext.Current.Services.ContentService.CreateContentWithIdentity(contentDate.Month.ToString(), newYear, "month");
                ApplicationContext.Current.Services.ContentService.PublishWithStatus(newMonth);

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
        private IContent GetYearNode(IContent node, int year)
        {
            foreach (var child in node.Children())
            {
                if (child.ContentType.Alias.Equals("year") && child.Name.Equals(year.ToString()))
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
        private IContent GetMonthNode(IContent node, int month)
        {
            foreach (var child in node.Children())
            {
                if (child.ContentType.Alias.Equals("month") && child.Name.Equals(month.ToString()))
                    return child;
            }

            return null;
        }
    }

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
}