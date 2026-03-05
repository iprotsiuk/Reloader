using System.Collections.Generic;
using Reloader.Weapons.Ballistics;
using Reloader.Weapons.Data;
using UnityEngine;

namespace Reloader.Weapons.Runtime
{
    public readonly struct WeaponFireData
    {
        public WeaponFireData(float timestamp, AmmoBallisticSnapshot? firedRound)
        {
            Timestamp = timestamp;
            FiredRound = firedRound;
        }

        public float Timestamp { get; }
        public AmmoBallisticSnapshot? FiredRound { get; }
    }

    public sealed class WeaponRuntimeState
    {
        private AmmoBallisticSnapshot? _chamberRound;
        private readonly Queue<AmmoBallisticSnapshot> _magazineRounds = new Queue<AmmoBallisticSnapshot>();
        private AmmoBallisticSnapshot? _reserveRoundTemplate;
        private readonly Dictionary<WeaponAttachmentSlotType, string> _equippedAttachmentItemIdsBySlot = new Dictionary<WeaponAttachmentSlotType, string>();

        public WeaponRuntimeState(
            string itemId,
            int magazineCapacity,
            float fireIntervalSeconds,
            int magazineCount,
            int reserveCount,
            bool chamberLoaded)
        {
            ItemId = itemId;
            MagazineCapacity = magazineCapacity < 0 ? 0 : magazineCapacity;
            FireIntervalSeconds = fireIntervalSeconds < 0.01f ? 0.01f : fireIntervalSeconds;
            MagazineCount = Mathf.Clamp(magazineCount, 0, MagazineCapacity);
            ReserveCount = reserveCount < 0 ? 0 : reserveCount;
            ChamberLoaded = chamberLoaded;
            if (ChamberLoaded && !_chamberRound.HasValue)
            {
                _chamberRound = BuildRoundFromTemplate();
            }

            SyncMagazineSnapshotsToCount();
        }

        public string ItemId { get; }
        public int MagazineCapacity { get; }
        public float FireIntervalSeconds { get; }
        public bool IsEquipped { get; set; }
        public bool ChamberLoaded { get; private set; }
        public int MagazineCount { get; private set; }
        public int ReserveCount { get; private set; }
        public bool IsReloading { get; set; }
        public float NextFireTime { get; private set; }
        public AmmoBallisticSnapshot? ChamberRound => _chamberRound;

        public bool CanFire(float now)
        {
            return !IsReloading && ChamberLoaded && now >= NextFireTime;
        }

        public bool TryFire(float now, out WeaponFireData fireData)
        {
            fireData = default;
            if (!CanFire(now))
            {
                return false;
            }

            SyncMagazineSnapshotsToCount();
            var firedRound = _chamberRound;
            if (MagazineCount > 0)
            {
                MagazineCount--;
                ChamberLoaded = true;
                if (_magazineRounds.Count > 0)
                {
                    _chamberRound = _magazineRounds.Dequeue();
                }
                else
                {
                    _chamberRound = BuildRoundFromTemplate();
                }
            }
            else
            {
                ChamberLoaded = false;
                _chamberRound = null;
            }

            NextFireTime = now + FireIntervalSeconds;
            fireData = new WeaponFireData(now, firedRound);
            return true;
        }

        public bool TryReload()
        {
            if (MagazineCount >= MagazineCapacity || ReserveCount <= 0)
            {
                return false;
            }

            SyncMagazineSnapshotsToCount();
            var needed = MagazineCapacity - MagazineCount;
            var moved = needed <= ReserveCount ? needed : ReserveCount;
            if (moved <= 0)
            {
                return false;
            }

            for (var i = 0; i < moved; i++)
            {
                _magazineRounds.Enqueue(BuildRoundFromTemplate());
            }

            MagazineCount += moved;
            ReserveCount -= moved;

            if (!ChamberLoaded && MagazineCount > 0)
            {
                MagazineCount--;
                ChamberLoaded = true;
                if (_magazineRounds.Count > 0)
                {
                    _chamberRound = _magazineRounds.Dequeue();
                }
                else
                {
                    _chamberRound = BuildRoundFromTemplate();
                }
            }

            return true;
        }

        public void SetAmmoCounts(int magazineCount, int reserveCount, bool chamberLoaded)
        {
            MagazineCount = Mathf.Clamp(magazineCount, 0, MagazineCapacity);
            ReserveCount = reserveCount < 0 ? 0 : reserveCount;
            ChamberLoaded = chamberLoaded;
            if (ChamberLoaded && !_chamberRound.HasValue)
            {
                _chamberRound = BuildRoundFromTemplate();
            }
            else if (!ChamberLoaded)
            {
                _chamberRound = null;
            }

            SyncMagazineSnapshotsToCount();
        }

        public void SetReserveCount(int reserveCount)
        {
            ReserveCount = reserveCount < 0 ? 0 : reserveCount;
        }

        public void SetAmmoLoadoutForTests(AmmoBallisticSnapshot? chamberRound, IReadOnlyList<AmmoBallisticSnapshot> magazineRounds)
        {
            _chamberRound = chamberRound;
            _magazineRounds.Clear();
            if (magazineRounds != null)
            {
                var count = Mathf.Min(magazineRounds.Count, MagazineCapacity);
                for (var i = 0; i < count; i++)
                {
                    _magazineRounds.Enqueue(magazineRounds[i]);
                }
            }

            ChamberLoaded = chamberRound.HasValue;
            MagazineCount = _magazineRounds.Count;
            _reserveRoundTemplate = chamberRound;
            if (!_reserveRoundTemplate.HasValue && _magazineRounds.Count > 0)
            {
                _reserveRoundTemplate = _magazineRounds.Peek();
            }
        }

        public IReadOnlyList<AmmoBallisticSnapshot> GetMagazineRoundsSnapshot()
        {
            SyncMagazineSnapshotsToCount();
            return new List<AmmoBallisticSnapshot>(_magazineRounds);
        }

        public string GetEquippedAttachmentItemId(WeaponAttachmentSlotType slotType)
        {
            if (_equippedAttachmentItemIdsBySlot.TryGetValue(slotType, out var itemId) && !string.IsNullOrWhiteSpace(itemId))
            {
                return itemId;
            }

            return string.Empty;
        }

        public void SetEquippedAttachmentItemId(WeaponAttachmentSlotType slotType, string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                _equippedAttachmentItemIdsBySlot.Remove(slotType);
                return;
            }

            _equippedAttachmentItemIdsBySlot[slotType] = itemId;
        }

        public IReadOnlyDictionary<WeaponAttachmentSlotType, string> GetEquippedAttachmentItemIdsSnapshot()
        {
            return new Dictionary<WeaponAttachmentSlotType, string>(_equippedAttachmentItemIdsBySlot);
        }

        private void SyncMagazineSnapshotsToCount()
        {
            while (_magazineRounds.Count < MagazineCount)
            {
                _magazineRounds.Enqueue(BuildRoundFromTemplate());
            }

            while (_magazineRounds.Count > MagazineCount)
            {
                _magazineRounds.Dequeue();
            }
        }

        private AmmoBallisticSnapshot BuildRoundFromTemplate()
        {
            if (!_reserveRoundTemplate.HasValue)
            {
                _reserveRoundTemplate = _chamberRound ?? BuildDefaultRound();
            }

            return WeaponAmmoDefaults.BuildRoundFromTemplate(_reserveRoundTemplate.Value);
        }

        private static AmmoBallisticSnapshot BuildDefaultRound()
        {
            return WeaponAmmoDefaults.BuildDefaultRound();
        }
    }
}
