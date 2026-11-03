using System;
using System.IO;
using NUnit.Framework;
using Survivor.Core;

namespace Survivor.Tests
{
    [SetUpFixture]
    public sealed class SaveTestIsolation
    {
        [OneTimeSetUp] public void Begin()
            => GameFlow.SaveDirectoryOverride = Path.Combine(Path.GetTempPath(), "Survivor-PlayTests-" + Guid.NewGuid().ToString("N"));
        [OneTimeTearDown] public void End() => GameFlow.SaveDirectoryOverride = null;
    }
}
