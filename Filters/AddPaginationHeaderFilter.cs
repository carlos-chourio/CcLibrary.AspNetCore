﻿using System.Threading.Tasks;
using AutoHateoas.AspNetCore.Services.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using AutoHateoas.AspNetCore.Common;
using System.Linq;
using AutoHateoas.AspNetCore.Extensions;
using System;

namespace AutoHateoas.AspNetCore.Filters {

    /// <summary>
    /// Adds X-Pagination Header to the response.
    /// The value of the response should be of IQueryable of <typeparamref name="TEntity"/>
    /// </summary>
    /// <typeparam name="TEntity">The entity</typeparam>
    [Obsolete]
    public class AddPaginationHeaderFilter<TEntity> : IAsyncResultFilter {
        private readonly IPaginationHelperService<TEntity> paginationHelperService;
        private readonly HateoasScanner filterConfiguration;

        public AddPaginationHeaderFilter(IPaginationHelperService<TEntity> paginationHelperService, HateoasScanner filterConfiguration) {
            this.paginationHelperService = paginationHelperService ?? throw new System.ArgumentNullException(nameof(paginationHelperService));
            this.filterConfiguration = filterConfiguration ?? throw new System.ArgumentNullException(nameof(filterConfiguration));
        }

        public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next) {
            var result = context.Result as ObjectResult;
            if (FiltersHelper.IsResponseSuccesful(result)) {
                PaginationModel<TEntity> paginationModel = await FiltersHelper.GetParameterFromActionAsync<PaginationModel<TEntity>>(context);
                string controllerName = context.Controller.GetType().Name;
                IQueryable<TEntity> list = result.Value as IQueryable<TEntity>;
                var pagedList = await list.ToPagedListAsync(paginationModel.PageSize, paginationModel.PageNumber);
                var paginationMetadata = paginationHelperService.GeneratePaginationMetaData(pagedList, paginationModel, controllerName, "");
                HateoasHelper.AddPaginationHeaders(filterConfiguration, context, paginationMetadata);
                await next();
            }
        }

        
    }
}