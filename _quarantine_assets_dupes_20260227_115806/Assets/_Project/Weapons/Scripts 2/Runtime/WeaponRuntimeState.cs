namespace Reloader.Weapons.Runtime
{
    public readonly struct WeaponFireData
    {
        public WeaponFireData(float timestamp)
        {
            Timestamp = timestamp;
        }

        public float Timestamp { get; }
    }

    public sealed class WeaponRuntimeState
    {
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
            MagazineCount = magazineCount < 0 ? 0 : magazineCount;
            ReserveCount = reserveCount < 0 ? 0 : reserveCount;
            ChamberLoaded = chamberLoaded;
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

            if (MagazineCount > 0)
            {
                MagazineCount--;
                ChamberLoaded = true;
            }
            else
            {
                ChamberLoaded = false;
            }

            NextFireTime = now + FireIntervalSeconds;
            fireData = new WeaponFireData(now);
            return true;
        }

        public bool TryReload()
        {
            if (MagazineCount >= MagazineCapacity || ReserveCount <= 0)
            {
                return false;
            }

            var needed = MagazineCapacity - MagazineCount;
            var moved = needed <= ReserveCount ? needed : ReserveCount;
            MagazineCount += moved;
            ReserveCount -= moved;
            return moved > 0;
        }

        public void SetAmmoCounts(int magazineCount, int reserveCount, bool chamberLoaded)
        {
            MagazineCount = magazineCount < 0 ? 0 : magazineCount;
            ReserveCount = reserveCount < 0 ? 0 : reserveCount;
            ChamberLoaded = chamberLoaded;
        }
    }
}
