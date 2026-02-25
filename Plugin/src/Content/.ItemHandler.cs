using Dusk;

namespace ViralCompany.Content;
public class ItemHandler : ContentHandler<ItemHandler>
{
    public class CameraAssets(DuskMod mod, string filePath) : AssetBundleLoader<CameraAssets>(mod, filePath)
    {
    }

    public CameraAssets? Camera = null;

    public ItemHandler(DuskMod mod) : base(mod)
    {
        RegisterContent("viralcameraassets", out Camera);
    }
}