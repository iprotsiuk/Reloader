using Reloader.Core.Save.Modules;
using Reloader.Weapons.Ballistics;
using Reloader.Weapons.Controllers;
using UnityEngine;

namespace Reloader.Weapons.Runtime
{
    public sealed class WeaponsRuntimeSaveBridge : MonoBehaviour
    {
        [SerializeField] private PlayerWeaponController _weaponController;
        [SerializeField] private bool _logWarnings;

        private WeaponsModule _weaponsModule;

        public void SetWeaponsModuleForRuntime(WeaponsModule weaponsModule)
        {
            _weaponsModule = weaponsModule;
        }

        public void CaptureToModule()
        {
            if (!ResolveDependencies())
            {
                return;
            }

            _weaponsModule.WeaponStates.Clear();
            var snapshots = _weaponController.GetRuntimeStateSnapshots();
            for (var i = 0; i < snapshots.Count; i++)
            {
                var snapshot = snapshots[i];
                _weaponsModule.WeaponStates.Add(new WeaponsModule.WeaponStateRecord
                {
                    ItemId = snapshot.ItemId,
                    ChamberLoaded = snapshot.ChamberLoaded,
                    MagCount = snapshot.MagCount,
                    MagCapacity = snapshot.MagCapacity,
                    ReserveCount = snapshot.ReserveCount,
                    ChamberRound = snapshot.ChamberRound.HasValue ? ToRecord(snapshot.ChamberRound.Value) : null,
                    MagazineRounds = ToRecords(snapshot.MagazineRounds)
                });
            }
        }

        public void RestoreFromModule()
        {
            if (!ResolveDependencies())
            {
                return;
            }

            for (var i = 0; i < _weaponsModule.WeaponStates.Count; i++)
            {
                var state = _weaponsModule.WeaponStates[i];
                if (state == null || string.IsNullOrWhiteSpace(state.ItemId))
                {
                    continue;
                }

                var normalizedItemId = WeaponItemIdAliases.Normalize(state.ItemId);
                _weaponController.ApplyRuntimeState(normalizedItemId, state.MagCount, state.ReserveCount, state.ChamberLoaded);
                _weaponController.ApplyRuntimeBallistics(normalizedItemId, ToSnapshot(state.ChamberRound), ToSnapshots(state.MagazineRounds));
            }
        }

        private static WeaponsModule.AmmoBallisticRecord ToRecord(AmmoBallisticSnapshot snapshot)
        {
            return new WeaponsModule.AmmoBallisticRecord
            {
                CartridgeId = snapshot.CartridgeId,
                DisplayName = snapshot.DisplayName,
                AmmoItemId = snapshot.AmmoItemId,
                AmmoSource = (int)snapshot.AmmoSource,
                MuzzleVelocityFps = snapshot.MuzzleVelocityFps,
                VelocityStdDevFps = snapshot.VelocityStdDevFps,
                ProjectileMassGrains = snapshot.ProjectileMassGrains,
                BallisticCoefficientG1 = snapshot.BallisticCoefficientG1,
                DispersionMoa = snapshot.DispersionMoa
            };
        }

        private static System.Collections.Generic.List<WeaponsModule.AmmoBallisticRecord> ToRecords(System.Collections.Generic.IReadOnlyList<AmmoBallisticSnapshot> snapshots)
        {
            var records = new System.Collections.Generic.List<WeaponsModule.AmmoBallisticRecord>();
            if (snapshots == null)
            {
                return records;
            }

            for (var i = 0; i < snapshots.Count; i++)
            {
                records.Add(ToRecord(snapshots[i]));
            }

            return records;
        }

        private static AmmoBallisticSnapshot? ToSnapshot(WeaponsModule.AmmoBallisticRecord record)
        {
            if (record == null)
            {
                return null;
            }

            return new AmmoBallisticSnapshot(
                (AmmoSourceType)record.AmmoSource,
                record.MuzzleVelocityFps,
                record.VelocityStdDevFps,
                record.ProjectileMassGrains,
                record.BallisticCoefficientG1,
                record.DispersionMoa,
                record.DisplayName,
                record.CartridgeId,
                record.AmmoItemId);
        }

        private static System.Collections.Generic.List<AmmoBallisticSnapshot> ToSnapshots(System.Collections.Generic.IReadOnlyList<WeaponsModule.AmmoBallisticRecord> records)
        {
            var snapshots = new System.Collections.Generic.List<AmmoBallisticSnapshot>();
            if (records == null)
            {
                return snapshots;
            }

            for (var i = 0; i < records.Count; i++)
            {
                var snapshot = ToSnapshot(records[i]);
                if (snapshot.HasValue)
                {
                    snapshots.Add(snapshot.Value);
                }
            }

            return snapshots;
        }

        private bool ResolveDependencies()
        {
            if (_weaponController == null)
            {
                _weaponController = FindAnyObjectByType<PlayerWeaponController>();
            }

            var ready = _weaponController != null && _weaponsModule != null;
            if (!ready && _logWarnings)
            {
                Debug.LogWarning("WeaponsRuntimeSaveBridge requires both PlayerWeaponController and WeaponsModule.", this);
            }

            return ready;
        }
    }
}
