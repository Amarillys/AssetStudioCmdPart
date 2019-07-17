using AssetStudio;

namespace AssetStudioCmd
{
    internal class AssetItem
    {
        public Object Asset;
        public SerializedFile SourceFile;
        public long FullSize;
        public ClassIDType Type;
        public string TypeString;
        public string Text;

        public string Extension;
        public string InfoText;
        public string UniqueID;

        public AssetItem(Object asset)
        {
            Asset = asset;
            SourceFile = asset.assetsFile;
            FullSize = asset.byteSize;
            Type = asset.type;
            TypeString = Type.ToString();
        }

        public AssetItem()
        {
            Asset = null;
        }
    }
}
