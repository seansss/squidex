﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Orleans;
using Squidex.Domain.Apps.Core.Schemas;
using Squidex.Domain.Apps.Entities.Backup;
using Squidex.Domain.Apps.Entities.Schemas.Indexes;
using Squidex.Domain.Apps.Entities.Schemas.State;
using Squidex.Domain.Apps.Events.Schemas;
using Squidex.Infrastructure;
using Squidex.Infrastructure.EventSourcing;
using Squidex.Infrastructure.States;
using Squidex.Infrastructure.Tasks;

namespace Squidex.Domain.Apps.Entities.Schemas
{
    public sealed class BackupSchemas : BackupHandlerWithStore
    {
        private readonly HashSet<NamedId<Guid>> schemaIds = new HashSet<NamedId<Guid>>();
        private readonly Dictionary<string, Guid> schemasByName = new Dictionary<string, Guid>();
        private readonly FieldRegistry fieldRegistry;
        private readonly IGrainFactory grainFactory;

        public override string Name { get; } = "Schemas";

        public BackupSchemas(IStore<Guid> store, FieldRegistry fieldRegistry, IGrainFactory grainFactory)
            : base(store)
        {
            Guard.NotNull(fieldRegistry, nameof(fieldRegistry));
            Guard.NotNull(grainFactory, nameof(grainFactory));

            this.fieldRegistry = fieldRegistry;

            this.grainFactory = grainFactory;
        }

        public override async Task RemoveAsync(Guid appId)
        {
            var index = grainFactory.GetGrain<ISchemasByAppIndex>(appId);

            var idsToRemove = await index.GetSchemaIdsAsync();

            foreach (var schemaId in idsToRemove)
            {
                await RemoveSnapshotAsync<SchemaState>(schemaId);
            }

            await index.ClearAsync();
        }

        public override Task RestoreEventAsync(Envelope<IEvent> @event, Guid appId, BackupReader reader)
        {
            switch (@event.Payload)
            {
                case SchemaCreated schemaCreated:
                    schemaIds.Add(schemaCreated.SchemaId);
                    schemasByName[schemaCreated.SchemaId.Name] = schemaCreated.SchemaId.Id;
                    break;
            }

            return TaskHelper.Done;
        }

        public async override Task RestoreAsync(Guid appId, BackupReader reader)
        {
            await RebuildManyAsync(schemaIds.Select(x => x.Id), id => RebuildAsync<SchemaState, SchemaGrain>(id, (e, s) => s.Apply(e, fieldRegistry)));

            await grainFactory.GetGrain<ISchemasByAppIndex>(appId).RebuildAsync(schemasByName);
        }
    }
}
