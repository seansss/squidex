﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschränkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Squidex.Shared.Users;

namespace Squidex.Domain.Users
{
    public interface IUserEvents
    {
        Task<IEnumerable<Claim>> OnUserRegisteringAsync(IUser user);

        void OnUserRegistered(IUser user);

        void OnUserUpdated(IUser user);

        void OnConsentGiven(IUser user);
    }
}
