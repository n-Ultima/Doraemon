﻿using System.Collections.Generic;
using System.Linq;
using Doraemon.Common.Extensions;


namespace Doraemon.Data.Models.Core
{
    public class RoleClaimMapSearchCriteria
    {
        public IEnumerable<ulong>? RoleIds { get; set; }

        public IEnumerable<ClaimMapType>? Claims { get; set; }
    }

    internal static class RoleClaimMapQueryExtensions
    {
        internal static IQueryable<RoleClaimMap> FilterBy(this IQueryable<RoleClaimMap> query, RoleClaimMapSearchCriteria criteria)
            => query
                .FilterBy(
                    x => (x.RoleId != null && criteria!.RoleIds!.Contains(x.RoleId)),
                    (criteria?.RoleIds?.Any() ?? false))
                .FilterBy(
                    x => (x.RoleId != null) && criteria!.RoleIds!.Contains(x.RoleId),
                    (criteria?.RoleIds?.Any() ?? false))
                .FilterBy(
                    x => criteria!.Claims!.Contains(x.Type),
                    criteria?.Claims?.Any() ?? false);
    }
}