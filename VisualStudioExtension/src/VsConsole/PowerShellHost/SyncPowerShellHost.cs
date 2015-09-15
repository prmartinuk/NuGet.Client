﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using NuGet.PackageManagement.VisualStudio;

namespace NuGetConsole.Host.PowerShell.Implementation
{
    internal class SyncPowerShellHost : PowerShellHost
    {
        public SyncPowerShellHost(string name, IRunspaceManager runspaceManager)
            : base(name, runspaceManager)
        {
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        protected override bool ExecuteHost(string fullCommand, string command, params object[] inputs)
        {
            SetPrivateDataOnHost(true);

            try
            {
                Runspace.Invoke(fullCommand, inputs, true);
                OnExecuteCommandEnd();
            }
            catch (Exception e)
            {
                ExceptionHelper.WriteToActivityLog(e);
                throw;
            }

            return true;
        }

        protected override Task<string[]> GetExpansionsAsyncCore(string line, string lastWord, CancellationToken token)
        {
            return GetExpansionsAsyncCore(line, lastWord, isSync: true, token: token);
        }

        protected override Task<SimpleExpansion> GetPathExpansionsAsyncCore(string line, CancellationToken token)
        {
            return GetPathExpansionsAsyncCore(line, isSync: true, token: token);
        }
    }
}
