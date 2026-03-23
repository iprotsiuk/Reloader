using System.Reflection;
using NUnit.Framework;

namespace Reloader.World.Tests.EditMode
{
    public sealed class OriginRebaseParticipantEditModeTests
    {
        [Test]
        public void OriginRebaseParticipant_ExposesNotificationOnlyCallbacks()
        {
            var participantType = System.Type.GetType("Reloader.World.Runtime.Origin.IOriginRebaseParticipant, Reloader.World");
            Assert.That(participantType, Is.Not.Null);
            Assert.That(participantType.IsInterface, Is.True);

            var methods = participantType.GetMethods(BindingFlags.Instance | BindingFlags.Public);
            Assert.That(methods, Has.Length.EqualTo(2));
            Assert.That(methods[0].Name == "OnAfterOriginRebase" || methods[1].Name == "OnAfterOriginRebase", Is.True);
            Assert.That(methods[0].Name == "OnBeforeOriginRebase" || methods[1].Name == "OnBeforeOriginRebase", Is.True);
            Assert.That(participantType.GetProperties(BindingFlags.Instance | BindingFlags.Public), Is.Empty,
                "Participants should remain notification-only and not grow fallback reposition contracts.");
        }
    }
}
