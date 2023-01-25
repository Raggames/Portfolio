namespace SteamAndMagic.Systems.Economics
{
    public class SceneLootbag : LootBag
    {
        protected override void Awake()
        {
            base.Awake();

            for (int i = 0; i < LootsInBag.Count; ++i)
            {
                LootsInBag[i] = LootsInBag[i].GenerateLootData();
            }
        }
    }
}
