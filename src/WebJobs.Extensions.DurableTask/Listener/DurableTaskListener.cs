﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Azure.WebJobs.Host.Listeners;

namespace Microsoft.Azure.WebJobs.Extensions.DurableTask
{
    internal sealed class DurableTaskListener : IListener
    {
        private readonly DurableTaskExtension config;
        private readonly FunctionName functionName;
        private readonly ITriggeredFunctionExecutor executor;
        private readonly bool isOrchestrator;

        public DurableTaskListener(
            DurableTaskExtension config,
            FunctionName functionName,
            ITriggeredFunctionExecutor executor,
            bool isOrchestrator)
        {
            this.config = config ?? throw new ArgumentNullException(nameof(config));
            this.executor = executor ?? throw new ArgumentNullException(nameof(executor));

            if (functionName == default(FunctionName))
            {
                throw new ArgumentNullException(nameof(functionName));
            }

            this.functionName = functionName;
            this.isOrchestrator = isOrchestrator;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            if (this.isOrchestrator)
            {
                this.config.RegisterOrchestrator(this.functionName, this.executor);
            }
            else
            {
                this.config.RegisterActivity(this.functionName, this.executor);
            }

            return this.config.StartTaskHubWorkerIfNotStartedAsync();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            // The actual listener is a task hub worker, which is shared by all orchestration
            // and activity function listeners in the function app. The task hub worker
            // gets shut down only when all durable functions are shut down.
            if (this.isOrchestrator)
            {
                this.config.DeregisterOrchestrator(this.functionName);
            }
            else
            {
                this.config.DeregisterActivity(this.functionName);
            }

            return this.config.StopTaskHubWorkerIfIdleAsync();
        }

        public void Cancel()
        {
            this.StopAsync(CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        public void Dispose()
        {
        }
    }
}
