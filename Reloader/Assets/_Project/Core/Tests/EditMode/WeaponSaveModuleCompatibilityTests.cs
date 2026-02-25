using NUnit.Framework;
using Reloader.Core.Save.Modules;
using Reloader.Weapons.Ballistics;

namespace Reloader.Core.Tests.EditMode
{
    public class WeaponSaveModuleCompatibilityTests
    {
        [Test]
        public void WeaponsModule_RoundTrip_PreservesWeaponStatesByItemId()
        {
            var module = new WeaponsModule();
            module.WeaponStates.Add(new WeaponsModule.WeaponStateRecord
            {
                ItemId = "weapon-rifle-01",
                ChamberLoaded = true,
                MagCapacity = 5,
                MagCount = 3,
                ReserveCount = 19,
                ChamberRound = new WeaponsModule.AmmoBallisticRecord
                {
                    AmmoSource = (int)AmmoSourceType.Handload,
                    MuzzleVelocityFps = 2715f,
                    VelocityStdDevFps = 9f,
                    ProjectileMassGrains = 175f,
                    BallisticCoefficientG1 = 0.5f,
                    DispersionMoa = 0.8f
                },
                MagazineRounds = new System.Collections.Generic.List<WeaponsModule.AmmoBallisticRecord>
                {
                    new WeaponsModule.AmmoBallisticRecord
                    {
                        AmmoSource = (int)AmmoSourceType.Factory,
                        MuzzleVelocityFps = 2650f,
                        VelocityStdDevFps = 15f,
                        ProjectileMassGrains = 168f,
                        BallisticCoefficientG1 = 0.45f,
                        DispersionMoa = 1.2f
                    }
                }
            });

            var json = module.CaptureModuleStateJson();

            var restored = new WeaponsModule();
            restored.RestoreModuleStateFromJson(json);

            Assert.That(restored.WeaponStates.Count, Is.EqualTo(1));
            Assert.That(restored.WeaponStates[0].ItemId, Is.EqualTo("weapon-rifle-01"));
            Assert.That(restored.WeaponStates[0].ChamberLoaded, Is.True);
            Assert.That(restored.WeaponStates[0].MagCapacity, Is.EqualTo(5));
            Assert.That(restored.WeaponStates[0].MagCount, Is.EqualTo(3));
            Assert.That(restored.WeaponStates[0].ReserveCount, Is.EqualTo(19));
            Assert.That(restored.WeaponStates[0].ChamberRound, Is.Not.Null);
            Assert.That(restored.WeaponStates[0].ChamberRound.MuzzleVelocityFps, Is.EqualTo(2715f));
            Assert.That(restored.WeaponStates[0].MagazineRounds, Is.Not.Null);
            Assert.That(restored.WeaponStates[0].MagazineRounds.Count, Is.EqualTo(1));
            Assert.That(restored.WeaponStates[0].MagazineRounds[0].AmmoSource, Is.EqualTo((int)AmmoSourceType.Factory));
        }

        [Test]
        public void WeaponsModule_Restore_LegacyPayload_DefaultsToEmptyStates()
        {
            var module = new WeaponsModule();
            module.RestoreModuleStateFromJson("{}");

            Assert.That(module.WeaponStates.Count, Is.EqualTo(0));
        }

        [Test]
        public void WeaponsModule_Validate_Throws_WhenMagCountExceedsCapacity()
        {
            var module = new WeaponsModule();
            module.WeaponStates.Add(new WeaponsModule.WeaponStateRecord
            {
                ItemId = "weapon-rifle-01",
                MagCapacity = 3,
                MagCount = 4,
                ReserveCount = 0,
                ChamberLoaded = false
            });

            Assert.Throws<System.InvalidOperationException>(() => module.ValidateModuleState());
        }

        [Test]
        public void WeaponsModule_Validate_Throws_WhenMagazinePayloadCountDiffersFromMagCount()
        {
            var module = new WeaponsModule();
            module.WeaponStates.Add(new WeaponsModule.WeaponStateRecord
            {
                ItemId = "weapon-rifle-01",
                MagCapacity = 5,
                MagCount = 2,
                ReserveCount = 10,
                ChamberLoaded = false,
                MagazineRounds = new System.Collections.Generic.List<WeaponsModule.AmmoBallisticRecord>
                {
                    new WeaponsModule.AmmoBallisticRecord
                    {
                        AmmoSource = (int)AmmoSourceType.Factory,
                        MuzzleVelocityFps = 2650f
                    }
                }
            });

            Assert.Throws<System.InvalidOperationException>(() => module.ValidateModuleState());
        }

        [Test]
        public void WeaponsModule_Validate_AllowsLargeCapacityAndReserveValues()
        {
            var module = new WeaponsModule();
            module.WeaponStates.Add(new WeaponsModule.WeaponStateRecord
            {
                ItemId = "weapon-rifle-01",
                MagCapacity = 500,
                MagCount = 250,
                ReserveCount = 125000,
                ChamberLoaded = false,
                MagazineRounds = new System.Collections.Generic.List<WeaponsModule.AmmoBallisticRecord>()
            });

            for (var i = 0; i < 250; i++)
            {
                module.WeaponStates[0].MagazineRounds.Add(new WeaponsModule.AmmoBallisticRecord
                {
                    AmmoSource = (int)AmmoSourceType.Factory,
                    MuzzleVelocityFps = 2650f
                });
            }

            Assert.DoesNotThrow(() => module.ValidateModuleState());
        }
    }
}
