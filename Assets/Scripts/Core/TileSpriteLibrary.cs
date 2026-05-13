using System;
using UnityEngine;

[CreateAssetMenu(fileName = "TileSpriteLibrary", menuName = "Unlock2D/Tile Sprite Library")]
public sealed class TileSpriteLibrary : ScriptableObject
{
    [SerializeField] private Sprite[] tileSprites = Array.Empty<Sprite>();
    [SerializeField] private Sprite backTileSprite;

    public Sprite[] TileSprites => tileSprites;
    public Sprite BackTileSprite => backTileSprite;
}
