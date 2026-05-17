public class Camp
{
    public bool ForcesNewSpawnLocation(CampCardData worldEffectCard)
    {
        if (worldEffectCard == null)
        {
            return true;
        }

        return worldEffectCard.forcesNewSpawnLocation;
    }
}
