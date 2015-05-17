﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MsProjectsCleanerBase.cs" company="CatenaLogic">
//   Copyright (c) 2014 - 2015 CatenaLogic. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------


namespace RepositoryCleaner.Cleaners
{
    using System.Collections.Generic;
    using System.IO;
    using Catel.Caching;
    using Microsoft.Build.Evaluation;
    using Models;

    public abstract class MsProjectsCleanerBase : CleanerBase
    {
        private static readonly ICacheStorage<string, IEnumerable<Project>> _solutionsCache = new CacheStorage<string, IEnumerable<Project>>(); 

        protected virtual IEnumerable<Project> GetAllProjects(Repository repository)
        {
            var solutionFiles = Directory.GetFiles(repository.Directory, "*.sln", SearchOption.AllDirectories);

            var projects = new List<Project>();

            foreach (var solutionFile in solutionFiles)
            {
                var solutionProjects = GetAllProjects(solutionFile);
                projects.AddRange(solutionProjects);
            }

            return projects;
        }

        protected virtual IEnumerable<Project> GetAllProjects(string solutionFile)
        {
            return _solutionsCache.GetFromCacheOrFetch(solutionFile, () =>
            {
                var projects = ProjectHelper.GetProjectsForAllConfigurations(solutionFile);
                return projects;
            });
        }
    }
}