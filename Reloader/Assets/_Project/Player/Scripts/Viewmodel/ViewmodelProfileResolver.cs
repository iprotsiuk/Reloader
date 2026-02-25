namespace Reloader.Player.Viewmodel
{
    public static class ViewmodelProfileResolver
    {
        public static T Resolve<T>(
            T weaponSpecific,
            T familyDefault,
            T globalDefault) where T : class
        {
            return weaponSpecific != null
                ? weaponSpecific
                : familyDefault != null
                    ? familyDefault
                    : globalDefault;
        }
    }
}
