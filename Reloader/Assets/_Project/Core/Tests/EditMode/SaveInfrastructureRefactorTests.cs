using System;
using System.Collections.Generic;
using NUnit.Framework;
using Reloader.Core.Save;
using UnityEngine;
using UnityEngine.TestTools;

namespace Reloader.Core.Tests.EditMode
{
    public class SaveInfrastructureRefactorTests
    {
        [Test]
        public void SaveLoadTransaction_Rollback_RestoresSnapshotState()
        {
            var moduleA = new RecordingModule("A", "{\"value\":\"before-a\"}");
            var moduleB = new RecordingModule("B", "{\"value\":\"before-b\"}");
            var transaction = SaveLoadTransaction.Capture(new[]
            {
                new SaveModuleRegistration(0, moduleA),
                new SaveModuleRegistration(1, moduleB)
            });

            moduleA.SetPayloadForTests("{\"value\":\"after-a\"}");
            moduleB.SetPayloadForTests("{\"value\":\"after-b\"}");

            Assert.DoesNotThrow(() => transaction.RollbackOrThrow(new InvalidOperationException("restore failed")));
            Assert.That(moduleA.CurrentPayload, Is.EqualTo("{\"value\":\"before-a\"}"));
            Assert.That(moduleB.CurrentPayload, Is.EqualTo("{\"value\":\"before-b\"}"));
        }

        [Test]
        public void SaveLoadTransaction_RollbackFailure_ThrowsAggregateWithRestoreAndRollbackExceptions()
        {
            var moduleA = new RecordingModule("A", "{\"value\":\"before-a\"}");
            var moduleB = new RecordingModule("B", "{\"value\":\"before-b\"}") { ThrowOnRestore = true };
            var transaction = SaveLoadTransaction.Capture(new[]
            {
                new SaveModuleRegistration(0, moduleA),
                new SaveModuleRegistration(1, moduleB)
            });

            var restoreEx = new InvalidOperationException("restore failed");
            var ex = Assert.Throws<InvalidOperationException>(() => transaction.RollbackOrThrow(restoreEx));
            Assert.That(ex, Is.Not.Null);
            Assert.That(ex.InnerException, Is.TypeOf<AggregateException>());
            var aggregate = (AggregateException)ex.InnerException;
            Assert.That(aggregate.InnerExceptions.Count, Is.EqualTo(2));
            Assert.That(aggregate.InnerExceptions[0], Is.SameAs(restoreEx));
            Assert.That(aggregate.InnerExceptions[1].Message, Does.Contain("Rollback restore failed for B"));
        }

        [Test]
        public void SaveValidation_EnsureRequiredString_ThrowsForWhitespace()
        {
            Assert.Throws<InvalidOperationException>(() =>
                SaveValidation.EnsureRequiredString(" ", "expected failure"));
        }

        [Test]
        public void SaveValidation_EnsureNonNegative_ThrowsForNegative()
        {
            Assert.Throws<InvalidOperationException>(() =>
                SaveValidation.EnsureNonNegative(-1, "expected failure"));
        }

        [Test]
        public void SaveValidation_EnsureCountMatch_ThrowsForMismatch()
        {
            Assert.Throws<InvalidOperationException>(() =>
                SaveValidation.EnsureCountMatch(2, 1, "expected failure"));
        }

        [Test]
        public void DependencyResolutionGuard_HasRequiredReferences_LogsOnlyOnce()
        {
            var context = new GameObject("GuardContext");
            var logged = false;
            var message = "Missing required dependency.";

            LogAssert.Expect(LogType.Error, message);
            Assert.That(DependencyResolutionGuard.HasRequiredReferences(ref logged, context, message, null), Is.False);
            Assert.That(DependencyResolutionGuard.HasRequiredReferences(ref logged, context, message, null), Is.False);

            UnityEngine.Object.DestroyImmediate(context);
        }

        [Test]
        public void DependencyResolutionGuard_ResolveOnce_AttemptsResolverOnce()
        {
            var attempted = false;
            var calls = 0;
            object dependency = null;

            DependencyResolutionGuard.ResolveOnce(ref dependency, ref attempted, () =>
            {
                calls++;
                return new object();
            });

            DependencyResolutionGuard.ResolveOnce(ref dependency, ref attempted, () =>
            {
                calls++;
                return new object();
            });

            Assert.That(dependency, Is.Not.Null);
            Assert.That(calls, Is.EqualTo(1));
            Assert.That(attempted, Is.True);
        }

        private sealed class RecordingModule : ISaveDomainModule
        {
            public RecordingModule(string key, string payload)
            {
                ModuleKey = key;
                CurrentPayload = payload;
            }

            public string ModuleKey { get; }
            public int ModuleVersion => 1;
            public string CurrentPayload { get; private set; }
            public bool ThrowOnRestore { get; set; }

            public void SetPayloadForTests(string payload)
            {
                CurrentPayload = payload;
            }

            public string CaptureModuleStateJson()
            {
                return CurrentPayload;
            }

            public void RestoreModuleStateFromJson(string payloadJson)
            {
                if (ThrowOnRestore)
                {
                    throw new InvalidOperationException($"Rollback restore failed for {ModuleKey}");
                }

                CurrentPayload = payloadJson;
            }

            public void ValidateModuleState()
            {
            }
        }
    }
}
