using UnityEngine;

namespace Game1
{
    /// <summary>Local power is available as a long; remote level alone cannot identify the 70 billion boundary.</summary>
    public static class BlackPowerAura
    {
        const int FrameCount = 7;
        static Image[] frames;
        static int cacheZoom = -1;
        static bool attempted;
        static bool ownsTextures;

        public static bool ReachesThreshold(long power)
        {
            return power >= 70000000000L;
        }

        public static bool IsEligible(Char character)
        {
            if (character == null || !Char.isPaintAura2 || GameCanvas.lowGraphic || ModFunc.GiamDungLuong)
                return false;
            if ((GameCanvas.panel != null && GameCanvas.panel.isShow) || character.isHide
                || character.isCopy || character.isMiniPet || character.isMonkey != 0
                || character.statusMe == Char.A_DEADFLY || character.statusMe == Char.A_DEAD || character.statusMe == Char.A_HIDE)
                return false;
            if (character.idAuraEff > -1 && (!character.me || Char.isPaintAura))
                return false;
            if (character.me)
                return ReachesThreshold(character.cPower);

            Char pet = Char.KnownPowerPet;
            if (pet == null)
                return false;
            if (ReferenceEquals(character, pet))
                return ReachesThreshold(pet.cPower);
            bool disciple = character.isPet || (!string.IsNullOrEmpty(character.cName) && character.cName[0] == '$');
            if (!disciple)
                return false;
            Char owner = Char.KnownPowerOwner;
            bool samePet = pet.charID != 0 && pet.charID == character.charID;
            // The server assigns the player's disciple ID to -owner.id; the separate pet-info object has no ID.
            bool ownerPet = owner != null && owner.charID > 0 && (long)character.charID == -(long)owner.charID;
            return (samePet || ownerPet) && ReachesThreshold(pet.cPower);
        }

        public static bool IsReady(Char character)
        {
            return IsEligible(character) && EnsureFrames();
        }

        public static bool PaintBehind(mGraphics graphics, Char character)
        {
            if (!IsReady(character))
                return false;
            int phase = (character.charID & int.MaxValue) % FrameCount;
            int frame = ((GameCanvas.gameTick / 3) % FrameCount + phase) % FrameCount;
            if (frame < 0) frame += FrameCount;
            graphics.drawImage(frames[frame], character.cx, character.cy + 4, mGraphics.BOTTOM | mGraphics.HCENTER);
            return true;
        }

        static bool EnsureFrames()
        {
            if (cacheZoom != mGraphics.zoomLevel)
            {
                ReleaseFrames(frames, ownsTextures);
                frames = null;
                cacheZoom = mGraphics.zoomLevel;
                attempted = false;
                ownsTextures = cacheZoom != 4;
            }
            if (frames != null)
                return true;
            if (attempted)
                return false;
            attempted = true; // Missing assets fall back to the old aura; never retry each rendered frame.
            Image[] loaded = new Image[FrameCount];
            for (int i = 0; i < loaded.Length; i++)
            {
                loaded[i] = GameCanvas.loadImageResize("/poweraura/black/" + i + ".png");
                if (loaded[i] == null || loaded[i].texture == null || loaded[i].w <= 0 || loaded[i].h <= 0)
                {
                    ReleaseFrames(loaded, ownsTextures);
                    return false;
                }
            }
            frames = loaded;
            return true;
        }

        static void ReleaseFrames(Image[] images, bool owned)
        {
            // Zoom x4 comes directly from Resources and is shared. Only lower-zoom copies belong to this cache.
            if (!owned || images == null)
                return;
            for (int i = 0; i < images.Length; i++)
            {
                if (images[i] == null || images[i].texture == null)
                    continue;
                if (Application.isPlaying)
                    Object.Destroy(images[i].texture);
                else
                    Object.DestroyImmediate(images[i].texture);
            }
        }
    }
}
