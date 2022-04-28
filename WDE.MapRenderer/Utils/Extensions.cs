using WDE.Common.Database;

namespace WDE.MapRenderer.Utils;

public static class Extensions
{
    public static uint GetRandomModel(this ICreatureTemplate template)
    {
        int numberOfModels = 0;
        for (int i = 0; i < template.ModelsCount; ++i)
        {
            if (template.GetModel(i) > 0)
                numberOfModels++;
            else
                break;
        }

        return template.GetModel(Random.Shared.Next(numberOfModels));
    }
}